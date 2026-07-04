using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class CreateNhanVienDto
{
    public string TenNhanVien { get; set; } = null!;
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public decimal LuongCoBan { get; set; }
    public string? GhiChu { get; set; }
}
public class UpdateNhanVienDto
{
    public string TenNhanVien { get; set; } = null!;
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public decimal LuongCoBan { get; set; }
    public string? GhiChu { get; set; }
}
[Route("api/nhanvien")]
[ApiController]
public class NhanVienController : ControllerBase
{
    private readonly AppDbContext _context;

    public NhanVienController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/nhanvien
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _context.NhanViens
                .OrderByDescending(n => n.Id)
                .Select(n => new
                {
                    id = n.Id,
                    tenNhanVien = n.TenNhanVien,
                    soDienThoai = n.SoDienThoai,
                    diaChi = n.DiaChi,
                    ngayBatDau = n.NgayBatDau,
                    luongCoBan = n.LuongCoBan,
                    ghiChu = n.GhiChu,
                    trangThai = n.TrangThai
                })
                .ToListAsync();

            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy danh sách nhân viên: {ex.Message}" });
        }
    }
    // PUT /api/nhanvien/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNhanVienDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenNhanVien))
            return BadRequest(new { message = "Tên nhân viên là bắt buộc." });

        try
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null)
                return NotFound(new { message = "Không tìm thấy nhân viên." });

            nv.TenNhanVien = dto.TenNhanVien.Trim();
            nv.SoDienThoai = string.IsNullOrWhiteSpace(dto.SoDienThoai) ? null : dto.SoDienThoai.Trim();
            nv.DiaChi = string.IsNullOrWhiteSpace(dto.DiaChi) ? null : dto.DiaChi.Trim();
            nv.NgayBatDau = dto.NgayBatDau;
            nv.LuongCoBan = dto.LuongCoBan;
            nv.GhiChu = string.IsNullOrWhiteSpace(dto.GhiChu) ? null : dto.GhiChu.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật nhân viên thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi cập nhật nhân viên: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // DELETE /api/nhanvien/{id}
    // Xóa MỀM: chỉ chuyển trạng thái về "0" (đã nghỉ), không xóa vật lý,
    // để sau này vẫn tra cứu được lịch sử bán hàng/hoa hồng của nhân viên cũ.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null)
                return NotFound(new { message = "Không tìm thấy nhân viên." });

            nv.TrangThai = "0";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa nhân viên khỏi danh sách hoạt động!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi xóa nhân viên: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }
    // POST /api/nhanvien
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNhanVienDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenNhanVien))
            return BadRequest(new { message = "Tên nhân viên là bắt buộc." });

        try
        {
            var nv = new NhanVien
            {
                TenNhanVien = dto.TenNhanVien.Trim(),
                SoDienThoai = string.IsNullOrWhiteSpace(dto.SoDienThoai) ? null : dto.SoDienThoai.Trim(),
                DiaChi = string.IsNullOrWhiteSpace(dto.DiaChi) ? null : dto.DiaChi.Trim(),
                NgayBatDau = dto.NgayBatDau,
                LuongCoBan = dto.LuongCoBan,
                GhiChu = string.IsNullOrWhiteSpace(dto.GhiChu) ? null : dto.GhiChu.Trim(),
                TrangThai = "1" // luôn mặc định khi tạo, không nhận từ client
            };

            _context.NhanViens.Add(nv);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Thêm nhân viên thành công!", id = nv.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi thêm nhân viên: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }
}