using GoPlay.Web.Enums;
using GoPlay.Web.Models;

namespace GoPlay.Web.ViewModels;

// View model cho trang danh sách + tìm kiếm sân.
// Giữ riêng giá trị filter để form giữ lại sau khi submit (không mất tiêu chí tìm).
public class CourtSearchViewModel
{
    // Từ khoá tìm theo tên sân (có thể rỗng).
    public string? Name { get; set; }

    // Lọc theo loại sân (có thể rỗng nghĩa là "tất cả").
    public SportType? CourtType { get; set; }

    // Kết quả trả về sau khi filter.
    public List<Court> Results { get; set; } = new();
}