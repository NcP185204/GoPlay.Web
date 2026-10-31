using GoPlay.Web.Models;

namespace GoPlay.Web.ViewModels;

// View model cho trang quản lý sân của Admin: danh sách + form tạo/sửa.
public class AdminCourtsViewModel
{
    // Danh sách toàn bộ sân.
    public List<Court> Courts { get; set; } = new();

    // Form tạo/sửa sân (khi bấm "Sửa" sẽ được nạp dữ liệu tương ứng).
    public CourtFormViewModel Form { get; set; } = new();
}