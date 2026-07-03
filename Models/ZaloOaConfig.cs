using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Bảng chỉ có duy nhất 1 dòng (Id = 1) lưu cấu hình Zalo OA của cửa hàng.
[Table("zalo_oa_config")]
public class ZaloOaConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // ===== THÔNG TIN ỨNG DỤNG ZALO (lấy tại developers.zalo.me) =====
    [Column("app_id")]
    [StringLength(100)]
    public string? AppId { get; set; }

    [Column("app_secret")]
    [StringLength(255)]
    public string? AppSecret { get; set; }

    [Column("oa_id")]
    [StringLength(100)]
    public string? OaId { get; set; }

    // ===== TOKEN (được hệ thống tự sinh/tự làm mới, KHÔNG cho sửa tay) =====
    [Column("access_token")]
    public string? AccessToken { get; set; }

    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("token_expired_at")]
    public DateTime? TokenExpiredAt { get; set; }

    [Column("last_refresh_error")]
    [StringLength(500)]
    public string? LastRefreshError { get; set; }

    // ===== CỜ BẬT/TẮT TỪNG TÍNH NĂNG TỰ ĐỘNG =====
    [Column("enable_appointment_reminder")]
    public bool EnableAppointmentReminder { get; set; } = false;

    [Column("appointment_reminder_template_id")]
    [StringLength(50)]
    public string? AppointmentReminderTemplateId { get; set; }

    [Column("enable_order_notification")]
    public bool EnableOrderNotification { get; set; } = false;

    [Column("order_notification_template_id")]
    [StringLength(50)]
    public string? OrderNotificationTemplateId { get; set; }

    [Column("enable_tier_upgrade_notification")]
    public bool EnableTierUpgradeNotification { get; set; } = false;

    [Column("tier_upgrade_template_id")]
    [StringLength(50)]
    public string? TierUpgradeTemplateId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
