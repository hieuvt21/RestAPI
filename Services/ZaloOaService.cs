using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class ZaloOaService : IZaloOaService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ZaloOaService> _logger;

    // Endpoint chính thức của Zalo (theo tài liệu developers.zalo.me).
    // Lưu ý: Zalo có thể cập nhật tài liệu theo thời gian, nên nếu gặp lỗi
    // 404/410 hãy đối chiếu lại https://developers.zalo.me trước khi báo lỗi.
    private const string TokenEndpoint = "https://oauth.zaloapp.com/v4/oa/access_token";
    private const string ZnsSendEndpoint = "https://business.openapi.zalo.me/message/template";
    private const string OaMessageEndpoint = "https://openapi.zalo.me/v3.0/oa/message/cs";
    private const string OaFollowersEndpoint = "https://openapi.zalo.me/v3.0/oa/user/getlist";

    public ZaloOaService(AppDbContext context, IHttpClientFactory httpClientFactory, ILogger<ZaloOaService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ZaloOaConfig?> GetRawConfigAsync()
    {
        return await _context.ZaloOaConfigs.FirstOrDefaultAsync(c => c.Id == 1);
    }

    public async Task<ZaloOaConfigViewDto> GetConfigViewAsync()
    {
        var cfg = await GetRawConfigAsync();
        if (cfg == null)
        {
            return new ZaloOaConfigViewDto { IsConfigured = false };
        }

        return MapToView(cfg);
    }

    public async Task<ZaloOaConfigViewDto> UpdateConfigAsync(UpdateZaloOaConfigDto dto)
    {
        var cfg = await GetRawConfigAsync();
        bool isNew = cfg == null;
        cfg ??= new ZaloOaConfig { Id = 1 };

        cfg.AppId = dto.AppId?.Trim();
        cfg.AppSecret = dto.AppSecret?.Trim();
        cfg.OaId = dto.OaId?.Trim();

        // Chỉ ghi đè Refresh Token nếu người dùng thực sự nhập giá trị mới
        // (tránh trường hợp Flutter gửi lại giá trị rỗng do form không hiển thị token thật)
        if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            cfg.RefreshToken = dto.RefreshToken.Trim();
            // Refresh token mới nhập tay -> access token cũ (nếu có) không còn tin cậy, xoá để buộc refresh lại
            cfg.AccessToken = null;
            cfg.TokenExpiredAt = null;
        }

        cfg.EnableAppointmentReminder = dto.EnableAppointmentReminder;
        cfg.AppointmentReminderTemplateId = dto.AppointmentReminderTemplateId?.Trim();

        cfg.EnableOrderNotification = dto.EnableOrderNotification;
        cfg.OrderNotificationTemplateId = dto.OrderNotificationTemplateId?.Trim();

        cfg.EnableTierUpgradeNotification = dto.EnableTierUpgradeNotification;
        cfg.TierUpgradeTemplateId = dto.TierUpgradeTemplateId?.Trim();

        cfg.UpdatedAt = DateTime.UtcNow;

        if (isNew) _context.ZaloOaConfigs.Add(cfg);
        await _context.SaveChangesAsync();

        return MapToView(cfg);
    }

    public async Task<(bool ok, string? error)> RefreshAccessTokenAsync()
    {
        var cfg = await GetRawConfigAsync();
        if (cfg == null || string.IsNullOrWhiteSpace(cfg.AppId) ||
            string.IsNullOrWhiteSpace(cfg.AppSecret) || string.IsNullOrWhiteSpace(cfg.RefreshToken))
        {
            return (false, "Chưa khai báo đủ App ID / App Secret / Refresh Token.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
            request.Headers.Add("secret_key", cfg.AppSecret);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["app_id"] = cfg.AppId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = cfg.RefreshToken,
            });

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                cfg.LastRefreshError = $"HTTP {(int)response.StatusCode}: {body}";
                await _context.SaveChangesAsync();
                return (false, cfg.LastRefreshError);
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("access_token", out var accessTokenEl))
            {
                cfg.LastRefreshError = $"Phản hồi không có access_token: {body}";
                await _context.SaveChangesAsync();
                return (false, cfg.LastRefreshError);
            }

            cfg.AccessToken = accessTokenEl.GetString();

            if (root.TryGetProperty("refresh_token", out var refreshEl))
                cfg.RefreshToken = refreshEl.GetString();

            int expiresInSeconds = 3600; // fallback an toàn: 1 giờ
            if (root.TryGetProperty("expires_in", out var expiresEl))
            {
                // Zalo có lúc trả về string, có lúc trả về number -> xử lý cả hai
                if (expiresEl.ValueKind == JsonValueKind.String)
                    int.TryParse(expiresEl.GetString(), out expiresInSeconds);
                else if (expiresEl.ValueKind == JsonValueKind.Number)
                    expiresInSeconds = expiresEl.GetInt32();
            }

            cfg.TokenExpiredAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            cfg.LastRefreshError = null;
            cfg.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            cfg.LastRefreshError = ex.Message;
            await _context.SaveChangesAsync();
            return (false, ex.Message);
        }
    }

    public async Task<string?> EnsureValidAccessTokenAsync()
    {
        var cfg = await GetRawConfigAsync();
        if (cfg == null) return null;

        bool needsRefresh = string.IsNullOrWhiteSpace(cfg.AccessToken)
            || cfg.TokenExpiredAt == null
            || cfg.TokenExpiredAt.Value < DateTime.UtcNow.AddMinutes(60);

        if (needsRefresh)
        {
            var (ok, _) = await RefreshAccessTokenAsync();
            if (!ok) return null;
            cfg = await GetRawConfigAsync();
        }

        return cfg?.AccessToken;
    }

    public async Task<ZnsSendResult> SendZnsAsync(
        string phone,
        string templateId,
        Dictionary<string, string> templateData,
        string messageType,
        int? khachHangId = null)
    {
        var log = new ZaloMessageLog
        {
            KhachHangId = khachHangId,
            Phone = phone,
            MessageType = messageType,
            TemplateId = templateId,
            TemplateDataJson = JsonSerializer.Serialize(templateData),
            Status = "Pending",
        };

        var accessToken = await EnsureValidAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            log.Status = "Failed";
            log.ErrorMessage = "Không lấy được Access Token hợp lệ (kiểm tra lại cấu hình Zalo OA).";
            await SafeSaveLogAsync(log);
            return new ZnsSendResult { Success = false, ErrorMessage = log.ErrorMessage };
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, ZnsSendEndpoint);
            request.Headers.Add("access_token", accessToken);

            var payload = new
            {
                phone,
                template_id = templateId,
                template_data = templateData,
                tracking_id = Guid.NewGuid().ToString("N"),
            };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Zalo trả về { "error": 0, "message": "Success", "data": { "msg_id": "..." } } khi thành công
            int errorCode = root.TryGetProperty("error", out var errEl) ? errEl.GetInt32() : -1;

            if (errorCode == 0)
            {
                string? msgId = null;
                if (root.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("msg_id", out var msgIdEl))
                    msgId = msgIdEl.GetString();

                log.Status = "Success";
                log.ZaloMsgId = msgId;
                await SafeSaveLogAsync(log);

                return new ZnsSendResult { Success = true, ZaloMsgId = msgId };
            }
            else
            {
                string errorMsg = root.TryGetProperty("message", out var msgEl)
                    ? msgEl.GetString() ?? body
                    : body;

                log.Status = "Failed";
                log.ErrorMessage = $"[{errorCode}] {errorMsg}";
                await SafeSaveLogAsync(log);

                return new ZnsSendResult { Success = false, ErrorMessage = log.ErrorMessage };
            }
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
            await SafeSaveLogAsync(log);

            return new ZnsSendResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ZnsSendResult> SendOaTextMessageAsync(string userId, string text)
    {
        var log = new ZaloMessageLog
        {
            Phone = userId, // Gửi qua User ID (không có SĐT thật) -> lưu thẳng user_id vào cột phone
            MessageType = "TestOA",
            TemplateDataJson = JsonSerializer.Serialize(new { text }),
            Status = "Pending",
        };

        var accessToken = await EnsureValidAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            log.Status = "Failed";
            log.ErrorMessage = "Không lấy được Access Token hợp lệ (kiểm tra lại cấu hình Zalo OA).";
            await SafeSaveLogAsync(log);
            return new ZnsSendResult { Success = false, ErrorMessage = log.ErrorMessage };
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, OaMessageEndpoint);
            request.Headers.Add("access_token", accessToken);

            var payload = new
            {
                recipient = new { user_id = userId },
                message = new { text },
            };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            int errorCode = root.TryGetProperty("error", out var errEl) ? errEl.GetInt32() : -1;

            if (errorCode == 0)
            {
                string? msgId = null;
                if (root.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("message_id", out var msgIdEl))
                    msgId = msgIdEl.GetString();

                log.Status = "Success";
                log.ZaloMsgId = msgId;
                await SafeSaveLogAsync(log);

                return new ZnsSendResult { Success = true, ZaloMsgId = msgId };
            }
            else
            {
                // Lỗi thường gặp nhất khi test: error = -216 (user chưa từng tương tác/ngoài khung 48h)
                string errorMsg = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? body : body;
                log.Status = "Failed";
                log.ErrorMessage = $"[{errorCode}] {errorMsg}";
                await SafeSaveLogAsync(log);

                return new ZnsSendResult { Success = false, ErrorMessage = log.ErrorMessage };
            }
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
            await SafeSaveLogAsync(log);
            return new ZnsSendResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<List<ZaloFollowerDto>> GetFollowersAsync(int count = 20)
    {
        var accessToken = await EnsureValidAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Không lấy được Access Token hợp lệ.");
        }

        var client = _httpClientFactory.CreateClient();
        // Zalo yêu cầu gộp offset/count vào 1 tham số "data" dạng JSON đã encode,
        // KHÔNG truyền offset & count là 2 query param riêng lẻ (sẽ bị lỗi -201).
        var dataParam = JsonSerializer.Serialize(new { offset = 0, count });
        var url = $"{OaFollowersEndpoint}?data={Uri.EscapeDataString(dataParam)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("access_token", accessToken);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        int errorCode = root.TryGetProperty("error", out var errEl) ? errEl.GetInt32() : -1;

        var result = new List<ZaloFollowerDto>();
        if (errorCode == 0 && root.TryGetProperty("data", out var dataEl)
            && dataEl.TryGetProperty("followers", out var followersEl))
        {
            foreach (var f in followersEl.EnumerateArray())
            {
                result.Add(new ZaloFollowerDto
                {
                    UserId = f.TryGetProperty("user_id", out var idEl) ? idEl.GetString() ?? "" : "",
                    DisplayName = f.TryGetProperty("display_name", out var nameEl) ? nameEl.GetString() : null,
                });
            }
        }
        else
        {
            string errorMsg = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? body : body;
            throw new InvalidOperationException($"[{errorCode}] {errorMsg}");
        }

        return result;
    }

    public async Task<List<ZaloMessageLog>> GetLogsAsync(int take = 100)
    {
        return await _context.ZaloMessageLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    // Ghi log gửi tin vào DB một cách an toàn: nếu vì lý do gì đó (lệch schema,
    // mất kết nối DB tạm thời...) mà ghi log thất bại, chỉ ghi nhận vào
    // ILogger chứ KHÔNG ném exception ra ngoài — vì lúc này tin nhắn Zalo đã
    // gửi xong rồi, không được để lỗi ghi log làm hỏng kết quả trả về cho Flutter.
    private async Task SafeSaveLogAsync(ZaloMessageLog log)
    {
        try
        {
            _context.ZaloMessageLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ghi log Zalo message thất bại (không ảnh hưởng kết quả gửi tin đã trả về).");
            // Gỡ entity khỏi change tracker để tránh lỗi lặp lại ở lần SaveChanges kế tiếp
            _context.Entry(log).State = EntityState.Detached;
        }
    }

    private static ZaloOaConfigViewDto MapToView(ZaloOaConfig cfg)
    {
        return new ZaloOaConfigViewDto
        {
            IsConfigured = !string.IsNullOrWhiteSpace(cfg.AppId) && !string.IsNullOrWhiteSpace(cfg.OaId),
            AppId = cfg.AppId,
            OaId = cfg.OaId,
            AppSecretMasked = MaskSecret(cfg.AppSecret),
            HasValidToken = !string.IsNullOrWhiteSpace(cfg.AccessToken)
                && cfg.TokenExpiredAt.HasValue
                && cfg.TokenExpiredAt.Value > DateTime.UtcNow,
            TokenExpiredAt = cfg.TokenExpiredAt,
            LastRefreshError = cfg.LastRefreshError,
            EnableAppointmentReminder = cfg.EnableAppointmentReminder,
            AppointmentReminderTemplateId = cfg.AppointmentReminderTemplateId,
            EnableOrderNotification = cfg.EnableOrderNotification,
            OrderNotificationTemplateId = cfg.OrderNotificationTemplateId,
            EnableTierUpgradeNotification = cfg.EnableTierUpgradeNotification,
            TierUpgradeTemplateId = cfg.TierUpgradeTemplateId,
        };
    }

    private static string? MaskSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length < 6) return secret == null ? null : "••••••";
        return secret[..3] + new string('•', 6) + secret[^2..];
    }
}