using GoPlay.Web.Enums;

namespace GoPlay.Web.Models;

// Đơn đặt sân (tương đương model/Booking.java + model/Payment.java gộp lại).
public class Booking
{
    public int Id { get; set; }

    // Người đặt (FK tới AspNetUsers).
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    // Sân (denormalize từ slot đầu để tiện truy vấn lịch sử theo sân).
    public int CourtId { get; set; }
    public Court Court { get; set; } = null!;

    // Tổng tiền = tổng giá các slot đã chọn.
    public decimal TotalPrice { get; set; }

    // Trạng thái đơn (lưu dạng chuỗi qua Fluent API).
    public BookingStatus Status { get; set; }

    // Trạng thái thanh toán (gộp từ Payment, lưu dạng chuỗi).
    public PaymentStatus PaymentStatus { get; set; }

    // Phương thức thanh toán, luôn "CASH" ở v1.
    public string PaymentMethod { get; set; } = "CASH";

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation many-to-many: 1 đơn chứa nhiều khung giờ.
    public List<TimeSlot> TimeSlots { get; set; } = new();
}