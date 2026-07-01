using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDungs { get; set; }
    public DbSet<VaiTro> VaiTros { get; set; }
    public DbSet<NguoiDungVaiTro> NguoiDungVaiTros { get; set; }
    public DbSet<VaiTroQuyen> VaiTroQuyens { get; set; } // <-- THÊM DÒNG NÀY

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<VaiTroQuyen>()
        .HasKey(vq => new { vq.VaiTroId, vq.ModuleCode });
        modelBuilder.Entity<NguoiDungVaiTro>()
        .HasKey(nv => new { nv.NguoiDungId, nv.VaiTroId });
        modelBuilder.Entity<NguoiDungVaiTro>()
            .HasKey(nv => new { nv.NguoiDungId, nv.VaiTroId });

        // THÊM: Composite key cho VaiTroQuyen
        modelBuilder.Entity<VaiTroQuyen>()
            .HasKey(vq => new { vq.VaiTroId, vq.ModuleCode });
    }

}