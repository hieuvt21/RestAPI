using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================= 1. CẤU HÌNH CORS (CHO PHÉP FLUTTER GỌI API) =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// ================= 2. BỔ SUNG: HỖ TRỢ CONTROLLERS (ĐỂ CHẠY AUTHCONTROLLER) =================
builder.Services.AddControllers();

// ================= 3. BỔ SUNG: CẤU HÌNH EF CORE VỚI DB CHỮ THƯỜNG =================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= 4. BỔ SUNG: CẤU HÌNH XÁC THỰC BẢO MẬT JWT TOKEN =================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "Chuoi_Secret_Key_Bao_Mat_Nhat_Cua_Hieu_POS_App_2026");

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

// Kích hoạt CORS ngay sau khi Build app
app.UseCors("AllowAll");

// Đọc chuỗi kết nối từ file appsettings.json
string connectionString = app.Configuration.GetConnectionString("DefaultConnection")!;

// ================= 5. BỔ SUNG: MIDDLEWARE XÁC THỰC (BẮT BUỘC ĐÚNG THỨ TỰ) =================
app.UseAuthentication(); // Xác thực danh tính (Ai đang vào?)
app.UseAuthorization();  // Kiểm tra quyền hạn (Có được làm không?)

// ================= 6. BỔ SUNG: MAP CONTROLLER ROUTING =================
app.MapControllers(); // Tự động nhận diện API trong AuthController

// ================= GIỮ NGUYÊN CÁC API CŨ CỦA BẠN =================

// 1. ENDPOINT KIỂM TRA TRẠNG THÁI SERVER (GET /)
app.MapGet("/", () => "API .NET 10 đang hoạt động ở cổng 5000!");

// 2. API LẤY DANH SÁCH KHÁCH HÀNG TỪ SQL SERVER (GET /api/khachhang)
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
                ngay_sinh = reader["ngay_sinh"] != DBNull.Value ? Convert.ToDateTime(reader["ngay_sinh"]) : (DateTime?)null,
                ghi_chu = reader["ghi_chu"]?.ToString(),
                trang_thai = reader["trang_thai"]?.ToString(),
                chi_tieu = reader["chi_tieu"] != DBNull.Value ? Convert.ToDecimal(reader["chi_tieu"]) : 0m
            });
        }

        return Results.Ok(danhSach);
    }
    catch (Exception ex)
    {
        return Results.InternalServerError(new { message = ex.Message });
    }
});

// 3. API THÊM MỚI KHÁCH HÀNG VÀO SQL SERVER (POST /api/khachhang)
app.MapPost("/api/khachhang", async (KhachHang model) =>
{
    if (string.IsNullOrWhiteSpace(model.ten))
    {
        return Results.BadRequest(new { message = "Tên khách hàng là bắt buộc." });
    }

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
        cmd.Parameters.AddWithValue("@ngay_sinh", model.ngay_sinh.HasValue ? model.ngay_sinh.Value : (object)DBNull.Value);
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
// ================= TỰ ĐỘNG KHỞI TẠO TÀI KHOẢN ADMIN BẰNG CODE .NET =================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // 1. Nếu chưa có quyền Admin trong DB, tự thêm bằng EF Core
        if (!await context.VaiTros.AnyAsync(v => v.TenVaiTro == "Admin"))
        {
            var adminRole = new VaiTro { TenVaiTro = "Admin", MoTa = "Toàn quyền quản trị" };
            context.VaiTros.Add(adminRole);
            await context.SaveChangesAsync();
        }

        // 2. Nếu chưa có tài khoản 'admin', tự sinh ra qua thư viện BCrypt của .NET
        if (!await context.NguoiDungs.AnyAsync(u => u.TenDangNhap == "admin"))
        {
            // Thư viện .NET tự băm ra chuỗi cực chuẩn bảo mật
            string dynamicHash = BCrypt.Net.BCrypt.HashPassword("123456");

            var adminUser = new NguoiDung
            {
                TenDangNhap = "admin",
                MatKhauHash = dynamicHash,
                HoTen = "Quản Trị Viên",
                TrangThai = "1"
            };

            context.NguoiDungs.Add(adminUser);
            await context.SaveChangesAsync(); // Lưu để lấy ID tự tăng của user

            // 3. Lấy ID của quyền Admin vừa tạo để liên kết vào bảng trung gian
            var role = await context.VaiTros.FirstAsync(v => v.TenVaiTro == "Admin");
            var userRole = new NguoiDungVaiTro
            {
                NguoiDungId = adminUser.Id,
                VaiTroId = role.Id
            };
            context.NguoiDungVaiTros.Add(userRole);
            await context.SaveChangesAsync();

            Console.WriteLine(">>> Đã khởi tạo thành công tài khoản admin/123456 từ Visual Studio!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi khi tự động Seed Data: {ex.Message}");
    }
}

app.Run();

// ================= KHAI BÁO MODEL (RECORD CLASS C# NEW) =================
public record KhachHang(
    string ten,
    string? sdt,
    string? dia_chi,
    DateTime? ngay_sinh,
    string? ghi_chu,
    string? trang_thai,
    decimal? chi_tieu
);