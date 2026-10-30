using GoPlay.Web.Enums;
using GoPlay.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Web.Data;

// Seed dữ liệu ban đầu khi app khởi động:
// role + tài khoản demo + sân mẫu + slot.
// Tương đương DataSeeder/CommandLineRunner bên Spring Boot.
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        // Lấy các service từ DI (scope khởi động). RoleManager/UserManager
        // là service của Identity, dùng để tạo role và user chuẩn băm mật khẩu.
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        await EnsureRolesAsync(roleManager);
        await EnsureUsersAsync(userManager);

        // Chỉ seed sân + slot khi bảng Courts còn trống, tránh seed trùng mỗi lần chạy.
        if (!await db.Courts.AnyAsync())
        {
            await SeedCourtsWithSlotsAsync(db);
        }
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        // Tạo 2 role cố định, bỏ MANAGER theo SPEC (chỉ Admin + Customer).
        string[] roles = [IdentityRoles.Admin, IdentityRoles.Customer];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task EnsureUsersAsync(UserManager<ApplicationUser> userManager)
    {
        // Tài khoản demo theo SPEC, mật khẩu đủ mạnh theo policy mặc định của Identity.
        await CreateUserIfMissingAsync(userManager, "admin@goplay.com", "Admin@123", "Quản trị viên", IdentityRoles.Admin);
        await CreateUserIfMissingAsync(userManager, "customer@goplay.com", "Customer@123", "Khách hàng demo", IdentityRoles.Customer);
    }

    private static async Task CreateUserIfMissingAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return; // đã tồn tại thì bỏ qua.
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            // Đánh dấu email đã xác nhận để không bị kẹt bước confirm email.
            EmailConfirmed = true
        };

        // CreateAsync tự băm mật khẩu (tương đương BCryptPasswordEncoder).
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Không tạo được user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
    }

    private static async Task SeedCourtsWithSlotsAsync(ApplicationDbContext db)
    {
        // Vài sân mẫu để demo danh sách + đặt sân.
        var courts = new List<Court>
        {
            new()
            {
                Name = "Sân bóng đá mini Sài Gòn",
                Address = "123 Lê Văn Sỹ, Quận 3",
                Description = "Sân cỏ nhân tạo 5 người",
                CourtType = SportType.Football,
                PricePerHour = 300000,
                ThumbnailUrl = "https://placehold.co/600x400?text=Football"
            },
            new()
            {
                Name = "Sân cầu lông Trung Tâm",
                Address = "45 Nguyễn Đình Chiểu, Quận 1",
                Description = "Sân cầu lông trong nhà",
                CourtType = SportType.Badminton,
                PricePerHour = 120000,
                ThumbnailUrl = "https://placehold.co/600x400?text=Badminton"
            },
            new()
            {
                Name = "Sân bóng rổ ngoài trời",
                Address = "78 Võ Văn Tần, Quận 3",
                Description = "Sân bóng rổ tiêu chuẩn",
                CourtType = SportType.Basketball,
                PricePerHour = 200000,
                ThumbnailUrl = "https://placehold.co/600x400?text=Basketball"
            }
        };

        db.Courts.AddRange(courts);
        await db.SaveChangesAsync(); // lưu để có Court.Id trước khi gán slot.

        // Sinh slot cho 7 ngày tới, mỗi slot 60 phút, từ 7h đến 22h.
        const int slotDurationMinutes = 60;
        var today = DateTime.Today;

        foreach (var court in courts)
        {
            for (var day = 0; day < 7; day++)
            {
                var date = today.AddDays(day);

                for (var hour = 7; hour < 22; hour++)
                {
                    var start = date.AddHours(hour);

                    db.TimeSlots.Add(new TimeSlot
                    {
                        CourtId = court.Id,
                        StartTime = start,
                        EndTime = start.AddMinutes(slotDurationMinutes),
                        IsAvailable = true,
                        // Giá slot = Court.PricePerHour × (độ dài slot / 60).
                        Price = court.PricePerHour * (slotDurationMinutes / 60m)
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }
}

// Nơi đặt tên 2 role để tránh gõ sai chuỗi ở nhiều nơi.
public static class IdentityRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}