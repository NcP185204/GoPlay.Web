namespace GoPlay.Web.Models;

// Khung giờ đặt sân (tương đương model/TimeSlot.java).
public class TimeSlot
{
    public int Id { get; set; }

    // FK tới sân.
    public int CourtId { get; set; }

    // Navigation ngược về sân.
    public Court Court { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Còn trống hay đã bị đặt (default true khi sinh slot).
    public bool IsAvailable { get; set; } = true;

    // Giá khung giờ: Court.PricePerHour * (độ dài slot / 60), gán lúc sinh slot.
    public decimal Price { get; set; }

    // Navigation many-to-many: 1 slot có thể nằm trong 1 đơn đặt.
    public List<Booking> Bookings { get; set; } = new();
}