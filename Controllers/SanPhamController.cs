using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/sanpham")]
[ApiController]
public class SanPhamController : ControllerBase
{
    private readonly AppDbContext _context;

    public SanPhamController(AppDbContext context)
    {
        _context = context;
    }

    // 1. LẤY DANH SÁCH SẢN PHẨM (kèm danh mục, biến thể, đơn vị quy đổi)
    // GET /api/sanpham
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var danhMucs = await _context.DanhMucSanPhams.ToListAsync();

            var sanPhams = await _context.SanPhams
                .Where(s => s.TrangThai == "1")
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var sanPhamIds = sanPhams.Select(s => s.Id).ToList();

            var bienThes = await _context.BienTheSanPhams
                .Where(b => sanPhamIds.Contains(b.SanPhamId) && b.TrangThai == "1")
                .ToListAsync();

            var bienTheIds = bienThes.Select(b => b.Id).ToList();

            var donViQuyDois = await _context.DonViQuyDois
                .Where(d => bienTheIds.Contains(d.BienTheSanPhamId) && d.TrangThai == "1")
                .ToListAsync();

            var result = sanPhams.Select(s => new
            {
                id = s.Id,
                tenSanPham = s.TenSanPham,
                danhMucId = s.DanhMucId,
                danhMucTen = danhMucs.FirstOrDefault(d => d.Id == s.DanhMucId)?.TenDanhMuc,
                hinhAnh = s.HinhAnh,
                moTa = s.MoTa,
                coBienThe = s.CoBienThe,
                quanLyTonKho = s.QuanLyTonKho,
                thueSuat = s.ThueSuat,
                trangThai = s.TrangThai,
                bienThe = bienThes
                    .Where(b => b.SanPhamId == s.Id)
                    .Select(b => new
                    {
                        id = b.Id,
                        maSku = b.MaSku,
                        tenBienThe = b.TenBienThe,
                        giaVon = b.GiaVon,
                        giaBanLe = b.GiaBanLe,
                        giaSiLon = b.GiaSiLon,
                        giaSiNho = b.GiaSiNho,
                        giaCtv = b.GiaCtv,
                        tonKho = b.TonKho,
                        hinhAnhRieng = b.HinhAnhRieng,
                        donViQuyDoi = donViQuyDois
                            .Where(d => d.BienTheSanPhamId == b.Id)
                            .Select(d => new
                            {
                                id = d.Id,
                                tenDonVi = d.TenDonVi,
                                soLuongQuyDoi = d.SoLuongQuyDoi,
                                giaBanLe = d.GiaBanLe,
                                giaSiLon = d.GiaSiLon,
                                giaSiNho = d.GiaSiNho,
                                giaCtv = d.GiaCtv,
                                maSkuRieng = d.MaSkuRieng
                            })
                            .ToList()
                    })
                    .ToList()
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi lấy danh sách sản phẩm: {ex.Message}" });
        }
    }

    // 2. TẠO SẢN PHẨM MỚI (kèm toàn bộ biến thể + đơn vị quy đổi)
    // POST /api/sanpham
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSanPhamDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenSanPham))
            return BadRequest(new { message = "Tên sản phẩm là bắt buộc." });

        if (dto.BienThe == null || dto.BienThe.Count == 0)
            return BadRequest(new { message = "Sản phẩm cần ít nhất 1 biến thể (dùng 'Mặc định' nếu không phân loại)." });

        try
        {
            var sanPham = new SanPham
            {
                TenSanPham = dto.TenSanPham.Trim(),
                DanhMucId = dto.DanhMucId,
                HinhAnh = dto.HinhAnh,
                MoTa = dto.MoTa,
                CoBienThe = dto.CoBienThe,
                QuanLyTonKho = dto.QuanLyTonKho,
                ThueSuat = dto.ThueSuat,
                TrangThai = "1"
            };

            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();

            await ThemBienTheVaDonViQuyDoiAsync(sanPham.Id, dto.BienThe, dto.QuanLyTonKho);

            return Ok(new { success = true, message = "Đã tạo sản phẩm thành công!", id = sanPham.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi tạo sản phẩm: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 3. CẬP NHẬT SẢN PHẨM
    // PUT /api/sanpham/{id}
    // Chiến lược: xóa toàn bộ biến thể + đơn vị quy đổi cũ, tạo lại từ danh sách mới gửi lên
    // (đơn giản, nhất quán với cách RolesController xử lý danh sách quyền con).
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSanPhamDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenSanPham))
            return BadRequest(new { message = "Tên sản phẩm là bắt buộc." });

        if (dto.BienThe == null || dto.BienThe.Count == 0)
            return BadRequest(new { message = "Sản phẩm cần ít nhất 1 biến thể (dùng 'Mặc định' nếu không phân loại)." });

        try
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            sanPham.TenSanPham = dto.TenSanPham.Trim();
            sanPham.DanhMucId = dto.DanhMucId;
            sanPham.HinhAnh = dto.HinhAnh;
            sanPham.MoTa = dto.MoTa;
            sanPham.CoBienThe = dto.CoBienThe;
            sanPham.QuanLyTonKho = dto.QuanLyTonKho;
            sanPham.ThueSuat = dto.ThueSuat;
            await _context.SaveChangesAsync();

            var bienTheIdsCu = await _context.BienTheSanPhams
                .Where(b => b.SanPhamId == id)
                .Select(b => b.Id)
                .ToListAsync();

            if (bienTheIdsCu.Count > 0)
            {
                await _context.DonViQuyDois
                    .Where(d => bienTheIdsCu.Contains(d.BienTheSanPhamId))
                    .ExecuteDeleteAsync();
            }
            await _context.BienTheSanPhams.Where(b => b.SanPhamId == id).ExecuteDeleteAsync();

            await ThemBienTheVaDonViQuyDoiAsync(id, dto.BienThe, dto.QuanLyTonKho);

            return Ok(new { success = true, message = "Đã cập nhật sản phẩm thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi cập nhật sản phẩm: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // 4. XÓA (MỀM) SẢN PHẨM — cascade xóa mềm luôn các biến thể con
    // DELETE /api/sanpham/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            sanPham.TrangThai = "0";
            await _context.SaveChangesAsync();

            await _context.BienTheSanPhams
                .Where(b => b.SanPhamId == id)
                .ExecuteUpdateAsync(setter => setter.SetProperty(b => b.TrangThai, "0"));

            return Ok(new { success = true, message = "Đã xóa sản phẩm thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi xóa sản phẩm: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // ===== HÀM PHỤ DÙNG CHUNG CHO CREATE & UPDATE =====
    private async Task ThemBienTheVaDonViQuyDoiAsync(int sanPhamId, List<BienTheDto> bienTheList, bool quanLyTonKho)
    {
        foreach (var bt in bienTheList)
        {
            var bienThe = new BienTheSanPham
            {
                SanPhamId = sanPhamId,
                MaSku = string.IsNullOrWhiteSpace(bt.MaSku) ? null : bt.MaSku.Trim(),
                TenBienThe = string.IsNullOrWhiteSpace(bt.TenBienThe) ? "Mặc định" : bt.TenBienThe.Trim(),
                GiaVon = bt.GiaVon,
                GiaBanLe = bt.GiaBanLe,
                GiaSiLon = bt.GiaSiLon,
                GiaSiNho = bt.GiaSiNho,
                GiaCtv = bt.GiaCtv,
                // Nếu sản phẩm KHÔNG quản lý tồn kho thì luôn lưu 0, tránh dữ liệu ảo gây hiểu nhầm
                TonKho = quanLyTonKho ? bt.TonKho : 0,
                HinhAnhRieng = bt.HinhAnhRieng,
                TrangThai = "1"
            };

            _context.BienTheSanPhams.Add(bienThe);
            await _context.SaveChangesAsync();

            if (bt.DonViQuyDoi != null && bt.DonViQuyDoi.Count > 0)
            {
                foreach (var dv in bt.DonViQuyDoi)
                {
                    if (string.IsNullOrWhiteSpace(dv.TenDonVi) || dv.SoLuongQuyDoi <= 0) continue;

                    _context.DonViQuyDois.Add(new DonViQuyDoi
                    {
                        BienTheSanPhamId = bienThe.Id,
                        TenDonVi = dv.TenDonVi.Trim(),
                        SoLuongQuyDoi = dv.SoLuongQuyDoi,
                        GiaBanLe = dv.GiaBanLe,
                        GiaSiLon = dv.GiaSiLon,
                        GiaSiNho = dv.GiaSiNho,
                        GiaCtv = dv.GiaCtv,
                        MaSkuRieng = string.IsNullOrWhiteSpace(dv.MaSkuRieng) ? null : dv.MaSkuRieng.Trim(),
                        TrangThai = "1"
                    });
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
