using GoPlay.Web.Data;
using GoPlay.Web.Enums;
using GoPlay.Web.Models;
using GoPlay.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Tests;

// Unit test cho BookingService: tính giá, chặn slot đã đặt, concurrency, hủy đơn.
public class BookingServiceTests
{
    // Tạo sân + 1 slot mẫu rồi trả về để test dùng lại.
    private static async Task<(Court court, TimeSlot slot)> SeedCourtAndSlotAsync(
        ApplicationDbContext db,
        decimal pricePerHour = 100000m,
        decimal? slotPrice = null)
    {
        var court = new Court
        {
            Name = "Sân test",
            Address = "Địa chỉ test",
            CourtType = SportType.Football,
            PricePerHour = pricePerHour
        };
        db.Courts.Add(court);
        await db.SaveChangesAsync();

        var slot = new TimeSlot
        {
            CourtId = court.Id,
            StartTime = new DateTime(2026, 11, 1, 8, 0, 0),
            EndTime = new DateTime(2026, 11, 1, 9, 0, 0),
            IsAvailable = true,
            Price = slotPrice ?? pricePerHour
        };
        db.TimeSlots.Add(slot);
        await db.SaveChangesAsync();

        return (court, slot);
    }

    // Tạo user thật trong AspNetUsers để Booking.UserId thoả mãn ràng buộc FK.
    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext db, string userId)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            Email = $"{userId}@test.com",
            FullName = "Người dùng test"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // R5: totalPrice = tổng giá các slot được chọn.
    [Fact]
    public async Task CreateAsync_ComputesTotalPriceFromSelectedSlots()
    {
        using var db = new TestDb();
        var svc = new BookingService(db.Context);
        var (court, slot1) = await SeedCourtAndSlotAsync(db.Context, 100000m, 100000m);

        var slot2 = new TimeSlot
        {
            CourtId = court.Id,
            StartTime = new DateTime(2026, 11, 1, 9, 0, 0),
            EndTime = new DateTime(2026, 11, 1, 10, 0, 0),
            IsAvailable = true,
            Price = 150000m
        };
        db.Context.TimeSlots.Add(slot2);
        await db.Context.SaveChangesAsync();

        var user = await SeedUserAsync(db.Context, "user-1");
        var booking = await svc.CreateAsync(user.Id, new List<int> { slot1.Id, slot2.Id }, null);

        Assert.Equal(250000m, booking.TotalPrice);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(PaymentStatus.Pending, booking.PaymentStatus);
        Assert.Equal("CASH", booking.PaymentMethod);
    }

    // R3: slot đã bị đặt thì không cho đặt lại.
    [Fact]
    public async Task CreateAsync_BlocksAlreadyBookedSlot()
    {
        using var db = new TestDb();
        var svc = new BookingService(db.Context);
        var (_, slot) = await SeedCourtAndSlotAsync(db.Context);
        var user1 = await SeedUserAsync(db.Context, "user-1");
        await SeedUserAsync(db.Context, "user-2");
        await svc.CreateAsync(user1.Id, new List<int> { slot.Id }, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateAsync("user-2", new List<int> { slot.Id }, null));

        Assert.Equal("Khung giờ đã bị người khác đặt.", ex.Message);
    }

    // Concurrency: rowversion của TimeSlot phải phát hiện cập nhật song song (stale write).
    // SQLite không có kiểu rowversion gốc nên ta mô phỏng việc SQL Server tự tăng RowVersion
    // sau lần ghi đầu tiên bằng một câu UPDATE thủ công.
    [Fact]
    public async Task TimeSlot_RowVersion_ThrowsOnStaleConcurrentUpdate()
    {
        using var db = new TestDb();
        var (_, slot) = await SeedCourtAndSlotAsync(db.Context);

        // Gán RowVersion ban đầu là 0x00 để tránh so sánh NULL.
        await db.Context.Database.ExecuteSqlRawAsync(
            "UPDATE TimeSlots SET RowVersion = X'00' WHERE Id = {0}", slot.Id);

        var ctxA = db.CreateContext();
        var ctxB = db.CreateContext();

        var slotA = await ctxA.TimeSlots.FindAsync(slot.Id);
        var slotB = await ctxB.TimeSlots.FindAsync(slot.Id);
        Assert.NotNull(slotA);
        Assert.NotNull(slotB);

        // Người thứ nhất ghi trước (đặt slot).
        slotA!.IsAvailable = false;
        await ctxA.SaveChangesAsync();

        // Mô phỏng SQL Server tự tăng rowversion sau khi hàng bị thay đổi.
        await db.Context.Database.ExecuteSqlRawAsync(
            "UPDATE TimeSlots SET RowVersion = X'01' WHERE Id = {0}", slot.Id);

        // Người thứ hai vẫn giữ RowVersion cũ (0x00) → phát hiện xung đột.
        slotB!.IsAvailable = false;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());
    }

    // R8 + R9: hủy đơn trả slot về trống và chuyển trạng thái đơn/thanh toán.
    [Fact]
    public async Task CancelAsync_FreesSlotsAndUpdatesStatuses()
    {
        using var db = new TestDb();
        var svc = new BookingService(db.Context);
        var (_, slot) = await SeedCourtAndSlotAsync(db.Context);
        var user = await SeedUserAsync(db.Context, "user-1");
        var booking = await svc.CreateAsync(user.Id, new List<int> { slot.Id }, null);

        var ok = await svc.CancelAsync(booking.Id, "user-1");
        Assert.True(ok);

        var reloaded = await db.Context.Bookings
            .Include(b => b.TimeSlots)
            .FirstAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatus.Cancelled, reloaded.Status);
        Assert.Equal(PaymentStatus.Failed, reloaded.PaymentStatus);
        Assert.True(reloaded.TimeSlots.All(t => t.IsAvailable));
    }

    // R7: chỉ chủ đơn mới được hủy.
    [Fact]
    public async Task CancelAsync_ByNonOwner_Throws()
    {
        using var db = new TestDb();
        var svc = new BookingService(db.Context);
        var (_, slot) = await SeedCourtAndSlotAsync(db.Context);
        var user = await SeedUserAsync(db.Context, "user-1");
        await SeedUserAsync(db.Context, "user-2");
        var booking = await svc.CreateAsync(user.Id, new List<int> { slot.Id }, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelAsync(booking.Id, "user-2"));

        Assert.Equal("Bạn không có quyền hủy đơn này.", ex.Message);
    }
}