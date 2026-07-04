using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("nhanvien")]
public class NhanVien
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("ten_nhan_vien")]
    [StringLength(100)]
    public string TenNhanVien { get; set; } = null!;

    [Column("so_dien_thoai")]
    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [Column("dia_chi")]
    [StringLength(255)]
    public string? DiaChi { get; set; }

    [Column("ngay_bat_dau")]
    public DateTime? NgayBatDau { get; set; }

    [Precision(18, 2)]
    [Column("luong_co_ban")]
    public decimal LuongCoBan { get; set; } = 0;

    [Column("ghi_chu")]
    [StringLength(500)]
    public string? GhiChu { get; set; }

    // "1" = đang làm, "0" = đã nghỉ. Luôn = "1" khi tạo mới, server tự set.
    [Required]
    [Column("trang_thai")]
    [StringLength(5)]
    public string TrangThai { get; set; } = "1";
}