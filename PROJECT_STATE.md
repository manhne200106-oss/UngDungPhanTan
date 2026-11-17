# PROJECT_STATE.md

## 1. Tổng quan trạng thái dự án

- **Dự án**: BulkNotificationSystem (Hệ thống gửi thông báo hàng loạt qua email)
- **Giai đoạn hiện tại**: **P2.4 – Cấu hình PostgreSQL & Dependency Injection (PostgreSQL Configuration & DI)**
- **Trạng thái**: **P2.4 COMPLETED (HOÀN THÀNH P2.4)**
- **Ngày cập nhật**: 2026-11-17
- **Phụ trách**: Senior .NET Developer

---

## 2. Thông tin môi trường và phiên bản công nghệ

| Thành phần | Thông tin chi tiết | Phân loại nguồn gốc |
| :--- | :--- | :--- |
| **Hệ điều hành** | Ubuntu 24.04 LTS trên WSL2 (Linux x64) | Môi trường phát triển |
| **.NET SDK** | `10.0.404` (Target Framework: `net10.0`) | Lựa chọn công nghệ |
| **C# Features** | Nullable Reference Types (`enable`), Implicit Usings (`enable`) | Lựa chọn công nghệ |
| **Runtime Version** | `Microsoft.NETCore.App 10.0.15`, `Microsoft.AspNetCore.App 10.0.15` | Lựa chọn công nghệ |
| **Cơ sở dữ liệu** | PostgreSQL với Entity Framework Core 10 | Lựa chọn kỹ thuật của nhóm |
| **CLI Tools** | `dotnet-ef` version `10.0.15` (Global Tool) | Công cụ hỗ trợ migration |
| **Message Broker** | RabbitMQ với `RabbitMQ.Client` thuần (v7.x) | Lựa chọn kỹ thuật của nhóm |
| **Email Protocol** | SMTP qua MailKit / MimeKit | Lựa chọn kỹ thuật của nhóm |
| **CSV Parser** | CsvHelper (Stream-based) | Lựa chọn kỹ thuật của nhóm |
| **Container (Dự kiến)**| Docker, Docker Compose | Lựa chọn triển khai |

---

## 3. Kiến trúc Solution và các Project

### Danh sách Project trong `BulkNotificationSystem.sln`

1. **`src/Admin.Web`**
   - **Loại project**: ASP.NET Core Web App (Razor Pages) (`Microsoft.NET.Sdk.Web`, `net10.0`)
   - **Mục đích**: Giao diện web quản trị để cấu hình, theo dõi và kích hoạt các chiến dịch thông báo.
   - **Tham chiếu**: `src/Shared` (Không tham chiếu `Infrastructure`).

2. **`src/Campaign.Api`**
   - **Loại project**: ASP.NET Core Minimal API (`Microsoft.NET.Sdk.Web`, `net10.0`)
   - **Mục đích**: RESTful Minimal API xử lý tạo chiến dịch từ file CSV, nhận lệnh Start và kích hoạt Enqueuer nạp việc vào RabbitMQ.
   - **Tham chiếu**: `src/Shared`, `src/Infrastructure`.
   - **Cấu hình DB**: Đăng ký `BulkNotificationDbContext` qua `AddDbContext` + `UseNpgsql()`, đọc `ConnectionStrings:DefaultConnection`.

3. **`src/Notification.Worker`**
   - **Loại project**: .NET Worker Service (`Microsoft.NET.Sdk.Worker`, `net10.0`)
   - **Mục đích**: Dịch vụ nền tiêu thụ message từ RabbitMQ, claim task an toàn, gọi SMTP server gửi email và ghi nhận kết quả.
   - **Tham chiếu**: `src/Shared`, `src/Infrastructure`.
   - **Cấu hình DB**: Đăng ký `BulkNotificationDbContext` qua `AddDbContext` + `UseNpgsql()`, đọc `ConnectionStrings:DefaultConnection`.

4. **`src/Infrastructure`**
   - **Loại project**: C# Class Library (`Microsoft.NET.Sdk`, `net10.0`)
   - **Mục đích**: Tầng truy cập dữ liệu PostgreSQL dùng chung giữa `Campaign.Api` và `Notification.Worker`, chứa Entities (`Campaign`, `Delivery`), DbContext (`BulkNotificationDbContext`), Fluent API và Migrations.
   - **Tham chiếu**: `src/Shared`.
   - **Package cài đặt**:
     - `Microsoft.EntityFrameworkCore` (v10.0.15)
     - `Microsoft.EntityFrameworkCore.Design` (v10.0.15)
     - `Microsoft.EntityFrameworkCore.Relational` (v10.0.15)
     - `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.3)

5. **`src/Shared`**
   - **Loại project**: C# Class Library (`Microsoft.NET.Sdk`, `net10.0`)
   - **Mục đích**: Chứa Message Contracts, DTOs, Enums nghiệp vụ (`CampaignStatus`, `DeliveryStatus`) và Constants dùng chung.
   - **Tham chiếu**: Không tham chiếu project nào.

6. **`tests/`**
   - **Mục đích**: Thư mục chuẩn bị sẵn dành cho các project kiểm thử tự động.

### Ma trận tham chiếu (Dependency Matrix)

| Project | Admin.Web | Campaign.Api | Notification.Worker | Infrastructure | Shared |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Admin.Web** | - | Không | Không | **Không** | **Có** |
| **Campaign.Api** | Không | - | Không | **Có** | **Có** |
| **Notification.Worker** | Không | Không | - | **Có** | **Có** |
| **Infrastructure** | Không | Không | Không | - | **Có** |
| **Shared** | Không | Không | Không | Không | - |

---

## 4. Chi tiết hiện thực P2.4 (PostgreSQL Configuration & DI)

### 4.1. Đăng ký DbContext bằng Dependency Injection
Cả hai ứng dụng `src/Campaign.Api/Program.cs` và `src/Notification.Worker/Program.cs` đều đăng ký `BulkNotificationDbContext` dùng chung từ `Infrastructure`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing required configuration 'ConnectionStrings:DefaultConnection'. " +
        "Please provide a valid connection string via 'ConnectionStrings__DefaultConnection' environment variable, " +
        "appsettings.json, or .env file.");
}

builder.Services.AddDbContext<BulkNotificationDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### 4.2. Cơ chế đọc Connection String & Xử lý cấu hình thiếu
- Đọc qua `builder.Configuration.GetConnectionString("DefaultConnection")` (tương đương key `ConnectionStrings:DefaultConnection`).
- **Fail-Fast**: Nếu connection string bị thiếu hoặc rỗng, ứng dụng lập tức ném ngoại lệ `InvalidOperationException` kèm thông báo hướng dẫn cụ thể, ngăn chặn ứng dụng khởi động âm thầm với cấu hình sai.
- Tuyệt đối không fallback về mật khẩu mặc định hay ghi connection string chứa mật khẩu vào log.

### 4.3. Hướng dẫn cấu hình kết nối: Docker vs Host (WSL2)
Connection string được nạp qua biến môi trường chuẩn của .NET: `ConnectionStrings__DefaultConnection`.

1. **Khi chạy trong Docker / Docker Compose**:
   - Sử dụng service name làm hostname trong mạng nội bộ Docker:
     ```text
     ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=bulk_notification;Username=bulk_user;Password=CHANGE_ME
     ```
   - *Lưu ý*: Không dùng `localhost` trong container vì `localhost` trỏ về chính container đó chứ không phải container PostgreSQL.
2. **Khi chạy trực tiếp trên WSL2 / Host (ngoài Docker)**:
   - Sử dụng `localhost` với cổng PostgreSQL được expose ra host (ví dụ cổng 5432):
     ```text
     ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=bulk_notification;Username=bulk_user;Password=CHANGE_ME
     ```

### 4.4. Bảo vệ bí mật thông tin (Credentials Protection)
- File mẫu `.env.example` chỉ chứa template và placeholder (`CHANGE_ME`), không chứa bí mật thật.
- File `.gitignore` đã cấu hình loại trừ triệt để `.env`, `.env.*`, `.env.local` nhưng cho phép theo dõi `!.env.example`.
- Trong `appsettings.json`, key `ConnectionStrings:DefaultConnection` để rỗng `""`, đóng vai trò schema placeholder.

---

## 5. Giới Hạn Tuân Thủ Trong Giai Đoạn P2.4

- [x] Không tạo Migration (`dotnet ef migrations add` chưa được gọi).
- [x] Không chạy `database update`.
- [x] Không tạo Docker Compose hay cài đặt PostgreSQL server cục bộ vào WSL.
- [x] Không viết Repository, Service hay logic gửi email.
- [x] Không thay đổi Entity, Enum, hay cấu hình Fluent API.
- [x] Không thêm health check hay retry policy ngoài phạm vi.

---

## 6. Kế Hoạch Công Việc Tiếp Theo (Roadmap)

1. **P2.5 – Initial Migration & Verification**:
   - Tạo Migration ban đầu `InitialCreate`.
   - Sinh SQL script từ migration và kiểm tra tính tương thích cú pháp PostgreSQL.
2. **P2.6 – Database Deployment & Integration Verification**:
   - Khởi động PostgreSQL qua Docker và áp dụng migration để xác thực cấu trúc bảng, constraints, indexes.
3. **P3 – Shared Contracts, Enums & DTOs**:
   - Hoàn thiện Message Contracts (`SendEmailNotificationCommand`) và API DTOs trong `src/Shared`.
4. **P4 – Campaign.Api Implementation**:
   - Tích hợp `CsvHelper` đọc stream CSV.
   - Minimal API endpoints và Enqueuer Transaction (`FOR UPDATE SKIP LOCKED`).
5. **P5 – Notification.Worker Implementation**:
   - RabbitMQ Consumer, Atomic Claim (`claim_version`), Lease, SMTP Client và Stale Recovery Job.
6. **P6 – Admin.Web Implementation**:
   - Giao diện Razor Pages quản lý chiến dịch và thống kê.
7. **P7 – Containerization & CI/CD**:
   - Dockerfile, `docker-compose.yml` (Postgres, RabbitMQ, Mailpit).
