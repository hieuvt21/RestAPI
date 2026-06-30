public class RegisterDto
{
    public string TenDangNhap { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
    public string HoTen { get; set; } = null!;
    public int VaiTroId { get; set; } // 1: Admin, 2: Staff...
}

public class LoginDto
{
    public string TenDangNhap { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
}