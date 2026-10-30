using GoPlay.Web.Enums;

namespace GoPlay.Web.Models;

// Sân thể thao (tương đương model/Court.java, đã cắt owner/rating/ảnh upload).
public class Court
{
    public int Id { get; set; }

    // Tên sân hiển thị trong danh sách/tìm kiếm.
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    // Mô tả tuỳ chọn, cho phép NULL trong DB.
    public string? Description { get; set; }

    // Loại sân, lưu DB dạng chuỗi qua Fluent API.
    public SportType CourtType { get; set; }

    // Giá thuê 1 giờ (VND), dùng để tính giá slot khi sinh khung giờ.
    public decimal PricePerHour { get; set; }

    // Ảnh đại diện dạng URL (không upload file).
    public string? ThumbnailUrl { get; set; }

    // Navigation: 1 sân có nhiều khung giờ.
    public List<TimeSlot> TimeSlots { get; set; } = new();
}