namespace GoPlay.Web.ViewModels;

// View model cho trang dashboard Admin (thống kê tổng quan).
public class AdminDashboardViewModel
{
    // Tổng số sân đang quản lý.
    public int TotalCourts { get; set; }

    // Tổng số đơn đặt sân.
    public int TotalBookings { get; set; }

    // Doanh thu = tổng tiền các đơn Confirmed.
    public decimal Revenue { get; set; }

    // Đơn chờ xử lý = đơn chưa thu tiền mặt (PaymentStatus.Pending).
    public int PendingPayments { get; set; }
}