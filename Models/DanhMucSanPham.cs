using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("danh_muc_san_pham")]
public class DanhMucSanPham
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("ten_danh_muc")]
    [StringLength(100)]
    public string TenDanhMuc { get; set; } = null!;

    [Column("mo_ta")]
    [StringLength(250)]
    public string? MoTa { get; set; }

    [Required]
    [Column("trang_thai")]
    [StringLength(5)]
    public string TrangThai { get; set; } = "1";
}
