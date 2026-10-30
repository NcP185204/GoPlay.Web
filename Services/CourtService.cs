using GoPlay.Web.Enums;
using GoPlay.Web.Models;
using GoPlay.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Web.Services;

// Nghiệp vụ liên quan sân (giống CourtServiceImpl bên Java).
// Controller chỉ gọi service, không truy vấn DbContext trực tiếp.
public class CourtService(ApplicationDbContext db)
{
    // Tìm danh sách sân theo tên (chứa chuỗi, không phân biệt hoa thường)
    // và loại sân (chỉ lọc khi CourtType được chọn).
    public async Task<List<Court>> SearchAsync(string? name, SportType? courtType)
    {
        var query = db.Courts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(c => c.Name.Contains(name));
        }

        if (courtType is not null)
        {
            query = query.Where(c => c.CourtType == courtType);
        }

        // Sắp xếp theo tên để danh sách ổn định, dễ đọc.
        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    // Lấy thông tin sân theo Id (hoặc null nếu không tồn tại).
    public async Task<Court?> GetByIdAsync(int id)
    {
        return await db.Courts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Lấy khung giờ của 1 sân trong 1 ngày, sắp theo giờ bắt đầu.
    public async Task<List<TimeSlot>> GetSlotsForDateAsync(int courtId, DateTime date)
    {
        var start = date.Date;                          // 00:00:00
        var end = start.AddDays(1);                     // 00:00:00 ngày hôm sau

        return await db.TimeSlots
            .AsNoTracking()
            .Where(t => t.CourtId == courtId && t.StartTime >= start && t.StartTime < end)
            .OrderBy(t => t.StartTime)
            .ToListAsync();
    }
}
