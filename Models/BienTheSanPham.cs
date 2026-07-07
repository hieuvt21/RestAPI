using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("bien_the_san_pham")]
public class BienTheSanPham
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("san_pham_id")]
    public int SanPhamId { get; set; }

    [Column("ma_sku")]
    [StringLength(50)]
    public string? MaSku { get; set; }

    // "Mặc định" nếu sản phẩm không có biến thể, ngược lại VD "Size M - Đỏ"
    [Required]
    [Column("ten_bien_the")]
    [StringLength(100)]
    public string TenBienThe { get; set; } = "Mặc định";

    [Precision(18, 2)]
    [Column("gia_von")]
    public decimal GiaVon { get; set; } = 0;

    [Precision(18, 2)]
    [Column("gia_ban_le")]
    public decimal GiaBanLe { get; set; } = 0;

    [Precision(18, 2)]
    [Column("gia_si_lon")]
    public decimal? GiaSiLon { get; set; }

    [Precision(18, 2)]
    [Column("gia_si_nho")]
    public decimal? GiaSiNho { get; set; }

    [Precision(18, 2)]
    [Column("gia_ctv")]
    public decimal? GiaCtv { get; set; }

    // Chỉ có ý nghĩa khi SanPham.QuanLyTonKho = true
    [Column("ton_kho")]
    public int TonKho { get; set; } = 0;

    [Column("hinh_anh_rieng")]
    [StringLength(500)]
    public string? HinhAnhRieng { get; set; }

    [Required]
    [Column("trang_thai")]
    [StringLength(5)]
    public string TrangThai { get; set; } = "1";
}
