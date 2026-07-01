using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(
    jwtSettings["Key"] ?? "Chuoi_Secret_Key_Bao_Mat_Nhat_Cua_Hieu_POS_App_2026");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

app.UseCors("AllowAll");

string connectionString = app.Configuration.GetConnectionString("DefaultConnection")!;

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ===== ENDPOINT KIỂM TRA SERVER =====
app.MapGet("/", () => "API .NET 10 đang hoạt động ở cổng 5000!");

// ===== API KHÁCH HÀNG (GIỮ NGUYÊN) =====
app.MapGet("/api/khachhang", async () =>
{
    try
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = "SELECT id, ten, sdt, dia_chi, ngay_sinh, ghi_chu, trang_thai, chi_tieu FROM khach_hang ORDER BY id DESC";
        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        var danhSach = new List<object>();
        while (await reader.ReadAsync())
        {
            danhSach.Add(new
            {
                id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                ten = reader["ten"]?.ToString(),
                sdt = reader["sdt"]?.ToString(),
                dia_chi = reader["dia_chi"]?.ToString(),
                ngay_sinh = reader["ngay_sinh"] != DBNull.Value
                                ? Convert.ToDateTime(reader["ngay_sinh"]) : (DateTime?)null,
                ghi_chu = reader["ghi_chu"]?.ToString(),
                trang_thai = reader["trang_thai"]?.ToString(),
                chi_tieu = reader["chi_tieu"] != DBNull.Value
                                ? Convert.ToDecimal(reader["chi_tieu"]) : 0m
            });
        }
        return Results.Ok(danhSach);
    }
    catch (Exception ex)
    {
        return Results.InternalServerError(new { message = ex.Message });
    }
});

app.MapPost("/api/khachhang", async (KhachHang model) =>
{
    if (string.IsNullOrWhiteSpace(model.ten))
        return Results.BadRequest(new { message = "Tên khách hàng là bắt buộc." });

    try
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = @"INSERT INTO khach_hang (ten, sdt, dia_chi, ngay_sinh, ghi_chu, trang_thai)
                       VALUES (@ten, @sdt, @dia_chi, @ngay_sinh, @ghi_chu, @trang_thai)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ten", model.ten);
        cmd.Parameters.AddWithValue("@sdt", model.sdt ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dia_chi", model.dia_chi ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ngay_sinh", model.ngay_sinh.HasValue
                                                    ? model.ngay_sinh.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ghi_chu", model.ghi_chu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@trang_thai", model.trang_thai ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
        return Results.Ok(new { success = true, message = "Thêm khách hàng thành công!" });
    }
    catch (Exception ex)
    {
        return Results.InternalServerError(new { message = ex.Message });
    }
});

// ===== SEED DATA =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // --- Seed vai trò Admin ---
        if (!await context.VaiTros.AnyAsync(v => v.TenVaiTro == "Admin"))
        {
            var adminRole = new VaiTro { TenVaiTro = "Admin", MoTa = "Toàn quyền quản trị" };
            context.VaiTros.Add(adminRole);
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Đã tạo vai trò Admin");
        }

        // --- Seed tài khoản admin ---
        if (!await context.NguoiDungs.AnyAsync(u => u.TenDangNhap == "admin"))
        {
            var adminUser = new NguoiDung
            {
                TenDangNhap = "admin",
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                HoTen = "Quản Trị Viên",
                TrangThai = "1"
            };
            context.NguoiDungs.Add(adminUser);
            await context.SaveChangesAsync();

            var adminRole = await context.VaiTros.FirstAsync(v => v.TenVaiTro == "Admin");
            context.NguoiDungVaiTros.Add(new NguoiDungVaiTro
            {
                NguoiDungId = adminUser.Id,
                VaiTroId = adminRole.Id
            });
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Đã tạo tài khoản admin/123456");
        }

        // --- Seed 3 vai trò mặc định ---
        var defaultRoles = new[]
        {
            new
            {
                Name = "Quản lý",
                Desc = "Toàn quyền quản lý hệ thống trừ cài đặt hệ thống",
                Permissions = new[]
                {
                    "dashboard.view",
                    "sales.view", "sales.create",
                    "orders.view", "orders.create", "orders.edit", "orders.delete",
                    "products.view", "products.create", "products.edit", "products.delete",
                    "services.view", "services.create", "services.edit", "services.delete",
                    "customers.view", "customers.create", "customers.edit", "customers.delete",
                    "reports.view", "reports.export"
                }
            },
            new
            {
                Name = "Bán hàng",
                Desc = "Thực hiện bán hàng và quản lý khách hàng cơ bản",
                Permissions = new[]
                {
                    "dashboard.view",
                    "sales.view", "sales.create",
                    "orders.view", "orders.create",
                    "customers.view", "customers.create"
                }
            },
            new
            {
                Name = "Quản lý kho",
                Desc = "Quản lý sản phẩm, dịch vụ và tồn kho",
                Permissions = new[]
                {
                    "dashboard.view",
                    "products.view", "products.create", "products.edit", "products.delete",
                    "services.view", "services.create", "services.edit", "services.delete",
                    "reports.view"
                }
            },
        };

        foreach (var r in defaultRoles)
        {
            if (!await context.VaiTros.AnyAsync(v => v.TenVaiTro == r.Name))
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
                Console.WriteLine($">>> Đã tạo vai trò: {r.Name} ({r.Permissions.Length} quyền)");
            }
            else
            {
                Console.WriteLine($">>> Vai trò '{r.Name}' đã tồn tại, bỏ qua.");
            }
        }
    }
    catch (Exception ex)
    {
        // In chi tiết lỗi để dễ debug
        Console.WriteLine($"[LỖI SEED] {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"[LỖI SEED - Inner] {ex.InnerException.Message}");
    }
}

app.Run();

// ===== MODEL RECORD =====
public record KhachHang(
    string ten,
    string? sdt,
    string? dia_chi,
    DateTime? ngay_sinh,
    string? ghi_chu,
    string? trang_thai,
    decimal? chi_tieu
);