using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("don_vi_quy_doi")]
public class DonViQuyDoi
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // Biến thể gốc (đơn vị nhỏ nhất), VD biến thể "Chiếc"
    [Column("bien_the_san_pham_id")]
    public int BienTheSanPhamId { get; set; }

    // VD: "Thùng", "Lốc"
    [Required]
    [Column("ten_don_vi")]
    [StringLength(50)]
    public string TenDonVi { get; set; } = null!;

    // VD: 12 (1 thùng = 12 đơn vị gốc). Khi bán sẽ trừ TonKho của biến thể gốc theo số này.
    [Column("so_luong_quy_doi")]
    public int SoLuongQuyDoi { get; set; }

    [Precision(18, 2)]
    [Column("gia_ban_le")]
    public decimal? GiaBanLe { get; set; }

    [Precision(18, 2)]
    [Column("gia_si_lon")]
    public decimal? GiaSiLon { get; set; }

    [Precision(18, 2)]
    [Column("gia_si_nho")]
    public decimal? GiaSiNho { get; set; }

    [Precision(18, 2)]
    [Column("gia_ctv")]
    public decimal? GiaCtv { get; set; }

    [Column("ma_sku_rieng")]
    [StringLength(50)]
    public string? MaSkuRieng { get; set; }

    [Required]
    [Column("trang_thai")]
    [StringLength(5)]
    public string TrangThai { get; set; } = "1";
}
