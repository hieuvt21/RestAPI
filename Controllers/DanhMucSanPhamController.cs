using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/danhmucsanpham")]
[ApiController]
public class DanhMucSanPhamController : ControllerBase
{
    private readonly AppDbContext _context;

    public DanhMucSanPhamController(AppDbContext context)
    {
        _context = context;
    }

    // 1. LẤY DANH SÁCH DANH MỤC (kèm số lượng sản phẩm đang dùng)
    // GET /api/danhmucsanpham
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _context.DanhMucSanPhams
                .Where(d => d.TrangThai == "1")
                .OrderBy(d => d.Id)
                .ToListAsync();

            var soLuongTheoDanhMuc = await _context.SanPhams
                .Where(s => s.TrangThai == "1" && s.DanhMucId != null)
                .GroupBy(s => s.DanhMucId)
                .Select(g => new { DanhMucId = g.Key, SoLuong = g.Count() })
                .ToListAsync();

            var result = list.Select(d => new
            {
                id = d.Id,
                tenDanhMuc = d.TenDanhMuc,
                moTa = d.MoTa,
                soLuongSanPham = soLuongTheoDanhMuc.FirstOrDefault(x => x.DanhMucId == d.Id)?.SoLuong ?? 0
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy danh mục sản phẩm: {ex.Message}" });
        }
    }

    // 2. TẠO DANH MỤC MỚI
    // POST /api/danhmucsanpham
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDanhMucDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDanhMuc))
            return BadRequest(new { message = "Tên danh mục là bắt buộc." });

        try
        {
            string tenChuan = dto.TenDanhMuc.Trim();

            bool exists = await _context.DanhMucSanPhams
                .AnyAsync(d => d.TrangThai == "1" && d.TenDanhMuc.ToLower() == tenChuan.ToLower());
            if (exists)
                return BadRequest(new { message = $"Danh mục '{tenChuan}' đã tồn tại." });

            var danhMuc = new DanhMucSanPham
            {
                TenDanhMuc = tenChuan,
                MoTa = string.IsNullOrWhiteSpace(dto.MoTa) ? null : dto.MoTa.Trim(),
                TrangThai = "1"
            };

            _context.DanhMucSanPhams.Add(danhMuc);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã tạo danh mục thành công!", id = danhMuc.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi tạo danh mục: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 3. CẬP NHẬT DANH MỤC
    // PUT /api/danhmucsanpham/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateDanhMucDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDanhMuc))
            return BadRequest(new { message = "Tên danh mục là bắt buộc." });

        try
        {
            var danhMuc = await _context.DanhMucSanPhams.FindAsync(id);
            if (danhMuc == null)
                return NotFound(new { message = "Không tìm thấy danh mục." });

            string tenChuan = dto.TenDanhMuc.Trim();
            bool trung = await _context.DanhMucSanPhams
                .AnyAsync(d => d.TrangThai == "1" && d.Id != id && d.TenDanhMuc.ToLower() == tenChuan.ToLower());
            if (trung)
                return BadRequest(new { message = $"Danh mục '{tenChuan}' đã tồn tại." });

            danhMuc.TenDanhMuc = tenChuan;
            danhMuc.MoTa = string.IsNullOrWhiteSpace(dto.MoTa) ? null : dto.MoTa.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã cập nhật danh mục thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi cập nhật danh mục: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 4. XÓA (MỀM) DANH MỤC — chặn nếu còn sản phẩm đang dùng
    // DELETE /api/danhmucsanpham/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var danhMuc = await _context.DanhMucSanPhams.FindAsync(id);
            if (danhMuc == null)
                return NotFound(new { message = "Không tìm thấy danh mục." });

            bool dangSuDung = await _context.SanPhams
                .AnyAsync(s => s.DanhMucId == id && s.TrangThai == "1");
            if (dangSuDung)
                return BadRequest(new { message = "Danh mục đang có sản phẩm sử dụng, không thể xóa." });

            danhMuc.TrangThai = "0";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa danh mục thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi xóa danh mục: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }
}
