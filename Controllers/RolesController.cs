using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// ===== DTOs =====
public class CreateRoleDto
{
    public string TenVaiTro { get; set; } = null!;
    public string? MoTa { get; set; }
}

public class UpdateQuyenDto
{
    public List<string> Modules { get; set; } = new();
}

[Route("api/roles")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    // 1. LẤY DANH SÁCH VAI TRÒ (kèm modules đã cấp quyền)
    // GET /api/roles
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var roles = await _context.VaiTros
                .OrderBy(v => v.Id)
                .Select(v => new
                {
                    id = v.Id,
                    tenVaiTro = v.TenVaiTro,
                    moTa = v.MoTa,
                    modules = _context.VaiTroQuyens
                        .Where(q => q.VaiTroId == v.Id)
                        .Select(q => q.ModuleCode)
                        .ToList()
                })
                .ToListAsync();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy danh sách vai trò: {ex.Message}" });
        }
    }

    // 2. TẠO VAI TRÒ MỚI
    // POST /api/roles
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenVaiTro))
            return BadRequest(new { message = "Tên vai trò là bắt buộc." });

        try
        {
            string tenChuan = dto.TenVaiTro.Trim();

            bool exists = await _context.VaiTros
                .AnyAsync(v => v.TenVaiTro.ToLower() == tenChuan.ToLower());
            if (exists)
                return BadRequest(new { message = $"Vai trò '{tenChuan}' đã tồn tại." });

            var role = new VaiTro
            {
                TenVaiTro = tenChuan,
                MoTa = string.IsNullOrWhiteSpace(dto.MoTa) ? null : dto.MoTa.Trim()
            };

            _context.VaiTros.Add(role);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Tạo vai trò thành công!", id = role.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi tạo vai trò: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 3. CẬP NHẬT DANH SÁCH QUYỀN CHO 1 VAI TRÒ
    // PUT /api/roles/{id}/quyen
    [HttpPut("{id}/quyen")]
    public async Task<IActionResult> UpdatePermissions(int id, [FromBody] UpdateQuyenDto dto)
    {
        try
        {
            var role = await _context.VaiTros.FindAsync(id);
            if (role == null)
                return NotFound(new { message = "Không tìm thấy vai trò." });

            if (role.TenVaiTro == "Admin")
                return BadRequest(new { message = "Không thể chỉnh sửa quyền của vai trò Admin." });

            // Xóa toàn bộ quyền cũ của vai trò này
            var existingQuyens = _context.VaiTroQuyens.Where(q => q.VaiTroId == id);
            _context.VaiTroQuyens.RemoveRange(existingQuyens);
            await _context.SaveChangesAsync();

            // Thêm lại danh sách quyền mới (loại trùng)
            var moduleList = dto.Modules?.Distinct().ToList() ?? new List<string>();
            foreach (var moduleCode in moduleList)
            {
                if (string.IsNullOrWhiteSpace(moduleCode)) continue;
                _context.VaiTroQuyens.Add(new VaiTroQuyen
                {
                    VaiTroId = id,
                    ModuleCode = moduleCode
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã lưu phân quyền thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lưu phân quyền: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 4. XÓA VAI TRÒ
    // DELETE /api/roles/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        try
        {
            var role = await _context.VaiTros.FindAsync(id);
            if (role == null)
                return NotFound(new { message = "Không tìm thấy vai trò." });

            if (role.TenVaiTro == "Admin")
                return BadRequest(new { message = "Không thể xóa vai trò Admin." });

            bool dangSuDung = await _context.NguoiDungVaiTros.AnyAsync(nv => nv.VaiTroId == id);
            if (dangSuDung)
                return BadRequest(new { message = "Vai trò đang được gán cho tài khoản, không thể xóa." });

            var quyens = _context.VaiTroQuyens.Where(q => q.VaiTroId == id);
            _context.VaiTroQuyens.RemoveRange(quyens);
            _context.VaiTros.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa vai trò thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi xóa vai trò: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }
}