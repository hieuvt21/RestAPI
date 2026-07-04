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

public class UpdateProfileDto
{
    public int Id { get; set; }
    public string HoTen { get; set; } = null!;
}

public class ChangePasswordDto
{
    public int Id { get; set; }
    public string MatKhauCu { get; set; } = null!;
    public string MatKhauMoi { get; set; } = null!;
}
public class VerifyAdminPasswordDto
{
    public string MatKhau { get; set; } = null!;
}