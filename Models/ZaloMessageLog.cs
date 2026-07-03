using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("zalo_message_log")]
public class ZaloMessageLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // Liên kết mềm tới khách hàng (không ràng buộc FK cứng vì bảng khach_hang
    // hiện đang được thao tác bằng raw SQL trong Program.cs, không phải EF entity)
    [Column("khach_hang_id")]
    public int? KhachHangId { get; set; }

    [Column("phone")]
    [StringLength(50)]
    public string Phone { get; set; } = null!;

    // Loại tin: AppointmentReminder | OrderNotification | TierUpgrade | Test
    [Column("message_type")]
    [StringLength(50)]
    public string MessageType { get; set; } = null!;

    [Column("template_id")]
    [StringLength(50)]
    public string? TemplateId { get; set; }

    // Lưu JSON của template_data đã gửi, phục vụ tra cứu/debug
    [Column("template_data")]
    public string? TemplateDataJson { get; set; }

    // Pending | Success | Failed
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [Column("zalo_msg_id")]
    [StringLength(100)]
    public string? ZaloMsgId { get; set; }

    [Column("error_message")]
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}