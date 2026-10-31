# GoPlay.Web

Ứng dụng web đặt sân thể thao (bóng đá, cầu lông, bóng rổ, tennis, bóng chuyền) viết bằng
**ASP.NET Core MVC + Razor**, dùng **SQL Server** chạy bằng Docker. Đây là bản port thu nhỏ
từ project GoPlay (Spring Boot monolith) sang .NET, tập trung vào luồng cốt lõi:
**tìm sân → xem khung giờ → đặt sân (tiền mặt) → lịch sử → hủy**, kèm phần quản trị.

## Công nghệ

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core (Code-First, SQL Server)
- ASP.NET Core Identity (cookie auth, 2 role: Admin / Customer)
- Bootstrap 5 (giao diện, không tự viết CSS)
- xUnit (unit test cho Business Logic)
- Docker Compose (SQL Server cho môi trường dev)

## Tính năng

### Người chơi (Customer)
- Đăng ký / đăng nhập / đăng xuất.
- Xem danh sách sân, tìm kiếm theo tên và loại sân.
- Xem chi tiết sân + khung giờ còn trống theo ngày kèm giá.
- Chọn nhiều khung giờ cùng 1 sân để đặt, thanh toán tiền mặt.
- Xem lịch sử đơn của mình (mới nhất trước).
- Hủy đơn của chính mình (trả khung giờ về trống).

### Quản trị viên (Admin)
- Dashboard thống kê: tổng sân, tổng đơn, doanh thu, đơn chờ xử lý.
- Quản lý sân: tạo mới, sửa, xóa.
- Sinh khung giờ cho sân (theo ngày, giờ mở/đóng, độ dài slot).
- Xem toàn bộ đơn và xác nhận đã thu tiền mặt.

## Cách chạy

### 1. Khởi động SQL Server bằng Docker

```bash
docker compose up -d
```

### 2. Tạo / cập nhật database

```bash
dotnet ef database update
```

> Cần cài `dotnet tool install --global dotnet-ef` nếu chưa có.

### 3. Chạy ứng dụng

```bash
dotnet run
```

Mở trình duyệt truy cập: <https://localhost:5001> (hoặc địa chỉ được in ra ở terminal).

## Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---------|-------|----------|
| Admin | `admin@goplay.com` | `Admin@123` |
| Customer | `customer@goplay.com` | `Customer@123` |

## Chạy test

```bash
dotnet test
```

Test dùng **SQLite in-memory** (thay vì EF InMemory) vì SQLite hỗ trợ kiểm tra
concurrency token (`RowVersion`), trong khi EF InMemory bỏ qua concurrency token
nên không bắt được xung đột khi hai người cùng đặt một khung giờ.

## Ảnh chụp màn hình

> Chỗ chèn ảnh chụp màn hình (sẽ được bổ sung sau):

- Danh sách sân: `![Danh sách sân](docs/screenshots/courts.png)`
- Chi tiết sân + khung giờ: `![Chi tiết sân](docs/screenshots/court-details.png)`
- Lịch sử đơn: `![Lịch sử đơn](docs/screenshots/bookings.png)`
- Dashboard Admin: `![Dashboard Admin](docs/screenshots/admin-dashboard.png)`
- Quản lý sân: `![Quản lý sân](docs/screenshots/admin-courts.png)`