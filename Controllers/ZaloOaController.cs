using Microsoft.AspNetCore.Mvc;

[Route("api/zalooa")]
[ApiController]
public class ZaloOaController : ControllerBase
{
    private readonly IZaloOaService _zaloOaService;

    public ZaloOaController(IZaloOaService zaloOaService)
    {
        _zaloOaService = zaloOaService;
    }

    // 1. LẤY CẤU HÌNH HIỆN TẠI (đã ẩn bớt thông tin nhạy cảm)
    // GET /api/zalooa/config
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var view = await _zaloOaService.GetConfigViewAsync();
        return Ok(view);
    }

    // 2. LƯU / CẬP NHẬT CẤU HÌNH
    // PUT /api/zalooa/config
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateZaloOaConfigDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AppId) || string.IsNullOrWhiteSpace(dto.OaId))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ App ID và OA ID." });
        }

        try
        {
            var view = await _zaloOaService.UpdateConfigAsync(dto);
            return Ok(new { success = true, message = "Đã lưu cấu hình Zalo OA!", config = view });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lưu cấu hình: {ex.Message}" });
        }
    }

    // 3. LÀM MỚI ACCESS TOKEN THỦ CÔNG (nút "Kiểm tra kết nối" trên UI)
    // POST /api/zalooa/refresh-token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var (ok, error) = await _zaloOaService.RefreshAccessTokenAsync();
        if (!ok)
        {
            return BadRequest(new { success = false, message = error ?? "Làm mới token thất bại." });
        }

        var view = await _zaloOaService.GetConfigViewAsync();
        return Ok(new { success = true, message = "Kết nối Zalo OA thành công!", config = view });
    }

    // 4. GỬI TIN THỬ (để kiểm tra template + số điện thoại trước khi bật tự động)
    // POST /api/zalooa/send-test
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTest([FromBody] SendTestZnsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.TemplateId))
        {
            return BadRequest(new { message = "Vui lòng nhập số điện thoại và Template ID." });
        }

        var result = await _zaloOaService.SendZnsAsync(dto.Phone, dto.TemplateId, dto.TemplateData, "Test");

        if (result.Success)
        {
            return Ok(new { success = true, message = "Đã gửi tin thử thành công!", msgId = result.ZaloMsgId });
        }

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    // 5. GỬI TIN OA THƯỜNG (text tự do, KHÔNG cần Template ZNS) - dùng để test nhanh
    // POST /api/zalooa/send-test-oa-message
    [HttpPost("send-test-oa-message")]
    public async Task<IActionResult> SendTestOaMessage([FromBody] SendTestOaMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.Text))
        {
            return BadRequest(new { message = "Vui lòng chọn người nhận và nhập nội dung." });
        }

        var result = await _zaloOaService.SendOaTextMessageAsync(dto.UserId, dto.Text);
        if (result.Success)
        {
            return Ok(new { success = true, message = "Đã gửi tin thử thành công!", msgId = result.ZaloMsgId });
        }

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    // 6. DANH SÁCH NGƯỜI ĐANG QUAN TÂM (FOLLOW) OA - để chọn người nhận khi gửi thử
    // GET /api/zalooa/followers
    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers([FromQuery] int count = 20)
    {
        try
        {
            var followers = await _zaloOaService.GetFollowersAsync(count);
            return Ok(followers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // 7. LỊCH SỬ GỬI TIN
    // GET /api/zalooa/logs?take=100
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int take = 100)
    {
        var logs = await _zaloOaService.GetLogsAsync(take);
        return Ok(logs.Select(l => new
        {
            l.Id,
            l.Phone,
            l.MessageType,
            l.TemplateId,
            l.Status,
            l.ZaloMsgId,
            l.ErrorMessage,
            l.CreatedAt,
        }));
    }
}