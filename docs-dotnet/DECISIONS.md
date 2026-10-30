# DECISIONS — GoPlay (.NET) — Các quyết định đơn giản hoá và lý do

> File này ghi lại **mọi quyết định đơn giản hoá** khi port từ Spring Boot sang .NET,
> kèm lý do. Các mục "điểm tôi tự quyết định" để trống để bạn điền sau khi đọc SPEC.

## 1. Quyết định đã chốt (đề xuất)

| # | Quyết định | Lý do |
|---|------------|-------|
| D1 | Dùng **ASP.NET Core Identity + cookie** thay cho JWT | App web (Razor) dùng cookie đơn giản hơn; JWT chỉ cần cho mobile/API. |
| D2 | Rút **2 role: Admin, Customer** (bỏ PLAYER/MANAGER) | Phù hợp JD Intern, đơn giản; Admin quản lý toàn bộ sân thay cho MANAGER. |
| D3 | **Gộp `Payment` vào `Booking`** (2 cột `PaymentStatus`, `PaymentMethod`) | Chỉ thanh toán tiền mặt, không cần bảng Payment riêng → giảm entity. |
| D4 | **Bỏ `PricingRule`**, dùng `Court.PricePerHour` | Đúng giới hạn 4 entity; quy tắc giá theo ngày/giờ là phức tạp không cần thiết cho v1. |
| D5 | **Bỏ Review + averageRating** | Giảm phạm vi; đánh giá không nằm trong luồng cốt lõi đặt sân. |
| D6 | **Ảnh sân chỉ là URL** (không upload file) | Tránh file storage/Cloudinary; nhập URL nhanh hơn. |
| D7 | **Denormalize `CourtId` vào `Booking`** | Tiện query Admin theo sân; gốc lấy court qua `TimeSlot` (many-to-many). |
| D8 | **Bỏ `FacilityService`/`services` của sân** | Tiện ích phụ, không ảnh hưởng luồng đặt sân. |
| D9 | Dùng `decimal` (thay `Double`) cho giá tiền | Tránh sai số làm tròn tiền tệ, chuẩn cho SQL Server. |
| D10 | Bỏ `latitude/longitude` + tìm theo bán kính | Chức năng app Android; web tìm theo tên/loại sân là đủ. |

## 2. Điểm tôi tự quyết định (để trống — bạn điền)

> (Bạn quyết định và ghi lại tại đây, ví dụ: chọn giữ hay bỏ PricingRule, chọn seed account,
> chọn số trang, chọn có upload ảnh thật hay không...)

- **Về PricingRule (D4):** `___________________________________________`
- **Về Review (D5):** `___________________________________________`
- **Về seed data:** `___________________________________________`
- **Về số trang Razor (10 hay 11):** `___________________________________________`
- **Về ảnh sân (URL vs upload):** `___________________________________________`
- **Khác:** `___________________________________________`

## 3. Lưu ý

- Mọi quyết định ở đây phải nhất quán với `SPEC.md` (mục "Câu hỏi cho tôi").
- Khi thay đổi 1 quyết định, cập nhật lại `DATA-MODEL.md` (entity) và `SPEC.md` (phạm vi).
