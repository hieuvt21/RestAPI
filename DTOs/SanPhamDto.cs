// ===== DTO cho Đơn vị quy đổi (bán theo thùng/lốc) =====
public class DonViQuyDoiDto
{
    public int? Id { get; set; } // null = tạo mới
    public string TenDonVi { get; set; } = null!;
    public int SoLuongQuyDoi { get; set; }
    public decimal? GiaBanLe { get; set; }
    public decimal? GiaSiLon { get; set; }
    public decimal? GiaSiNho { get; set; }
    public decimal? GiaCtv { get; set; }
    public string? MaSkuRieng { get; set; }
}

// ===== DTO cho 1 Biến thể sản phẩm =====
public class BienTheDto
{
    public int? Id { get; set; } // null = tạo mới
    public string? MaSku { get; set; }
    public string TenBienThe { get; set; } = "Mặc định";
    public decimal GiaVon { get; set; }
    public decimal GiaBanLe { get; set; }
    public decimal? GiaSiLon { get; set; }
    public decimal? GiaSiNho { get; set; }
    public decimal? GiaCtv { get; set; }
    public int TonKho { get; set; }
    public string? HinhAnhRieng { get; set; }
    public List<DonViQuyDoiDto> DonViQuyDoi { get; set; } = new();
}

// ===== DTO tạo mới Sản phẩm =====
public class CreateSanPhamDto
{
    public string TenSanPham { get; set; } = null!;
    public int? DanhMucId { get; set; }
    public string? HinhAnh { get; set; }
    public string? MoTa { get; set; }
    public bool CoBienThe { get; set; } = false;
    public bool QuanLyTonKho { get; set; } = true;
    public decimal ThueSuat { get; set; } = 0;
    public List<BienTheDto> BienThe { get; set; } = new();
}

// ===== DTO cập nhật Sản phẩm (kế thừa DTO tạo mới) =====
public class UpdateSanPhamDto : CreateSanPhamDto
{
}

// ===== DTO Danh mục sản phẩm =====
public class CreateDanhMucDto
{
    public string TenDanhMuc { get; set; } = null!;
    public string? MoTa { get; set; }
}

// ===== DTO Cài đặt sản phẩm =====
public class UpdateCaiDatSanPhamDto
{
    public int NguongTonKhoThap { get; set; }
}
