using GoPlay.Web.Data;
using GoPlay.Web.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Tests;

// DbContext dành riêng cho test: kế thừa ApplicationDbContext nhưng nới lỏng cấu hình
// RowVersion cho SQLite. SQLite không tự sinh rowversion (khác SQL Server), nên ta bỏ
// ràng buộc NOT NULL và đặt giá trị mặc định để việc INSERT TimeSlot không bị lỗi.
public class SqliteApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SQLite không hỗ trợ kiểu rowversion tự tăng; chỉ cần giữ concurrency token để test
        // phát hiện xung đột khi hai context cùng cập nhật một hàng.
        builder.Entity<TimeSlot>()
            .Property(t => t.RowVersion)
            .IsRequired(false)
            .HasDefaultValueSql("X'0000000000000000'");
    }
}

// Helper tạo SQLite in-memory cho test. Dùng chung 1 kết nối mở để nhiều context
// (mô phỏng nhiều request/user) cùng thấy một cơ sở dữ liệu.
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new SqliteApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    // Tạo context mới dùng chung kết nối (để mô phỏng user/request khác).
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new SqliteApplicationDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}