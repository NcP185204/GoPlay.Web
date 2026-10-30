using GoPlay.Web.Enums;
using GoPlay.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Web.Data;

// DbContext: cầu nối giữa entity C# và SQL Server.
// Kế thừa IdentityDbContext để có sẵn DbSet cho Users/Roles của Identity.
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // decimal nên khai báo precision để tránh SQL Server mặc định decimal(18,2) không khớp đơn vị VND.
        builder.Entity<Court>()
            .Property(c => c.PricePerHour).HasPrecision(18, 0);

        builder.Entity<TimeSlot>()
            .Property(t => t.Price).HasPrecision(18, 0);

        builder.Entity<Booking>()
            .Property(b => b.TotalPrice).HasPrecision(18, 0);

        // Enum lưu dạng chuỗi để dễ đọc DB (tương đương @Enumerated(STRING)).
        builder.Entity<Court>()
            .Property(c => c.CourtType).HasConversion<string>();

        builder.Entity<Booking>()
            .Property(b => b.Status).HasConversion<string>();

        builder.Entity<Booking>()
            .Property(b => b.PaymentStatus).HasConversion<string>();

        // Many-to-many: 1 đơn chứa nhiều slot, 1 slot thuộc 1 đơn.
        // EF Core >=5 tự tạo bảng join BookingTimeSlot.
        builder.Entity<Booking>()
            .HasMany(b => b.TimeSlots)
            .WithMany(t => t.Bookings);

        // 1 user nhiều đơn (xóa user không xóa đơn trong v1 — giữ đơn để lịch sử liền mạch).
        builder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1 sân nhiều đơn.
        builder.Entity<Booking>()
            .HasOne(b => b.Court)
            .WithMany()
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1 sân nhiều slot; xóa sân thì xóa slot theo.
        builder.Entity<TimeSlot>()
            .HasOne(t => t.Court)
            .WithMany(c => c.TimeSlots)
            .HasForeignKey(t => t.CourtId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}