using GoPlay.Web.Models;
using GoPlay.Web.Services;
using GoPlay.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoPlay.Web.Controllers;

// Đặt sân, lịch sử đơn và hủy đơn (tương đương BookingController bên Java).
// Đặt tên "BookingsController" (số nhiều) để route sinh ra là /Bookings và khớp
// thư mục view Views/Bookings/ theo SPEC (mục 5).
// Tất cả đều yêu cầu đăng nhập (Customer).
[Authorize]
public class BookingsController(
    BookingService bookingService,
    UserManager<ApplicationUser> userManager) : Controller
{
    // POST: /Bookings/Create — tạo đơn đặt sân thanh toán tiền mặt.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToDetailsWithError(model.CourtId, "Vui lòng chọn ít nhất một khung giờ.");
        }

        // Lấy user hiện tại để gán chủ đơn (tương đương @AuthenticationPrincipal bên Java).
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        try
        {
            await bookingService.CreateAsync(user.Id, model.SlotIds, model.Note);
            TempData["Success"] = "Đặt sân thành công.";
            return RedirectToAction("Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            // 2 user đặt cùng 1 slot: user sau bị phát hiện xung đột row version.
            return RedirectToDetailsWithError(model.CourtId, "Khung giờ vừa bị người khác đặt. Vui lòng chọn khung giờ khác.");
        }
        catch (InvalidOperationException ex)
        {
            return RedirectToDetailsWithError(model.CourtId, ex.Message);
        }
    }

    // GET: /Bookings — lịch sử đơn của user hiện tại (mới nhất trước).
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = new BookingHistoryViewModel
        {
            Bookings = await bookingService.GetHistoryAsync(user.Id)
        };

        return View(model);
    }

    // POST: /Bookings/{id}/Cancel — hủy đơn (chỉ chủ đơn), trả slot về trống.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        try
        {
            var ok = await bookingService.CancelAsync(id, user.Id);
            if (!ok)
            {
                return NotFound();
            }

            TempData["Success"] = "Hủy đơn thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    // Tiện ích: quay lại trang chi tiết sân kèm thông báo lỗi (giữ nguyên ngày được chọn).
    private IActionResult RedirectToDetailsWithError(int courtId, string message)
    {
        if (courtId > 0)
        {
            TempData["Error"] = message;
            return RedirectToAction("Details", "Courts", new { id = courtId });
        }

        TempData["Error"] = message;
        return RedirectToAction("Index", "Courts");
    }
}