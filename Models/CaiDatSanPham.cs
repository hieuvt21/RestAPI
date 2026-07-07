using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Bảng chỉ có duy nhất 1 dòng (Id = 1) lưu cấu hình chung cho module Sản phẩm.
[Table("cai_dat_san_pham")]
public class CaiDatSanPham
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // Ngưỡng tồn kho thấp áp dụng chung cho mọi sản phẩm có quan_ly_ton_kho = true
    [Column("nguong_ton_kho_thap")]
    public int NguongTonKhoThap { get; set; } = 10;
}
