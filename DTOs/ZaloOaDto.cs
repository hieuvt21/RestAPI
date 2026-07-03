// ===== DTO nhận từ Flutter khi lưu cấu hình =====
public class UpdateZaloOaConfigDto
{
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? OaId { get; set; }
    public string? RefreshToken { get; set; } // Nhập 1 lần đầu để lấy access token lần đầu

    public bool EnableAppointmentReminder { get; set; }
    public string? AppointmentReminderTemplateId { get; set; }

    public bool EnableOrderNotification { get; set; }
    public string? OrderNotificationTemplateId { get; set; }

    public bool EnableTierUpgradeNotification { get; set; }
    public string? TierUpgradeTemplateId { get; set; }
}

// ===== DTO trả về Flutter (KHÔNG bao giờ trả AppSecret/RefreshToken/AccessToken đầy đủ) =====
public class ZaloOaConfigViewDto
{
    public bool IsConfigured { get; set; }
    public string? AppId { get; set; }
    public string? OaId { get; set; }
    public string? AppSecretMasked { get; set; }
    public bool HasValidToken { get; set; }
    public DateTime? TokenExpiredAt { get; set; }
    public string? LastRefreshError { get; set; }

    public bool EnableAppointmentReminder { get; set; }
    public string? AppointmentReminderTemplateId { get; set; }

    public bool EnableOrderNotification { get; set; }
    public string? OrderNotificationTemplateId { get; set; }

    public bool EnableTierUpgradeNotification { get; set; }
    public string? TierUpgradeTemplateId { get; set; }
}

// ===== DTO gửi tin thử =====
public class SendTestZnsDto
{
    public string Phone { get; set; } = null!;
    public string TemplateId { get; set; } = null!;
    public Dictionary<string, string> TemplateData { get; set; } = new();
}

// ===== Kết quả nội bộ khi gọi service gửi ZNS =====
public class ZnsSendResult
{
    public bool Success { get; set; }
    public string? ZaloMsgId { get; set; }
    public string? ErrorMessage { get; set; }
}

// ===== DTO gửi tin nhắn OA thường (text tự do, KHÔNG cần Template ZNS) =====
// Chỉ gửi được tới user_id đang follow OA và còn trong khung 48h tương tác.
public class SendTestOaMessageDto
{
    public string UserId { get; set; } = null!;
    public string Text { get; set; } = null!;
}

// ===== DTO 1 người đang quan tâm (follower) OA, dùng để chọn khi gửi thử =====
public class ZaloFollowerDto
{
    public string UserId { get; set; } = null!;
    public string? DisplayName { get; set; }
}