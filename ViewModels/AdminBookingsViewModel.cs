using GoPlay.Web.Models;

namespace GoPlay.Web.ViewModels;

// View model cho trang quản lý đơn của Admin (xem toàn bộ đơn).
public class AdminBookingsViewModel
{
    // Toàn bộ đơn trong hệ thống, sắp mới nhất trước.
    public List<Booking> Bookings { get; set; } = new();
}