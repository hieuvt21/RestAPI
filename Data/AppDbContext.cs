using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDungs { get; set; }
    public DbSet<VaiTro> VaiTros { get; set; }
    public DbSet<NguoiDungVaiTro> NguoiDungVaiTros { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Khai báo Composite Primary Key cho bảng trung gian đúng cấu trúc SQL của bạn
        modelBuilder.Entity<NguoiDungVaiTro>()
            .HasKey(nv => new { nv.NguoiDungId, nv.VaiTroId });
    }
}