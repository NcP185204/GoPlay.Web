using System.ComponentModel.DataAnnotations;
using GoPlay.Web.Models;

namespace GoPlay.Web.ViewModels;

// View model cho form sinh khung giờ của Admin (Task 14).
// Validation tương đương GenerateTimeSlotRequest bên Java (SPEC mục 6).
public class GenerateSlotsViewModel
{
    // Sân cần sinh slot (chọn từ danh sách).
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sân.")]
    public int CourtId { get; set; }

    // Ngày bắt đầu sinh slot.
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateTime? StartDate { get; set; }

    // Số ngày cần sinh (>= 1).
    [Range(1, int.MaxValue, ErrorMessage = "Số ngày phải lớn hơn hoặc bằng 1.")]
    public int NumberOfDays { get; set; } = 1;

    // Giờ mở cửa (định dạng HH:mm).
    [Required(ErrorMessage = "Vui lòng nhập giờ mở cửa.")]
    public TimeSpan? OpenTime { get; set; }

    // Giờ đóng cửa (phải sau giờ mở).
    [Required(ErrorMessage = "Vui lòng nhập giờ đóng cửa.")]
    public TimeSpan? CloseTime { get; set; }

    // Độ dài mỗi slot (>= 30 phút).
    [Range(30, int.MaxValue, ErrorMessage = "Độ dài slot tối thiểu 30 phút.")]
    public int SlotDurationInMinutes { get; set; } = 60;

    // Danh sách sân để chọn trong dropdown.
    public List<Court> Courts { get; set; } = new();

    // Số slot đã sinh (dùng hiển thị thông báo).
    public int CreatedCount { get; set; }
}