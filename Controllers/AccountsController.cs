using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// ===== DTOs =====
public class CreateAccountDto
{
    public string TenDangNhap { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
    public string HoTen { get; set; } = null!;
    public string? TrangThai { get; set; } // "1" = hoạt động, "0" = khóa. Mặc định "1"
    public List<int> VaiTroIds { get; set; } = new();
}

public class UpdateAccountDto
{
    public string HoTen { get; set; } = null!;
    public string TrangThai { get; set; } = "1";
    public List<int> VaiTroIds { get; set; } = new();
    public string? MatKhauMoi { get; set; } // để trống nếu không đổi mật khẩu
}

[Route("api/accounts")]
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccountsController(AppDbContext context)
    {
        _context = context;
    }

    // 1. LẤY DANH SÁCH TÀI KHOẢN (kèm vai trò đã gán)
    // GET /api/accounts
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        try
        {
            var allRoles = await _context.VaiTros.ToListAsync();

            var accountsRaw = await _context.NguoiDungs
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    id = u.Id,
                    tenDangNhap = u.TenDangNhap,
                    hoTen = u.HoTen,
                    trangThai = u.TrangThai,
                    vaiTroIds = _context.NguoiDungVaiTros
                        .Where(nv => nv.NguoiDungId == u.Id)
                        .Select(nv => nv.VaiTroId)
                        .ToList()
                })
                .ToListAsync();

            var result = accountsRaw.Select(a => new
            {
                a.id,
                a.tenDangNhap,
                a.hoTen,
                a.trangThai,
                vaiTroIds = a.vaiTroIds,
                vaiTroNames = allRoles.Where(r => a.vaiTroIds.Contains(r.Id))
                                      .Select(r => r.TenVaiTro)
                                      .ToList()
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy danh sách tài khoản: {ex.Message}" });
        }
    }

    // 2. TẠO TÀI KHOẢN MỚI
    // POST /api/accounts
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDangNhap) ||
            string.IsNullOrWhiteSpace(dto.MatKhau) ||
            string.IsNullOrWhiteSpace(dto.HoTen))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ tên đăng nhập, mật khẩu và họ tên." });
        }

        if (dto.MatKhau.Length < 6)
            return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự." });

        try
        {
            string tenChuan = dto.TenDangNhap.Trim();

            bool exists = await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == tenChuan);
            if (exists)
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại." });

            var newUser = new NguoiDung
            {
                TenDangNhap = tenChuan,
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                HoTen = dto.HoTen.Trim(),
                TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? "1" : dto.TrangThai
            };

            _context.NguoiDungs.Add(newUser);
            await _context.SaveChangesAsync();

            if (dto.VaiTroIds.Count > 0)
            {
                var validRoleIds = await _context.VaiTros
                    .Where(v => dto.VaiTroIds.Contains(v.Id))
                    .Select(v => v.Id)
                    .ToListAsync();

                foreach (var roleId in validRoleIds.Distinct())
                {
                    _context.NguoiDungVaiTros.Add(new NguoiDungVaiTro
                    {
                        NguoiDungId = newUser.Id,
                        VaiTroId = roleId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Tạo tài khoản thành công!", id = newUser.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi tạo tài khoản: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 3. CẬP NHẬT TÀI KHOẢN (họ tên, trạng thái, mật khẩu, vai trò)
    // PUT /api/accounts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HoTen))
            return BadRequest(new { message = "Họ tên là bắt buộc." });

        if (!string.IsNullOrWhiteSpace(dto.MatKhauMoi) && dto.MatKhauMoi.Length < 6)
            return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

        try
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            bool isRootAdmin = user.TenDangNhap == "admin";

            if (isRootAdmin && dto.TrangThai != "1")
                return BadRequest(new { message = "Không thể khóa tài khoản admin gốc." });

            user.HoTen = dto.HoTen.Trim();
            user.TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? user.TrangThai : dto.TrangThai;

            if (!string.IsNullOrWhiteSpace(dto.MatKhauMoi))
            {
                user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
            }

            // Lưu thông tin tài khoản (họ tên / trạng thái / mật khẩu) trước
            await _context.SaveChangesAsync();

            // Admin gốc luôn giữ nguyên vai trò hiện có, không cho đổi từ giao diện
            if (!isRootAdmin)
            {
                // Dùng ExecuteDeleteAsync (bulk delete trực tiếp bằng SQL) thay vì
                // RemoveRange + SaveChanges, để tránh lỗi optimistic concurrency.
                await _context.NguoiDungVaiTros.Where(nv => nv.NguoiDungId == id).ExecuteDeleteAsync();

                var validRoleIds = await _context.VaiTros
                    .Where(v => dto.VaiTroIds.Contains(v.Id))
                    .Select(v => v.Id)
                    .ToListAsync();

                foreach (var roleId in validRoleIds.Distinct())
                {
                    _context.NguoiDungVaiTros.Add(new NguoiDungVaiTro
                    {
                        NguoiDungId = id,
                        VaiTroId = roleId
                    });
                }

                if (validRoleIds.Count > 0)
            await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Cập nhật tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi cập nhật tài khoản: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 4. XÓA TÀI KHOẢN
    // DELETE /api/accounts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        try
        {
            var user = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            if (user.TenDangNhap == "admin")
                return BadRequest(new { message = "Không thể xóa tài khoản admin gốc." });

            // Dùng ExecuteDeleteAsync (bulk delete trực tiếp bằng SQL) thay vì
            // RemoveRange/Remove + SaveChanges, để tránh lỗi optimistic concurrency.
            await _context.NguoiDungVaiTros.Where(nv => nv.NguoiDungId == id).ExecuteDeleteAsync();
            int deletedCount = await _context.NguoiDungs.Where(u => u.Id == id).ExecuteDeleteAsync();

            if (deletedCount == 0)
                return NotFound(new { message = "Tài khoản đã bị xóa trước đó hoặc không còn tồn tại." });

            return Ok(new { success = true, message = "Đã xóa tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi xóa tài khoản: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }
}