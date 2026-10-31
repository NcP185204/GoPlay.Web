using GoPlay.Web.Enums;
using GoPlay.Web.Models;
using GoPlay.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Tests;

// Unit test cho AdminService: sinh slot đúng giá và không trùng.
public class AdminServiceTests
{
    // R5: giá mỗi slot = PricePerHour * (độ dài slot / 60).
    [Fact]
    public async Task GenerateSlotsAsync_UsesCorrectPricePerSlot()
    {
        using var db = new TestDb();
        var court = new Court
        {
            Name = "Sân test",
            Address = "Địa chỉ test",
            CourtType = SportType.Tennis,
            PricePerHour = 120000m
        };
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();

        var svc = new AdminService(db.Context);

        var created = await svc.GenerateSlotsAsync(
            court.Id,
            new DateTime(2026, 11, 1),
            1,
            new TimeSpan(7, 0, 0),
            new TimeSpan(8, 0, 0),
            30); // nửa giờ

        Assert.Equal(2, created);

        var slots = await db.Context.TimeSlots
            .Where(t => t.CourtId == court.Id)
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        Assert.All(slots, s => Assert.Equal(60000m, s.Price)); // 120000 * 30/60
    }

    // R12: sinh slot không trùng (bỏ qua khung giờ đã tồn tại).
    [Fact]
    public async Task GenerateSlotsAsync_SkipsExistingSlots()
    {
        using var db = new TestDb();
        var court = new Court
        {
            Name = "Sân test",
            Address = "Địa chỉ test",
            CourtType = SportType.Football,
            PricePerHour = 100000m
        };
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();

        var existingStart = new DateTime(2026, 11, 1, 7, 0, 0);
        db.Context.TimeSlots.Add(new TimeSlot
        {
            CourtId = court.Id,
            StartTime = existingStart,
            EndTime = existingStart.AddHours(1),
            IsAvailable = true,
            Price = 100000m
        });
        await db.Context.SaveChangesAsync();

        var svc = new AdminService(db.Context);

        // 7h-9h, slot 60 phút → sẽ sinh 2 slot (7h đã tồn tại, 8h mới).
        var created = await svc.GenerateSlotsAsync(
            court.Id,
            new DateTime(2026, 11, 1),
            1,
            new TimeSpan(7, 0, 0),
            new TimeSpan(9, 0, 0),
            60);

        Assert.Equal(1, created);

        var slots = await db.Context.TimeSlots
            .Where(t => t.CourtId == court.Id)
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        Assert.Equal(2, slots.Count);
    }

    // R13: endTime > closeTime thì dừng, không sinh slot vượt quá giờ đóng.
    [Fact]
    public async Task GenerateSlotsAsync_StopsAtCloseTime()
    {
        using var db = new TestDb();
        var court = new Court
        {
            Name = "Sân test",
            Address = "Địa chỉ test",
            CourtType = SportType.Basketball,
            PricePerHour = 200000m
        };
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();

        var svc = new AdminService(db.Context);

        // 7h-8h, slot 45 phút → chỉ sinh được 1 slot (7:00-7:45), slot 7:45-8:30 vượt giờ đóng.
        var created = await svc.GenerateSlotsAsync(
            court.Id,
            new DateTime(2026, 11, 1),
            1,
            new TimeSpan(7, 0, 0),
            new TimeSpan(8, 0, 0),
            45);

        Assert.Equal(1, created);
    }
}