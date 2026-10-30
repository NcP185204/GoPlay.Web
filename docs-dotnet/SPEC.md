# SPEC — GoPlay (.NET) — Bản thu nhỏ ASP.NET Core MVC + Razor

> Tài liệu đặc tả phạm vi port từ project GoPlay (Spring Boot monolith, Phase 1)
> sang **ASP.NET Core MVC + Razor**. Nguồn Java: `src/main/java/com/backend/GoPlay`.

## 1. Mục tiêu & bối cảnh

- Project **portfolio** để ứng tuyển Fullstack .NET Intern (yêu cầu C#/.NET + SQL Server).
- **1 project MVC monolith**, **1 database SQL Server** (chạy bằng Docker).
- Thời gian: **3–5 ngày**. Ưu tiên **Bootstrap 5** có sẵn, **không tự viết CSS**.
- Port TẬP TRUNG luồng cốt lõi: **tìm sân → xem khung giờ → đặt sân (tiền mặt) → lịch sử → hủy**, cộng phần **Admin** quản lý.
- Tên class/file viết bằng **tiếng Anh**, giải thích bằng **tiếng Việt**.

## 2. Phạm vi

### 2.1 MUST (bắt buộc làm)

| # | Chức năng | Ghi chú |
|---|-----------|---------|
| 1 | Đăng ký / Đăng nhập / Đăng xuất (ASP.NET Core Identity + cookie) | Thay JWT. 2 role: **Admin**, **Customer** |
| 2 | Xem danh sách sân + tìm kiếm (tên, loại sân) | Thay `CourtController.search` |
| 3 | Xem chi tiết sân + khung giờ còn trống theo ngày | Thay `GET /api/courts/{courtId}/available-slots` |
| 4 | Đặt sân (chọn nhiều khung giờ, thanh toán **tiền mặt**) | Thay `BookingController.createBooking` |
| 5 | Xem lịch sử đặt sân của tôi | Thay `GET /api/bookings/my-history` |
| 6 | Hủy đơn đặt sân (trả khung giờ về trống) | Thay `PUT /api/bookings/{id}/cancel` |
| 7 | Admin: dashboard thống kê | Thay `AdminDashboardStatsResponse` |
| 8 | Admin: CRUD sân | Thay `CourtController` create/update/delete |
| 9 | Admin: sinh khung giờ cho sân | Thay `POST /api/courts/{courtId}/generate-slots` |
| 10 | Admin: quản lý đơn + xác nhận thanh toán tiền mặt | Thay `ConfirmPaymentRequest` |

### 2.2 KHÔNG LÀM (cắt bỏ)

| Loại | Mô tả | File Java nguồn |
|------|-------|------------------|
| Thanh toán online | MoMo IPN/webhook, tạo link MoMo, ký HMAC | `PaymentController.momoSuccessWebhook`, `MomoPaymentStrategy` |
| Thông báo đẩy | Firebase FCM | `FirebasePushNotificationService`, `User.fcmToken` |
| Social login | Google/Facebook OAuth | `GoogleAuthService`, `FacebookAuthService` |
| JWT refresh token | Refresh token + logout xóa token | `RefreshTokenService`, `AuthController.refresh` |
| API Android | Favorite, FCM token, upload ảnh, tìm theo toạ độ | `UserController`, `FavoriteService`, `CourtImageService` |
| Đánh giá sân | Review + `averageRating` | `ReviewServiceImpl`, `Review` |
| Quy tắc giá phức tạp | `PricingRule` theo ngày/giờ | `PricingRule`, `CourtScheduleServiceImpl.setPricingRule` |
| Lưu trữ file | Upload ảnh Cloudinary/local | `CloudinaryConfig`, `FileStorageService` |

> **Nguyên tắc thanh toán:** chỉ giữ phần **tiền mặt (CASH) + trạng thái**. Khi đặt sân tiền mặt,
> đơn được `CONFIRMED` ngay (không có bước chờ thanh toán online).

## 3. Chức năng theo vai trò

### 3.1 Customer (người chơi)

- Đăng ký tài khoản (role mặc định = **Customer**).
- Đăng nhập / đăng xuất.
- Tìm kiếm sân theo tên + loại sân.
- Xem chi tiết sân, chọn ngày → thấy danh sách khung giờ (còn trống / đã đặt) kèm giá.
- Chọn 1 hoặc nhiều khung giờ (cùng 1 sân) → đặt sân thanh toán tiền mặt.
- Xem lịch sử đơn của mình (mới nhất trước).
- Hủy đơn của chính mình (chưa hủy mới được hủy).

### 3.2 Admin (quản trị viên)

- Dashboard: tổng số sân, tổng số đơn, doanh thu (đơn `Confirmed`), đơn chờ xử lý.
- Quản lý sân: tạo mới, sửa, xóa.
- Sinh khung giờ cho 1 sân (theo ngày bắt đầu, số ngày, giờ mở/đóng, độ dài slot).
- Xem toàn bộ đơn; xác nhận đã thu tiền mặt (chuyển `PaymentStatus.Pending → Success`).

## 4. Quy tắc nghiệp vụ quan trọng (kèm file Java nguồn)

| # | Quy tắc | Mô tả | File Java nguồn |
|---|---------|-------|------------------|
| R1 | Bắt buộc chọn khung giờ | Đơn phải có ≥ 1 khung giờ | `BookingServiceImpl.createBooking` |
| R2 | Cùng 1 sân trong 1 đơn | Không gộp slot nhiều sân vào 1 đơn | `BookingServiceImpl.createBooking` |
| R3 | Kiểm tra trùng giờ | Slot `isAvailable == false` → báo "đã bị người khác đặt" | `BookingServiceImpl.createBooking` |
| R4 | Khoá slot khi đặt | Đặt thành công → slot `isAvailable = false` | `BookingServiceImpl.createBooking` |
| R5 | Tính giá | `totalPrice = Σ(slot.price)`; giá slot = `Court.PricePerHour × (độ dài slot / 60)`, gán lúc sinh slot | `BookingServiceImpl.createBooking`, `CourtScheduleServiceImpl.generateInitialTimeSlots` |
| R6 | Tiền mặt → CONFIRMED ngay | `paymentMethod == "CASH"` → `Booking = Confirmed`, `Payment = Pending` (chờ thu tiền) | `BookingServiceImpl.createBooking` |
| R7 | Hủy đơn | Chỉ chủ đơn được hủy; đã `Cancelled` không hủy lại | `BookingServiceImpl.cancelBooking` |
| R8 | Trả slot khi hủy | Hủy → `Cancelled`, trả slot `available = true` | `BookingServiceImpl.cancelBooking` |
| R9 | Cập nhật thanh toán khi hủy | `Payment == Pending` → `Failed` | `BookingServiceImpl.cancelBooking` |
| R10 | Email duy nhất | Đăng ký trùng email → "Email đã được sử dụng" | `AuthServiceImpl.register` |
| R11 | Phân quyền sân | Chỉ Admin (thay owner/manager) tạo/sửa/xóa sân | `CourtServiceImpl.checkOwnership`, `CourtController` `@PreAuthorize` |
| R12 | Sinh slot không trùng | Bỏ qua slot đã tồn tại | `CourtScheduleServiceImpl.generateInitialTimeSlots` |
| R13 | Sinh slot chặn quá giờ đóng | `endTime > closeTime` thì dừng | `CourtScheduleServiceImpl.generateInitialTimeSlots` |

## 5. Danh sách trang Razor

| # | URL | Controller / Action | Vai trò | Mục đích |
|---|-----|---------------------|---------|----------|
| 1 | `GET /Courts` | `CourtController.Index` | Public | Danh sách + tìm kiếm sân (name, courtType) |
| 2 | `GET /Courts/Details/{id}` | `CourtController.Details` | Public | Chi tiết sân + chọn ngày + slot + nút đặt |
| 3 | `POST /Bookings/Create` | `BookingController.Create` | Customer | Tạo đơn (slotIds + note, thanh toán CASH) |
| 4 | `GET /Bookings` | `BookingController.Index` | Customer | Lịch sử đơn của tôi + nút Hủy |
| 5 | `POST /Bookings/{id}/Cancel` | `BookingController.Cancel` | Customer | Hủy đơn (trả slot về trống) |
| 6 | `GET/POST /Account/Login` | `AccountController.Login` | Public | Đăng nhập |
| 7 | `GET/POST /Account/Register` | `AccountController.Register` | Public | Đăng ký (role Customer) |
| 8 | `GET /Admin` | `AdminController.Index` | Admin | Dashboard thống kê |
| 9 | `GET/POST /Admin/Courts` | `AdminController.Courts` | Admin | Danh sách + form tạo/sửa/xóa sân |
| 10 | `GET/POST /Admin/Courts/GenerateSlots/{id}` | `AdminController.GenerateSlots` | Admin | Sinh khung giờ cho sân |
| 11 | `GET /Admin/Bookings` | `AdminController.Bookings` | Admin | Quản lý đơn + xác nhận thu tiền mặt |

> Lưu ý: mục tiêu **8–10 trang**. Trang #5 (Hủy) là **POST không có view riêng**
> (nút nằm trong trang #4); #6/#7 là 2 trang Identity tách biệt. Nếu cần rút gọn có thể
> gộp Login + Register vào 1 layout. (Xem "Quyết định đã chốt".)

## 6. Validation

| Trường | Quy tắc | Nguồn Java |
|--------|---------|------------|
| `Booking.TimeSlotIds` | Không rỗng, ≥ 1 phần tử | `BookingRequest` `@NotEmpty` |
| `Booking` cùng sân | Tất cả slot cùng `CourtId` | `BookingServiceImpl.createBooking` |
| `Register.Email` | Đúng định dạng, không trùng | `AuthServiceImpl.register` |
| `Register.Password` | Tối thiểu 6 ký tự (chuẩn Identity) | — |
| `Register.FullName` | Bắt buộc | `User.fullName` |
| `Court.Name`, `Address`, `CourtType`, `PricePerHour` | Bắt buộc | `Court` entity |
| `GenerateSlots.StartDate` | Bắt buộc | `GenerateTimeSlotRequest` `@NotNull` |
| `GenerateSlots.NumberOfDays` | ≥ 1 | `GenerateTimeSlotRequest` `@Min(1)` |
| `GenerateSlots.OpenTime` / `CloseTime` | Bắt buộc, `CloseTime > OpenTime` | `GenerateTimeSlotRequest` |
| `GenerateSlots.SlotDurationInMinutes` | ≥ 30 phút | `GenerateTimeSlotRequest` `@Min(30)` |

## 7. Quyết định đã chốt

1. **Role:** bỏ `MANAGER`, chỉ giữ **Admin + Customer** (Admin quản lý toàn bộ sân).
2. **PricingRule:** bỏ; giá slot = `Court.PricePerHour × (độ dài slot / 60)`.
3. **Payment:** gộp vào `Booking` (cột `PaymentStatus` + `PaymentMethod`).
4. **Review:** cắt ở v1 (không `Review`, không `averageRating`).
5. **Ảnh sân:** dùng URL (không upload file).
6. **Seed data:** `admin@goplay.com` / `Admin@123` và `customer@goplay.com` / `Customer@123`.
7. **Số trang:** chấp nhận **11 trang** (trang Hủy là POST, không có view riêng).


