using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("vaitro")]
public class VaiTro
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("ten_vai_tro")]
    [StringLength(50)]
    public string TenVaiTro { get; set; } = null!;

    [Column("mo_ta")]
    [StringLength(250)]
    public string? MoTa { get; set; }
}