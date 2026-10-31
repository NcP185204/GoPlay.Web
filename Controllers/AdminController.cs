using GoPlay.Web.Models;
using GoPlay.Web.Services;
using GoPlay.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoPlay.Web.Controllers;

// Trang quản trị dành riêng cho Admin (tương đương AdminController bên Java).
// Toàn bộ controller yêu cầu role Admin theo SPEC (mục phân quyền).
[Authorize(Roles = "Admin")]
public class AdminController(AdminService adminService) : Controller
{
    // GET: /Admin — dashboard thống kê tổng quan.
    public async Task<IActionResult> Index()
    {
        var stats = await adminService.GetDashboardStatsAsync();

        var model = new AdminDashboardViewModel
        {
            TotalCourts = stats.TotalCourts,
            TotalBookings = stats.TotalBookings,
            Revenue = stats.Revenue,
            PendingPayments = stats.PendingPayments
        };

        return View(model);
    }

    // GET: /Admin/Courts — danh sách sân + form tạo mới (Task 13).
    public async Task<IActionResult> Courts()
    {
        var model = new AdminCourtsViewModel
        {
            Courts = await adminService.GetAllCourtsAsync()
        };

        return View(model);
    }

    // POST: /Admin/Courts/Create — tạo mới sân.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourt(CourtFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu sân không hợp lệ.";
            return RedirectToAction("Courts");
        }

        await adminService.CreateCourtAsync(new Court
        {
            Name = form.Name,
            Address = form.Address,
            Description = form.Description,
            CourtType = form.CourtType,
            PricePerHour = form.PricePerHour,
            ThumbnailUrl = form.ThumbnailUrl
        });

        TempData["Success"] = "Tạo sân thành công.";
        return RedirectToAction("Courts");
    }

    // POST: /Admin/Courts/Edit — cập nhật sân (nạp form từ danh sách).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourt(CourtFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu sân không hợp lệ.";
            return RedirectToAction("Courts");
        }

        var court = await adminService.GetCourtByIdAsync(form.Id);
        if (court is null)
        {
            return NotFound();
        }

        court.Name = form.Name;
        court.Address = form.Address;
        court.Description = form.Description;
        court.CourtType = form.CourtType;
        court.PricePerHour = form.PricePerHour;
        court.ThumbnailUrl = form.ThumbnailUrl;

        await adminService.UpdateCourtAsync(court);
        TempData["Success"] = "Cập nhật sân thành công.";
        return RedirectToAction("Courts");
    }

    // POST: /Admin/Courts/Delete/{id} — xóa sân.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourt(int id)
    {
        var ok = await adminService.DeleteCourtAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        TempData["Success"] = "Xóa sân thành công.";
        return RedirectToAction("Courts");
    }

    // GET: /Admin/GenerateSlots — form sinh khung giờ cho sân.
    public async Task<IActionResult> GenerateSlots()
    {
        var model = new GenerateSlotsViewModel
        {
            Courts = await adminService.GetAllCourtsAsync()
        };

        return View(model);
    }

    // POST: /Admin/GenerateSlots — sinh khung giờ (R12, R13) + đúng giá.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSlots(GenerateSlotsViewModel model)
    {
        // Kiểm tra bổ sung: CloseTime phải sau OpenTime.
        if (model.OpenTime is not null && model.CloseTime is not null &&
            model.CloseTime <= model.OpenTime)
        {
            ModelState.AddModelError(string.Empty, "Giờ đóng cửa phải sau giờ mở cửa.");
        }

        if (!ModelState.IsValid)
        {
            model.Courts = await adminService.GetAllCourtsAsync();
            return View(model);
        }

        try
        {
            var created = await adminService.GenerateSlotsAsync(
                model.CourtId,
                model.StartDate!.Value,
                model.NumberOfDays,
                model.OpenTime!.Value,
                model.CloseTime!.Value,
                model.SlotDurationInMinutes);

            TempData["Success"] = $"Đã sinh {created} khung giờ mới.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("GenerateSlots");
    }

    // GET: /Admin/Bookings — xem toàn bộ đơn + xác nhận thu tiền mặt.
    public async Task<IActionResult> Bookings()
    {
        var model = new AdminBookingsViewModel
        {
            Bookings = await adminService.GetAllBookingsAsync()
        };

        return View(model);
    }

    // POST: /Admin/Bookings/ConfirmPayment/{id} — xác nhận đã thu tiền mặt.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var ok = await adminService.ConfirmPaymentAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        TempData["Success"] = "Xác nhận thanh toán thành công.";
        return RedirectToAction("Bookings");
    }
}
