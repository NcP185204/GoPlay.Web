using System.ComponentModel.DataAnnotations;
using GoPlay.Web.Enums;

namespace GoPlay.Web.ViewModels;

// View model cho form tạo/sửa sân trong trang quản trị.
// Dùng DataAnnotations để validate tương đương jakarta.validation bên Java.
public class CourtFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sân.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    public string Address { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại sân.")]
    public SportType CourtType { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá thuê.")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá thuê phải lớn hơn hoặc bằng 0.")]
    public decimal PricePerHour { get; set; }

    // Ảnh đại diện dạng URL (không bắt buộc).
    public string? ThumbnailUrl { get; set; }
}