# ARCHITECTURE — GoPlay (.NET) — ASP.NET Core MVC + Razor

> Kiến trúc MVC monolith đơn giản: **Controller → Service → DbContext**.
> KHÔNG dùng Clean Architecture / MediatR / CQRS / Repository pattern.
> Dùng **ASP.NET Core Identity** (cookie auth) thay cho JWT.

## 1. Nguyên tắc

- **Layered đơn giản**: `Controllers` (nhận request, điều hướng view) → `Services`
  (nghiệp vụ) → `ApplicationDbContext` (EF Core truy vấn DB).
- Controller **không chứa logic nghiệp vụ**; logic nằm trong Service (giống Java tách
  `Controller` ↔ `ServiceImpl`).
- Service dùng thẳng `ApplicationDbContext` (tương đương JpaRepository) — không qua
  tầng Repository để code ngắn gọn cho portfolio.
- DI đăng ký toàn bộ trong `Program.cs`.

## 2. Cấu trúc thư mục

```
GoPlay.Web/                          # 1 project duy nhất (monolith)
├── Program.cs                        # Entry + DI + pipeline
├── appsettings.json                  # Connection string, seed config
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs          # Login/Register/Logout
│   ├── CourtController.cs            # Danh sách + chi tiết sân
│   ├── BookingController.cs          # Đặt/hủy/lịch sử
│   └── AdminController.cs            # Dashboard, quản lý sân, slot, đơn
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbInitializer.cs              # Seed admin/customer/sân mẫu
│   └── Migrations/
├── Models/                           # Entity (Code-First)
│   ├── ApplicationUser.cs
│   ├── Court.cs
│   ├── TimeSlot.cs
│   └── Booking.cs
├── Enums/
│   ├── SportType.cs
│   ├── BookingStatus.cs
│   └── PaymentStatus.cs
├── Services/
│   ├── CourtService.cs
│   ├── BookingService.cs
│   └── AdminService.cs
├── ViewModels/                       # DTO cho từng view/form
│   ├── CourtSearchViewModel.cs
│   ├── CourtDetailsViewModel.cs
│   ├── BookingCreateViewModel.cs
│   ├── GenerateSlotsViewModel.cs
│   └── AdminDashboardViewModel.cs
└── Views/                            # Razor (Bootstrap 5)
    ├── _ViewImports.cshtml
    ├── _Layout.cshtml
    ├── Home/Index.cshtml
    ├── Account/Login.cshtml
    ├── Account/Register.cshtml
    ├── Courts/Index.cshtml
    ├── Courts/Details.cshtml
    ├── Bookings/Index.cshtml
    └── Admin/
        ├── Index.cshtml
        ├── Courts.cshtml
        ├── GenerateSlots.cshtml
        └── Bookings.cshtml
```

- `wwwroot/` chứa Bootstrap 5 (CDN hoặc lib tĩnh). Không tự viết CSS.
- Test (xUnit) đặt project riêng `GoPlay.Web.Tests/`.

## 3. Luồng yêu cầu

```
Browser (Razor form)
   → Controller (model binding ViewModel, kiểm tra [Authorize])
   → Service (nghiệp vụ + transaction)
   → ApplicationDbContext (EF Core)
   → Controller trả View/Redirect (kèm TempData thông báo lỗi/thành công)
```

- **Validation**: dùng DataAnnotations trên ViewModel (`[Required]`, `[Range]`,
  `[EmailAddress]`...), tương đương `jakarta.validation` bên Java. Lỗi nghiệp vụ (trùng giờ,
  không đủ quyền) do Service throw → Controller bắt và đưa vào `ModelState`/`TempData`.

## 4. Dependency Injection (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => { /* mật khẩu, lockout */ })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<CourtService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AdminService>();

var app = builder.Build();

// pipeline: UseStaticFiles, UseRouting, UseAuthentication, UseAuthorization,
// MapControllerRoute(...), Seed database khi khởi động.

app.Run();
```

## 5. Authentication & Authorization

- **ASP.NET Core Identity** với **cookie auth** (app web, không phải mobile → không cần JWT).
- 2 role: `Admin`, `Customer`.
- `[Authorize]` chặn trang cần đăng nhập; `[Authorize(Roles = "Admin")]` cho trang Admin
  (tương đương `@PreAuthorize("hasAuthority('ROLE_ADMIN')")` bên Java).
- Seed role + tài khoản admin trong `DbInitializer`.

## 6. Bảng đối chiếu Spring Boot ↔ ASP.NET Core

| Spring Boot (Java) | ASP.NET Core (C#) | Ghi chú |
|--------------------|-------------------|---------|
| `@RestController` / `@RequestMapping` | `Controller` + `[Route]` + `[HttpGet/HttpPost]` | MVC trả View thay vì JSON |
| `@Service` | class + `AddScoped<T>()` | DI theo scope |
| `@Repository` / `JpaRepository` | `DbSet<T>` trên `ApplicationDbContext` | bỏ tầng Repository |
| `@Entity` / `@Table` | class + `DbSet<T>` + Fluent API | Code-First |
| `@Id @GeneratedValue(IDENTITY)` | `[Key]` + `DatabaseGenerated(Identity)` | PK int auto |
| `@ManyToOne` / `@OneToMany` | navigation + `HasOne/HasMany` | EF Core |
| `@ManyToMany` | `HasMany(...).WithMany(...)` | EF Core >=5 ngầm tạo bảng join |
| `@Enumerated(STRING)` | `HasConversion<string>()` | lưu enum dạng chuỗi |
| `jakarta.validation` | `System.ComponentModel.DataAnnotations` | `[Required]`, `[Range]`, ... |
| `@PreAuthorize` | `[Authorize(Roles=...)]` | phân quyền |
| `SecurityFilterChain` | `app.UseAuthentication(); app.UseAuthorization();` | pipeline |
| `BCryptPasswordEncoder` | `PasswordHasher<T>` (Identity) | băm mật khẩu |
| `@Transactional` | `SaveChanges()` (Unit of Work) | giao dịch ngầm |
| `@AuthenticationPrincipal User` | `UserManager.GetUserAsync(User)` | lấy user hiện tại |
| `DataSeeder` (`CommandLineRunner`) | `DbInitializer` gọi sau `app.Build()` | seed data |
| `GlobalExceptionHandler` | `UseExceptionHandler` + `[ValidateAntiForgeryToken]` | xử lý lỗi |
| JWT (`JwtTokenProvider`, filter) | **Identity cookie auth** | thay đổi chủ đích |
| `application.properties` | `appsettings.json` | cấu hình |

## 7. Ghi chú quan trọng

- **CSRF**: form Razor nên dùng `[ValidateAntiForgeryToken]` (bản Java tắt CSRF vì là API stateless).
- **Redirect/PRG pattern**: sau POST dùng `RedirectToAction` để tránh gửi lại form khi F5.
- **TempData**: dùng hiển thị thông báo thành công/lỗi một lần.

