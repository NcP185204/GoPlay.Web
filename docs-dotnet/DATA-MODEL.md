# DATA-MODEL — GoPlay (.NET) — EF Core Code-First trên SQL Server

> Mapping từ entity Java (`src/main/java/com/backend/GoPlay/model`) sang entity C#.
> Giữ **4 entity chính**. Dùng **EF Core Code-First** (migration), DB **SQL Server** (Docker).

## 1. Tổng quan 4 entity

```
ApplicationUser (Identity)  1 ──── *  Booking
       │                                    │ *──*  TimeSlot * ──── 1 Court
       └────────────── (không giữ quan hệ trực tiếp trong v1) ───────┘
```

| Entity C# | Bảng | Nguồn Java |
|-----------|------|------------|
| `ApplicationUser` | `AspNetUsers` (do Identity tạo) | `model/User.java` |
| `Court` | `Courts` | `model/Court.java` |
| `TimeSlot` | `TimeSlots` | `model/TimeSlot.java` |
| `Booking` | `Bookings` | `model/Booking.java` + `model/Payment.java` (gộp) |

> Entity Java bị **cắt**: `PricingRule`, `CourtImage`, `Review`, `Notification`, `RefreshToken`.
> Lý do: xem `DECISIONS.md`.

## 2. Entity chi tiết

### 2.1 `ApplicationUser : IdentityUser`

Kế thừa `Microsoft.AspNetCore.Identity.IdentityUser` (đã có sẵn `Id` (string), `Email`,
`PasswordHash`, `PhoneNumber`, `UserName`, ...).

| Thuộc tính C# | Kiểu | Nullable | Ghi chú | Nguồn Java |
|---------------|------|----------|---------|------------|
| `FullName` | `string` | Không | Họ tên hiển thị | `User.fullName` |
| (thừa kế `Email`) | `string` | Không | Email đăng nhập | `User.email` |
| (thừa kế `PasswordHash`) | `string` | — | Mật khẩu băm | `User.password` |
| (thừa kế `PhoneNumber`) | `string` | Có | SĐT | `User.phoneNumber` |

- Role: dùng **Identity Role** (`AspNetRoles`), 2 role: `Admin`, `Customer`.
  (Bản Java: enum `UserRole { PLAYER, MANAGER, ADMIN }` → rút gọn còn 2 role, xem `DECISIONS.md`.)
- Không dùng `fcmToken`, `favoriteCourts` (cắt).

### 2.2 `Court`

| Thuộc tính C# | Kiểu | Nullable | Ghi chú | Nguồn Java |
|---------------|------|----------|---------|------------|
| `Id` | `int` | Không (PK) | Identity auto | `Court.id` |
| `Name` | `string` | Không | Tên sân | `Court.name` |
| `Address` | `string` | Không | Địa chỉ | `Court.address` |
| `Description` | `string?` | Có | Mô tả | `Court.description` |
| `CourtType` | `SportType` (enum) | Không | Loại sân | `Court.courtType` |
| `PricePerHour` | `decimal` | Không | Giá/giờ (VND) | `Court.pricePerHour` |
| `ThumbnailUrl` | `string?` | Có | Ảnh đại diện (URL) | `Court.thumbnailUrl` |
| `TimeSlots` | `List<TimeSlot>` | — | Navigation | (gộp từ `Court.images`/`pricingRules`) |

- Bỏ: `owner`, `latitude`, `longitude`, `averageRating`, `images`, `pricingRules`, `services`.
- Không có owner → **Admin quản lý toàn bộ sân** (đơn giản hoá role MANAGER).

### 2.3 `TimeSlot`

| Thuộc tính C# | Kiểu | Nullable | Ghi chú | Nguồn Java |
|---------------|------|----------|---------|------------|
| `Id` | `int` | Không (PK) | Identity auto | `TimeSlot.id` |
| `CourtId` | `int` | Không (FK) | Sân | `TimeSlot.court` |
| `Court` | `Court` | — | Navigation | `TimeSlot.court` |
| `StartTime` | `DateTime` | Không | Bắt đầu | `TimeSlot.startTime` |
| `EndTime` | `DateTime` | Không | Kết thúc | `TimeSlot.endTime` |
| `IsAvailable` | `bool` | Không | Còn trống? (default `true`) | `TimeSlot.isAvailable` |
| `Price` | `decimal` | Không | Giá khung giờ | `TimeSlot.price` |
| `Bookings` | `List<Booking>` | — | Many-to-many | `Booking.timeSlots` |

### 2.4 `Booking`

Gộp `Booking` + `Payment` của Java (thanh toán chỉ tiền mặt).

| Thuộc tính C# | Kiểu | Nullable | Ghi chú | Nguồn Java |
|---------------|------|----------|---------|------------|
| `Id` | `int` | Không (PK) | Identity auto | `Booking.id` |
| `UserId` | `string` | Không (FK) | Người đặt | `Booking.user` / `Payment.user` |
| `User` | `ApplicationUser` | — | Navigation | `Booking.user` |
| `CourtId` | `int` | Không (FK) | Sân (denormalize từ slot đầu) | (tiện ích mới) |
| `Court` | `Court` | — | Navigation | (tiện ích mới) |
| `TotalPrice` | `decimal` | Không | Tổng tiền | `Booking.totalPrice` / `Payment.amount` |
| `Status` | `BookingStatus` (enum) | Không | Trạng thái đơn | `Booking.status` |
| `PaymentStatus` | `PaymentStatus` (enum) | Không | Trạng thái thanh toán | `Payment.status` |
| `PaymentMethod` | `string` | Không | Luôn `"CASH"` | `Payment.paymentMethod` |
| `Note` | `string?` | Có | Ghi chú | `Booking.note` |
| `CreatedAt` | `DateTime` | Không | Ngày tạo | `Booking.createdAt` |
| `TimeSlots` | `List<TimeSlot>` | — | Many-to-many | `Booking.timeSlots` |

- Bỏ `transactionId`, `updatedAt` (không giao dịch online).

## 3. Quan hệ (relationships)

| Quan hệ | Kiểu | Mô tả | Nguồn Java |
|---------|------|-------|------------|
| `ApplicationUser` ↔ `Booking` | 1–N | 1 user nhiều đơn | `Booking.user` `@ManyToOne` |
| `Court` ↔ `TimeSlot` | 1–N | 1 sân nhiều slot | `TimeSlot.court` `@ManyToOne` |
| `Court` ↔ `Booking` | 1–N | 1 sân nhiều đơn (denormalize) | (mới) |
| `Booking` ↔ `TimeSlot` | N–N | 1 đơn chứa nhiều slot | `Booking.timeSlots` `@ManyToMany` |

- EF Core (>= 5) hỗ trợ **Many-to-Many ngầm định**, tự tạo bảng join `BookingTimeSlot`
  (`BookingId`, `TimeSlotsId`). Không cần entity join riêng → vẫn đủ 4 entity.

## 4. Enums (C#)

| Enum C# | Giá trị | Nguồn Java |
|---------|---------|------------|
| `SportType` | `Football`, `Basketball`, `Tennis`, `Badminton`, `Volleyball` | `util/SportType.java` |
| `BookingStatus` | `Pending`, `Confirmed`, `Cancelled` | `util/BookingStatus.java` |
| `PaymentStatus` | `Pending`, `Success`, `Failed` (bỏ `Expired`) | `util/PaymentStatus.java` |

> C# dùng PascalCase cho enum member; lưu DB dạng chuỗi (như `@Enumerated(STRING)` bên Java)
> bằng EF Core `HasConversion<string>()`.

## 5. Cấu hình EF Core Code-First (gợi ý)

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Court>()
            .Property(c => c.PricePerHour).HasPrecision(18, 0);

        builder.Entity<TimeSlot>()
            .Property(t => t.Price).HasPrecision(18, 0);

        builder.Entity<Booking>()
            .Property(b => b.TotalPrice).HasPrecision(18, 0);

        builder.Entity<Booking>()
            .HasMany(b => b.TimeSlots)
            .WithMany(t => t.Bookings);
    }
}
```

- Chuỗi kết nối SQL Server (Docker) trong `appsettings.json`:
  `Server=localhost,1433;Database=GoPlayDb;User Id=sa;Password=...;TrustServerCertificate=True;`
- Seed dữ liệu (admin + customer + sân mẫu) trong `DbInitializer`/`DataSeeder` (xem `TASKS.md`).

