using GoPlay.Web.Models;

namespace GoPlay.Web.ViewModels;

// View model cho trang lịch sử đơn của user hiện tại.
public class BookingHistoryViewModel
{
    // Danh sách đơn của user (mới nhất trước), kèm Court và TimeSlots.
    public List<Booking> Bookings { get; set; } = new();
}