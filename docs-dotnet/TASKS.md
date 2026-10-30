# TASKS — GoPlay (.NET) — Danh sách task theo thứ tự

> Mỗi task **chạy độc lập** được và có **tiêu chí hoàn thành** rõ ràng.
> Làm tuần tự 1→16. Ước lượng tổng 3–5 ngày.

| # | Task | Nội dung | Tiêu chí hoàn thành |
|---|------|----------|---------------------|
| 1 | Khởi tạo project | `dotnet new mvc` (hoặc template MVC .NET 10), thêm package `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Tạo `docker-compose.yml` cho SQL Server, cấu hình trong `appsettings.json`; cài `dotnet tool install --global dotnet-ef`. | Chạy `dotnet run` mở trang Home mặc định; `docker compose up` chạy được SQL Server container. |
| 2 | Entity + Enums + DbContext | Tạo `ApplicationUser`, `Court`, `TimeSlot`, `Booking`, các enum, `ApplicationDbContext` với Fluent API (precision, many-to-many). | Build thành công; tạo migration đầu tiên (`dotnet ef migrations add Init`). |
| 3 | Migration + Database | Chạy `dotnet ef database update` tạo bảng trên SQL Server. | Kiểm tra DB có các bảng: `AspNetUsers`, `AspNetRoles`, `Courts`, `TimeSlots`, `Bookings`, `BookingTimeSlot`. |
| 4 | Identity + Seed | Đăng ký Identity 2 role `Admin`/`Customer`, `DbInitializer` seed admin/customer + sân mẫu + slot cho vài sân mẫu. | Đăng nhập được bằng tài khoản seed; role ghi đúng vào `AspNetUserRoles`; sân mẫu có sẵn slot. |
| 5 | Layout Bootstrap 5 | Tạo `_Layout.cshtml` dùng Bootstrap 5 (CDN/local), navbar (đăng nhập/đăng xuất), `_ViewImports`. | Giao diện responsive, navbar hiển thị đúng trạng thái đăng nhập. |
| 6 | Register + Login + Logout | `AccountController` với ViewModel + DataAnnotations; đăng ký (role Customer), đăng nhập, đăng xuất. | Đăng ký tài khoản mới → tự vào role Customer; đăng nhập/đăng xuất hoạt động; lỗi validation hiện trên form. |
| 7 | Danh sách + tìm kiếm sân | `CourtController.Index`: hiển thị danh sách sân, filter theo `name` + `courtType`. | Trang `/Courts` hiện danh sách; tìm kiếm lọc đúng theo tên/loại sân. |
| 8 | Chi tiết sân + khung giờ | `CourtController.Details`: hiện thông tin sân + chọn ngày → danh sách `TimeSlot` (trống/đã đặt) kèm giá. | Trang `/Courts/Details/{id}` hiển thị slot theo ngày; slot đã đặt bị vô hiệu. |
| 9 | Đặt sân (Booking) | `BookingController.Create`: chọn nhiều slot cùng sân → tạo `Booking` (CASH → `Confirmed`, `PaymentStatus.Pending`), khoá slot. Chống đặt trùng đồng thời bằng concurrency token `[Timestamp] byte[] RowVersion` trên `TimeSlot`, bắt `DbUpdateConcurrencyException` và báo "slot vừa bị người khác đặt". Thực hiện đúng R1–R6. | Đặt thành công → slot chuyển "đã đặt", đơn `Confirmed`; đặt slot đã có người → báo lỗi; 2 user đặt cùng slot → user sau báo "slot vừa bị người khác đặt". |
| 10 | Lịch sử đơn | `BookingController.Index`: liệt kê đơn của user hiện tại (mới nhất trước), hiện status + tổng tiền. | Trang `/Bookings` chỉ hiện đơn của người đang đăng nhập. |
| 11 | Hủy đơn | `BookingController.Cancel`: chỉ chủ đơn; chuyển `Cancelled`, trả slot `IsAvailable=true`, `PaymentStatus` Pending→Failed. Thực hiện R7–R9. | Hủy thành công → slot trống lại, trạng thái đơn `Cancelled`; user khác không hủy được. |
| 12 | Admin dashboard | `AdminController.Index`: tổng sân, tổng đơn, doanh thu (`Confirmed`), đơn chờ xử lý. | Trang `/Admin` hiển thị số liệu đúng; chỉ Admin truy cập được. |
| 13 | Admin quản lý sân | `AdminController.Courts`: danh sách + form tạo/sửa/xóa sân (Bootstrap modal/form). | CRUD sân hoạt động; xóa sân có cảnh báo/xác nhận. |
| 14 | Admin sinh slot + quản lý đơn | `AdminController.GenerateSlots` (theo R12, R13) + `AdminController.Bookings` (xem toàn bộ đơn, xác nhận thu tiền mặt `PaymentStatus.Pending→Success`). | Sinh slot không trùng, đúng giá; xác nhận thanh toán cập nhật đúng trạng thái. |
| 15 | Test (xUnit) | Viết unit test cho `BookingService` (tính giá, chặn trùng giờ, chống đặt trùng đồng thời, hủy trả slot) và `CourtService` (tìm kiếm). | `dotnet test` xanh; test phủ các quy tắc R1–R9. |
| 16 | README | Viết `README.md` có ảnh chụp màn hình, hướng dẫn `docker compose up`, tài khoản demo (admin/customer). | README đầy đủ hướng dẫn chạy project + tài khoản demo. |

> **Ghi chú thứ tự:** Task 7–11 là luồng chính của Customer; Task 12–14 là phần Admin;
> Task 15 nên làm song song hoặc cuối cùng. Có thể tách Task 13 thành 2 task nhỏ nếu cần.

> **Ghi chú SQL Server:** nếu SQL Server Docker lỗi trên Apple Silicon quá 1 giờ thì tạm dùng
> SQLite, đổi sang SQL Server ở cuối.