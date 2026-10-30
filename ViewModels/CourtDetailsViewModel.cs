using GoPlay.Web.Models;

namespace GoPlay.Web.ViewModels;

// View model cho trang chi tiết sân + khung giờ theo ngày.
public class CourtDetailsViewModel
{
    // Sân đang xem.
    public Court Court { get; set; } = null!;

    // Ngày được chọn (mặc định hôm nay).
    public DateTime Date { get; set; } = DateTime.Today;

    // Danh sách khung giờ của sân trong ngày đã chọn.
    public List<TimeSlot> TimeSlots { get; set; } = new();
}