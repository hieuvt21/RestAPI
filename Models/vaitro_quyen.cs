using System.ComponentModel.DataAnnotations.Schema;

[Table("vaitro_quyen")]
public class VaiTroQuyen
{
    [Column("vai_tro_id")]
    public int VaiTroId { get; set; }

    [Column("module_code")]
    public string ModuleCode { get; set; } = null!;
}