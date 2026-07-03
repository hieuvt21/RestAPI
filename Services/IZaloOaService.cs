public interface IZaloOaService
{
    Task<ZaloOaConfig?> GetRawConfigAsync();
    Task<ZaloOaConfigViewDto> GetConfigViewAsync();
    Task<ZaloOaConfigViewDto> UpdateConfigAsync(UpdateZaloOaConfigDto dto);

    /// Chủ động làm mới Access Token bằng Refresh Token hiện có.
    /// Trả về (thành công, thông báo lỗi nếu có).
    Task<(bool ok, string? error)> RefreshAccessTokenAsync();

    /// Đảm bảo access token còn hạn sử dụng trước khi gọi API gửi tin,
    /// tự động refresh nếu sắp hết hạn (< 60 phút).
    Task<string?> EnsureValidAccessTokenAsync();

    /// Gửi 1 tin ZNS theo số điện thoại + template đã được Zalo duyệt.
    Task<ZnsSendResult> SendZnsAsync(
        string phone,
        string templateId,
        Dictionary<string, string> templateData,
        string messageType,
        int? khachHangId = null);

    Task<List<ZaloMessageLog>> GetLogsAsync(int take = 100);

    /// Gửi tin nhắn OA thường (text tự do) tới 1 user_id đang follow OA.
    /// KHÔNG cần Template ZNS đã duyệt -> dùng để test nhanh trong lúc chờ duyệt template.
    /// Chỉ gửi được nếu user_id đó còn trong khung 48h tương tác gần nhất với OA.
    Task<ZnsSendResult> SendOaTextMessageAsync(string userId, string text);

    /// Lấy danh sách người đang quan tâm (follow) OA để chọn khi gửi thử.
    Task<List<ZaloFollowerDto>> GetFollowersAsync(int count = 20);
}