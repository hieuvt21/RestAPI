using Microsoft.EntityFrameworkCore;

public static class DbInitializer
{
    public static async Task SeedDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // --- Seed vai trò Admin ---
        try
        {
            if (!await context.VaiTros.AnyAsync(v => v.TenVaiTro == "Admin"))
            {
                var adminRoleSeed = new VaiTro { TenVaiTro = "Admin", MoTa = "Toàn quyền quản trị" };
                context.VaiTros.Add(adminRoleSeed);
                await context.SaveChangesAsync();
                Console.WriteLine(">>> Đã tạo vai trò Admin");
            }
        }
        catch (Exception ex) { Console.WriteLine($"[LỖI] Admin role: {ex.InnerException?.Message ?? ex.Message}"); }

        // --- Seed tài khoản admin (chỉ tạo mới nếu chưa tồn tại) ---
        NguoiDung? adminUser = null;
        try
        {
            adminUser = await context.NguoiDungs.FirstOrDefaultAsync(u => u.TenDangNhap == "admin");

            if (adminUser == null)
            {
                adminUser = new NguoiDung
                {
                    TenDangNhap = "admin",
                    MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    HoTen = "Quản Trị Viên",
                    TrangThai = "1"
                };
                context.NguoiDungs.Add(adminUser);
                await context.SaveChangesAsync();
                Console.WriteLine(">>> Đã tạo tài khoản admin/123456");
            }
        }
        catch (Exception ex) { Console.WriteLine($"[LỖI] Admin user: {ex.InnerException?.Message ?? ex.Message}"); }

        // --- QUAN TRỌNG: Luôn đảm bảo tài khoản admin được gán vai trò Admin ---
        // Chạy ở MỌI lần khởi động server (không chỉ lúc tạo mới), để tự sửa nếu
        // liên kết nguoidung_vaitro của admin bị thiếu/mất vì bất kỳ lý do gì.
        try
        {
            if (adminUser != null)
            {
                var adminRole = await context.VaiTros.FirstOrDefaultAsync(v => v.TenVaiTro == "Admin");
                if (adminRole != null)
                {
                    bool daGanVaiTro = await context.NguoiDungVaiTros
                        .AnyAsync(nv => nv.NguoiDungId == adminUser.Id && nv.VaiTroId == adminRole.Id);

                    if (!daGanVaiTro)
                    {
                        context.NguoiDungVaiTros.Add(new NguoiDungVaiTro
                        {
                            NguoiDungId = adminUser.Id,
                            VaiTroId = adminRole.Id
                        });
                        await context.SaveChangesAsync();
                        Console.WriteLine(">>> Đã tự động gán lại vai trò Admin cho tài khoản admin (dữ liệu bị thiếu trước đó)");
                    }
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[LỖI] Gán vai trò Admin cho admin: {ex.InnerException?.Message ?? ex.Message}"); }

        // --- Seed 3 vai trò mặc định ---
        var defaultRoles = new[]
        {
            new { Name = "Quản lý", Desc = "Toàn quyền quản lý hệ thống trừ cài đặt hệ thống", Permissions = new[] { "dashboard.view", "sales.view", "sales.create", "orders.view", "orders.create", "orders.edit", "orders.delete", "products.view", "products.create", "products.edit", "products.delete", "services.view", "services.create", "services.edit", "services.delete", "customers.view", "customers.create", "customers.edit", "customers.delete", "reports.view", "reports.export" } },
            new { Name = "Bán hàng", Desc = "Thực hiện bán hàng và quản lý khách hàng cơ bản", Permissions = new[] { "dashboard.view", "sales.view", "sales.create", "orders.view", "orders.create", "customers.view", "customers.create" } },
            new { Name = "Quản lý kho", Desc = "Quản lý sản phẩm, dịch vụ và tồn kho", Permissions = new[] { "dashboard.view", "products.view", "products.create", "products.edit", "products.delete", "services.view", "services.create", "services.edit", "services.delete", "reports.view" } },
        };

        foreach (var r in defaultRoles)
        {
            try
            {
                // Sử dụng EF.Functions.Like để tìm kiếm chính xác và bỏ qua khoảng trắng/dấu tùy cấu hình
                // Hoặc so sánh chữ thường .ToLower() để quét triệt để hơn
                var exists = await context.VaiTros
                    .AnyAsync(v => v.TenVaiTro.ToLower() == r.Name.ToLower());

                if (!exists)
                {
                    var newRole = new VaiTro { TenVaiTro = r.Name, MoTa = r.Desc };
                    context.VaiTros.Add(newRole);
                    await context.SaveChangesAsync();

                    foreach (var p in r.Permissions)
                    {
                        context.VaiTroQuyens.Add(new VaiTroQuyen
                        {
                            VaiTroId = newRole.Id,
                            ModuleCode = p
                        });
                    }
                    await context.SaveChangesAsync();
                    Console.WriteLine($">>> Đã tạo vai trò: {r.Name}");
                }
                else
                {
                    Console.WriteLine($">>> Vai trò '{r.Name}' đã tồn tại, bỏ qua.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI] Vai trò '{r.Name}': {ex.InnerException?.Message ?? ex.Message}");
                // Đoạn này bạn đã viết rất tốt: xóa các thực thể lỗi để vòng lặp sau không bị nghẽn
                context.ChangeTracker.Clear();
            }
        }
    }
}