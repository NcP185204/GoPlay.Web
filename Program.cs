using GoPlay.Web.Data;
using GoPlay.Web.Models;
using GoPlay.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký DbContext: mỗi request được cấp 1 instance (scoped),
// dùng SQL Server qua chuỗi kết nối trong appsettings.json.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký ASP.NET Core Identity (cookie auth) + hỗ trợ role.
// Dùng AddIdentity (không dùng AddDefaultIdentity) vì ta tự viết trang Login/Register,
// không cần thêm package Razor UI của Identity. AddIdentity vẫn cung cấp đủ
// UserManager/SignInManager/RoleManager và cơ chế đăng nhập cookie.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Giữ policy mặc định của Identity (mật khẩu tối thiểu 6 ký tự).
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Service nghiệp vụ, đăng ký scoped để inject vào Controller.
builder.Services.AddScoped<CourtService>();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Quan trọng: UseAuthentication phải đứng trước UseAuthorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seed dữ liệu ban đầu (role, tài khoản demo, sân + slot mẫu).
// Tạo scope riêng vì DbContext là scoped, không dùng chung với app lifetime.
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

app.Run();