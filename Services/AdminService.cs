using GoPlay.Web.Data;
using GoPlay.Web.Enums;
using GoPlay.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Web.Services;

// Nghiệp vụ dành riêng cho Admin: thống kê dashboard, quản lý sân,
// sinh khung giờ và quản lý đơn. Controller Admin chỉ gọi service này.
public class AdminService(ApplicationDbContext db)
{
    // Tổng quan dashboard: tổng sân, tổng đơn, doanh thu (Confirmed), đơn chờ thu tiền.
    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var totalCourts = await db.Courts.CountAsync();
        var totalBookings = await db.Bookings.CountAsync();

        // Doanh thu chỉ tính các đơn đã xác nhận (Confirmed).
        var revenue = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed)
            .SumAsync(b => b.TotalPrice);

        // Đơn chờ xử lý = đơn chưa thu tiền mặt (PaymentStatus.Pending).
        var pendingPayments = await db.Bookings
            .CountAsync(b => b.PaymentStatus == PaymentStatus.Pending);

        return new DashboardStats
        {
            TotalCourts = totalCourts,
            TotalBookings = totalBookings,
            Revenue = revenue,
            PendingPayments = pendingPayments
        };
    }

    // Danh sách toàn bộ sân phục vụ trang quản lý sân.
    public async Task<List<Court>> GetAllCourtsAsync()
    {
        return await db.Courts
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    // Lấy sân theo Id (dùng cho form sửa).
    public async Task<Court?> GetCourtByIdAsync(int id)
    {
        return await db.Courts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Tạo mới sân (R11: chỉ Admin được gọi, phân quyền nằm ở Controller).
    public async Task CreateCourtAsync(Court court)
    {
        db.Courts.Add(court);
        await db.SaveChangesAsync();
    }

    // Cập nhật thông tin sân.
    public async Task UpdateCourtAsync(Court court)
    {
        db.Courts.Update(court);
        await db.SaveChangesAsync();
    }

    // Xóa sân. Trả true nếu sân tồn tại, false nếu không.
    public async Task<bool> DeleteCourtAsync(int id)
    {
        var court = await db.Courts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (court is null)
        {
            return false;
        }

        db.Courts.Remove(court);
        await db.SaveChangesAsync();
        return true;
    }

    // Sinh khung giờ cho 1 sân theo ngày bắt đầu, số ngày, giờ mở/đóng và độ dài slot.
    // Tuân theo R12 (không sinh trùng) và R13 (endTime > closeTime thì dừng).
    // Giá slot = PricePerHour * (độ dài slot / 60).
    // Trả về số slot thực sự được tạo mới.
    public async Task<int> GenerateSlotsAsync(
        int courtId,
        DateTime startDate,
        int numberOfDays,
        TimeSpan openTime,
        TimeSpan closeTime,
        int slotDurationInMinutes)
    {
        var court = await db.Courts
            .FirstOrDefaultAsync(c => c.Id == courtId);

        if (court is null)
        {
            throw new InvalidOperationException("Sân không tồn tại.");
        }

        var created = 0;
        var pricePerSlot = court.PricePerHour * (slotDurationInMinutes / 60m);

        for (var day = 0; day < numberOfDays; day++)
        {
            var date = startDate.Date.AddDays(day);
            var slotStart = date.Add(openTime);
            var closing = date.Add(closeTime);

            // R13: sinh từng slot, nếu endTime vượt quá giờ đóng thì dừng.
            while (slotStart.AddMinutes(slotDurationInMinutes) <= closing)
            {
                var slotEnd = slotStart.AddMinutes(slotDurationInMinutes);

                // R12: bỏ qua slot đã tồn tại (trùng ngày + giờ bắt đầu).
                var exists = await db.TimeSlots
                    .AnyAsync(t => t.CourtId == courtId &&
                                   t.StartTime == slotStart &&
                                   t.EndTime == slotEnd);

                if (!exists)
                {
                    db.TimeSlots.Add(new TimeSlot
                    {
                        CourtId = courtId,
                        StartTime = slotStart,
                        EndTime = slotEnd,
                        IsAvailable = true,
                        Price = pricePerSlot
                    });
                    created++;
                }

                slotStart = slotEnd;
            }
        }

        await db.SaveChangesAsync();
        return created;
    }

    // Toàn bộ đơn trong hệ thống (mới nhất trước), kèm thông tin sân và user.
    public async Task<List<Booking>> GetAllBookingsAsync()
    {
        return await db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Include(b => b.User)
            .Include(b => b.TimeSlots)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    // Xác nhận đã thu tiền mặt: PaymentStatus.Pending → Success.
    // Trả true nếu đơn tồn tại, false nếu không.
    public async Task<bool> ConfirmPaymentAsync(int bookingId)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
        {
            return false;
        }

        // Chỉ xác nhận khi đơn đang chờ thu tiền.
        if (booking.PaymentStatus == PaymentStatus.Pending)
        {
            booking.PaymentStatus = PaymentStatus.Success;
            await db.SaveChangesAsync();
        }

        return true;
    }
}

// DTO tổng hợp số liệu dashboard (trả về cho Controller gán vào ViewModel).
public class DashboardStats
{
    public int TotalCourts { get; set; }
    public int TotalBookings { get; set; }
    public decimal Revenue { get; set; }
    public int PendingPayments { get; set; }
}