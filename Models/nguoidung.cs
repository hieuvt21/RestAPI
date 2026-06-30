using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("nguoidung")]
public class NguoiDung
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("ten_dang_nhap")]
    [StringLength(50)]
    public string TenDangNhap { get; set; } = null!;

    [Required]
    [Column("mat_khau_hash")]
    [StringLength(255)]
    public string MatKhauHash { get; set; } = null!;

    [Required]
    [Column("ho_ten")]
    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    [Required]
    [Column("trang_thai")]
    [StringLength(20)]
    public string TrangThai { get; set; } = "1";
}