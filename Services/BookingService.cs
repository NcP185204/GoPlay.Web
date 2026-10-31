using GoPlay.Web.Data;
using GoPlay.Web.Enums;
using GoPlay.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoPlay.Web.Services;

// Nghiệp vụ đặt sân: tạo đơn, xem lịch sử, hủy đơn.
// Tương đương BookingServiceImpl bên Java (nghiệp vụ + giao dịch).
public class BookingService(ApplicationDbContext db, ILogger<BookingService> logger)
{
    // Tạo đơn đặt sân thanh toán tiền mặt (R1–R6 trong SPEC).
    // Throws InvalidOperationException khi slot trống rỗng/trùng/khác sân;
    // DbUpdateConcurrencyException khi có người đặt trước (bắt ở Controller).
    public async Task<Booking> CreateAsync(string userId, List<int> slotIds, string? note)
    {
        // R1: phải chọn ít nhất 1 slot (vẫn giữ làm lớp chặn cuối).
        if (slotIds is null || slotIds.Count == 0)
        {
            throw new InvalidOperationException("Vui lòng chọn ít nhất một khung giờ.");
        }

        // Load tất cả slot được chọn cùng 1 lần để kiểm tra + khoá.
        var slots = await db.TimeSlots
            .Where(t => slotIds.Contains(t.Id))
            .ToListAsync();

        // Slot không tồn tại (id sai) hoặc thiếu → báo lỗi.
        if (slots.Count != slotIds.Count)
        {
            throw new InvalidOperationException("Một hoặc nhiều khung giờ không tồn tại.");
        }

        // R2: tất cả slot phải cùng 1 sân.
        var distinctCourtIds = slots.Select(s => s.CourtId).Distinct().ToList();
        if (distinctCourtIds.Count != 1)
        {
            throw new InvalidOperationException("Các khung giờ phải thuộc cùng một sân.");
        }

        // R3: slot đã bị đặt thì không cho đặt nữa.
        var unavailableSlotIds = slots.Where(s => !s.IsAvailable).Select(s => s.Id).ToList();
        if (unavailableSlotIds.Count > 0)
        {
            logger.LogWarning(
                "User {UserId} attempted to book unavailable slot(s) {SlotIds}",
                userId,
                unavailableSlotIds);

            throw new InvalidOperationException("Khung giờ đã bị người khác đặt.");
        }

        // R5: totalPrice = tổng giá các slot.
        var totalPrice = slots.Sum(s => s.Price);

        // R6: tiền mặt → đơn Confirmed ngay, PaymentStatus.Pending (chờ thu tiền).
        var booking = new Booking
        {
            UserId = userId,
            CourtId = distinctCourtIds[0],
            TotalPrice = totalPrice,
            Status = BookingStatus.Confirmed,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = "CASH",
            Note = note,
            CreatedAt = DateTime.UtcNow,
            TimeSlots = slots
        };

        // R4: khoá slot lại (đánh dấu không còn trống).
        foreach (var slot in slots)
        {
            slot.IsAvailable = false;
        }

        db.Bookings.Add(booking);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating booking for user {UserId}", userId);
            throw;
        }

        logger.LogInformation(
            "Booking {BookingId} created for user {UserId} with total price {TotalPrice}",
            booking.Id,
            userId,
            totalPrice);

        return booking;
    }

    // Lịch sử đơn của 1 user, mới nhất trước (tương đương GET /api/bookings/my-history).
    public async Task<List<Booking>> GetHistoryAsync(string userId)
    {
        return await db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Include(b => b.TimeSlots)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    // Hủy đơn: chỉ chủ đơn, chưa hủy mới được hủy; trả slot về trống; Payment Pending→Failed (R7–R9).
    // Trả true nếu hủy thành công, false nếu không tìm thấy đơn.
    public async Task<bool> CancelAsync(int bookingId, string userId)
    {
        var booking = await db.Bookings
            .Include(b => b.TimeSlots)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        // Không tìm thấy đơn → false (Controller báo NotFound).
        if (booking is null)
        {
            return false;
        }

        // R7: chỉ chủ đơn được hủy.
        if (booking.UserId != userId)
        {
            throw new InvalidOperationException("Bạn không có quyền hủy đơn này.");
        }

        // Đã hủy thì không hủy lại.
        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Đơn đã bị hủy trước đó.");
        }

        // R8: chuyển trạng thái đơn + trả slot về trống.
        booking.Status = BookingStatus.Cancelled;

        // R9: nếu đang chờ thu tiền thì đánh dấu Failed.
        if (booking.PaymentStatus == PaymentStatus.Pending)
        {
            booking.PaymentStatus = PaymentStatus.Failed;
        }

        foreach (var slot in booking.TimeSlots)
        {
            slot.IsAvailable = true;
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while cancelling booking {BookingId}", bookingId);
            throw;
        }

        logger.LogInformation("Booking {BookingId} cancelled by user {UserId}", bookingId, userId);
        return true;
    }
}
