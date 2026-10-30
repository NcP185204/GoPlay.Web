using System.ComponentModel.DataAnnotations;

namespace GoPlay.Web.ViewModels;

// View model cho form đặt sân (POST /Bookings/Create).
// Tách khỏi entity để validation rõ ràng (tương đương BookingRequest bên Java).
public class BookingCreateViewModel
{
    // Danh sách id các khung giờ được chọn. Bắt buộc chọn ít nhất 1 slot (R1).
    [Required(ErrorMessage = "Vui lòng chọn ít nhất một khung giờ")]
    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một khung giờ")]
    public List<int> SlotIds { get; set; } = new();

    // Sân đang đặt — dùng để điều hướng lại trang chi tiết khi có lỗi.
    public int CourtId { get; set; }

    // Ghi chú tuỳ chọn của khách.
    public string? Note { get; set; }
}