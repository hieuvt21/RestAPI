using System.ComponentModel.DataAnnotations.Schema;

[Table("nguoidung_vaitro")]
public class NguoiDungVaiTro
{
    [Column("nguoi_dung_id")]
    public int NguoiDungId { get; set; }

    [Column("vai_tro_id")]
    public int VaiTroId { get; set; }
}