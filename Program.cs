using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddScoped<IZaloOaService, ZaloOaService>();
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

// ===== API KHÁCH HÀNG =====
app.MapGet("/api/khachhang", async () =>
{
    try
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = "SELECT id, ten, sdt, dia_chi, ngay_sinh, ghi_chu, trang_thai, chi_tieu FROM khach_hang WHERE trang_thai = '1' ORDER BY id DESC";
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

        // Trạng thái luôn được server đặt cứng là "1" (Hoạt động) khi tạo mới,
        // không phụ thuộc vào giá trị client gửi lên.
        string sql = @"INSERT INTO khach_hang (ten, sdt, dia_chi, ngay_sinh, ghi_chu, trang_thai)
                       VALUES (@ten, @sdt, @dia_chi, @ngay_sinh, @ghi_chu, '1')";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ten", model.ten);
        cmd.Parameters.AddWithValue("@sdt", model.sdt ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dia_chi", model.dia_chi ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ngay_sinh", model.ngay_sinh.HasValue
                                                    ? model.ngay_sinh.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ghi_chu", model.ghi_chu ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
        return Results.Ok(new { success = true, message = "Thêm khách hàng thành công!" });
    }
    catch (Exception ex)
    {
        return Results.InternalServerError(new { message = ex.Message });
    }
});

// ===== API SỬA THÔNG TIN KHÁCH HÀNG =====
app.MapPut("/api/khachhang/{id:int}", async (int id, UpdateKhachHangDto model) =>
{
    if (string.IsNullOrWhiteSpace(model.ten))
        return Results.BadRequest(new { message = "Tên khách hàng là bắt buộc." });

    try
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = @"UPDATE khach_hang
                       SET ten = @ten, sdt = @sdt, dia_chi = @dia_chi,
                           ngay_sinh = @ngay_sinh, ghi_chu = @ghi_chu
                       WHERE id = @id AND trang_thai = '1'";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@ten", model.ten);
        cmd.Parameters.AddWithValue("@sdt", model.sdt ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dia_chi", model.dia_chi ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ngay_sinh", model.ngay_sinh.HasValue
                                                    ? model.ngay_sinh.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ghi_chu", model.ghi_chu ?? (object)DBNull.Value);

        int affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return Results.NotFound(new { message = "Không tìm thấy khách hàng." });

        return Results.Ok(new { success = true, message = "Cập nhật khách hàng thành công!" });
    }
    catch (Exception ex)
    {
        return Results.InternalServerError(new { message = ex.Message });
    }
});

// ===== API XÓA (MỀM) KHÁCH HÀNG =====
// Không xóa vật lý khỏi DB, chỉ chuyển trang_thai về '0' để ẩn khỏi danh sách.
app.MapDelete("/api/khachhang/{id:int}", async (int id) =>
{
    try
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = "UPDATE khach_hang SET trang_thai = '0' WHERE id = @id AND trang_thai = '1'";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        int affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return Results.NotFound(new { message = "Không tìm thấy khách hàng hoặc đã bị xóa trước đó." });

        return Results.Ok(new { success = true, message = "Đã xóa khách hàng thành công!" });
    }
    catch (Exception ex)
    {
        return Results.InternalServerError(new { message = ex.Message });
    }
});

// Ghi chú: API vai trò/phân quyền đã được chuyển sang Controllers/RolesController.cs
// (đường dẫn /api/roles) để khớp với giao diện Flutter (vai_tro_sub.dart).

await DbInitializer.SeedDataAsync(app);

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

public record UpdateKhachHangDto(
    string ten,
    string? sdt,
    string? dia_chi,
    DateTime? ngay_sinh,
    string? ghi_chu
);