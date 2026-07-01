using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // 1. API ĐĂNG KÝ TÀI KHOẢN
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == dto.TenDangNhap))
        {
            return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

        var newUser = new NguoiDung
        {
            TenDangNhap = dto.TenDangNhap,
            MatKhauHash = passwordHash,
            HoTen = dto.HoTen,
            TrangThai = "1"
        };

        _context.NguoiDungs.Add(newUser);
        await _context.SaveChangesAsync();

        var userRole = new NguoiDungVaiTro
        {
            NguoiDungId = newUser.Id,
            VaiTroId = dto.VaiTroId
        };
        _context.NguoiDungVaiTros.Add(userRole);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đăng ký tài khoản thành công!" });
    }

    // 2. API ĐĂNG NHẬP (Xác thực và trả về Token)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.TenDangNhap == dto.TenDangNhap);
        if (user == null || user.TrangThai != "1")
        {
            return Unauthorized(new { message = "Tài khoản không tồn tại hoặc đã bị khóa!" });
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhauHash);
        if (!isPasswordValid)
        {
            return Unauthorized(new { message = "Mật khẩu không chính xác!" });
        }

        var roleIds = await _context.NguoiDungVaiTros
            .Where(ur => ur.NguoiDungId == user.Id)
            .Select(ur => ur.VaiTroId)
            .ToListAsync();

        var roles = await _context.VaiTros
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.TenVaiTro)
            .ToListAsync();

        var token = GenerateJwtToken(user, roles);

        return Ok(new
        {
            token = token,
            user = new { user.Id, user.TenDangNhap, user.HoTen, roles }
        });
    }

    // 3. API THAY ĐỔI THÔNG TIN TÀI KHOẢN
    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _context.NguoiDungs.FindAsync(dto.Id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

        user.HoTen = dto.HoTen;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công.", hoTen = user.HoTen });
    }

    // 4. API ĐỔI MẬT KHẨU
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await _context.NguoiDungs.FindAsync(dto.Id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

        if (!BCrypt.Net.BCrypt.Verify(dto.MatKhauCu, user.MatKhauHash))
            return BadRequest(new { message = "Mật khẩu cũ không đúng." });

        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    // HÀM BỔ TRỢ TẠO JWT TOKEN
    private string GenerateJwtToken(NguoiDung user, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.TenDangNhap),
            new Claim("HoTen", user.HoTen)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "Key_Bi_Mat_Sieu_Dai_Toi_Thieu_32_Ky_Tu_Nhe_Ban"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}