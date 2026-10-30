using System.ComponentModel.DataAnnotations;

namespace GoPlay.Web.ViewModels;

// ViewModel cho trang Login. Dùng DataAnnotations để validate
// tương đương jakarta.validation bên Java (tránh validate trực tiếp entity).
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Khi true giữ cookie lâu dài; để đơn giản v1 không dùng nhưng vẫn có sẵn.
    public bool RememberMe { get; set; }
}

// ViewModel cho trang Register (tách khỏi Login để validation riêng).
public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}