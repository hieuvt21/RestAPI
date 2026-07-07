using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/caidatsanpham")]
[ApiController]
public class CaiDatSanPhamController : ControllerBase
{
    private readonly AppDbContext _context;

    public CaiDatSanPhamController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/caidatsanpham
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var cfg = await _context.CaiDatSanPhams.FirstOrDefaultAsync(c => c.Id == 1);
            int nguong = cfg?.NguongTonKhoThap ?? 10;
            return Ok(new { nguongTonKhoThap = nguong });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy cài đặt sản phẩm: {ex.Message}" });
        }
    }

    // PUT /api/caidatsanpham
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCaiDatSanPhamDto dto)
    {
        if (dto.NguongTonKhoThap < 0)
            return BadRequest(new { message = "Ngưỡng tồn kho thấp không thể là số âm." });

        try
        {
            var cfg = await _context.CaiDatSanPhams.FirstOrDefaultAsync(c => c.Id == 1);
            bool isNew = cfg == null;
            cfg ??= new CaiDatSanPham { Id = 1 };
            cfg.NguongTonKhoThap = dto.NguongTonKhoThap;

            if (isNew) _context.CaiDatSanPhams.Add(cfg);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã lưu cài đặt sản phẩm!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lưu cài đặt: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }
}
