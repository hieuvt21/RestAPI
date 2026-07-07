using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("san_pham")]
public class SanPham
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("ten_san_pham")]
    [StringLength(200)]
    public string TenSanPham { get; set; } = null!;

    [Column("danh_muc_id")]
    public int? DanhMucId { get; set; }

    [Column("hinh_anh")]
    [StringLength(500)]
    public string? HinhAnh { get; set; }

    [Column("mo_ta")]
    [StringLength(500)]
    public string? MoTa { get; set; }

    // true = sản phẩm có nhiều biến thể (Size/Màu...); false = chỉ 1 biến thể "Mặc định"
    [Column("co_bien_the")]
    public bool CoBienThe { get; set; } = false;

    // true = có theo dõi tồn kho (nhập/xuất/trừ kho); false = bỏ qua tồn kho hoàn toàn
    [Column("quan_ly_ton_kho")]
    public bool QuanLyTonKho { get; set; } = true;

    // % thuế áp dụng cho MỌI biến thể của sản phẩm này (VD 10.00, 8.00, 0)
    [Precision(5, 2)]
    [Column("thue_suat")]
    public decimal ThueSuat { get; set; } = 0;

    // "1" đang bán / "0" ngừng bán (xóa mềm)
    [Required]
    [Column("trang_thai")]
    [StringLength(5)]
    public string TrangThai { get; set; } = "1";
}
