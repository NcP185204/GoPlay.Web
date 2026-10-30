using GoPlay.Web.Services;
using GoPlay.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GoPlay.Web.Controllers;

// Danh sách + chi tiết sân (tương đương CourtController bên Java).
// Đặt tên "CourtsController" (số nhiều) để route sinh ra là /Courts và khớp
// thư mục view Views/Courts/ theo SPEC (mục 5).
public class CourtsController(CourtService courtService) : Controller
{
    // GET: /Courts — hiển thị danh sách sân + form tìm kiếm.
    [HttpGet]
    public async Task<IActionResult> Index(CourtSearchViewModel model)
    {
        // Gọi service để lọc; trả lại model để form giữ nguyên tiêu chí đã chọn.
        model.Results = await courtService.SearchAsync(model.Name, model.CourtType);
        return View(model);
    }

    // GET: /Courts/Details/{id}?date=... — chi tiết sân + khung giờ theo ngày.
    [HttpGet]
    public async Task<IActionResult> Details(int id, DateTime? date)
    {
        var court = await courtService.GetByIdAsync(id);
        if (court is null)
        {
            return NotFound();
        }

        var selectedDate = date?.Date ?? DateTime.Today;
        var slots = await courtService.GetSlotsForDateAsync(id, selectedDate);

        var model = new CourtDetailsViewModel
        {
            Court = court,
            Date = selectedDate,
            TimeSlots = slots
        };

        return View(model);
    }
}