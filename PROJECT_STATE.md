# PROJECT_STATE.md

## 1. Tổng quan trạng thái dự án

- **Dự án**: BulkNotificationSystem (Hệ thống gửi thông báo hàng loạt qua email)
- **Giai đoạn hiện tại**: **P2.7 – RabbitMQ Infrastructure Setup & Integration Test**
- **Trạng thái**: **P2.7 COMPLETED (HOÀN THÀNH P2.7)**
- **Ngày cập nhật**: 2026-11-25
- **Phụ trách**: Senior .NET Developer

---

## 2. Thông tin môi trường và phiên bản công nghệ

| Thành phần | Thông tin chi tiết | Phân loại nguồn gốc |
| :--- | :--- | :--- |
| **Hệ điều hành** | Ubuntu 24.04 LTS trên WSL2 (Linux x64) | Môi trường phát triển |
| **.NET SDK** | `10.0.404` (Target Framework: `net10.0`) | Lựa chọn công nghệ |
| **C# Features** | Nullable Reference Types (`enable`), Implicit Usings (`enable`) | Lựa chọn công nghệ |
| **Runtime Version** | `Microsoft.NETCore.App 10.0.15`, `Microsoft.AspNetCore.App 10.0.15` | Lựa chọn công nghệ |
| **Cơ sở dữ liệu** | PostgreSQL 16 (Docker: `bulk-notification-postgres`) với EF Core 10 | Lựa chọn kỹ thuật của nhóm |
| **CLI Tools** | `dotnet-ef` version `10.0.15` (Global Tool) | Công cụ hỗ trợ migration |
| **Message Broker** | RabbitMQ 4.4.1 (Docker: `bulk-notification-rabbitmq`) với `RabbitMQ.Client` thuần (v7.x) | Lựa chọn kỹ thuật của nhóm |
| **Email Protocol** | SMTP qua MailKit / MimeKit | Lựa chọn kỹ thuật của nhóm |
| **CSV Parser** | CsvHelper (Stream-based) | Lựa chọn kỹ thuật của nhóm |
| **Container** | Docker CLI (`bulk-notification-postgres`, `bulk-notification-rabbitmq` standalone containers) | Lựa chọn triển khai |

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
   - **Mục đích**: Tầng truy cập dữ liệu PostgreSQL dùng chung giữa `Campaign.Api` và `Notification.Worker`, chứa Entities (`Campaign`, `Delivery`), DbContext (`BulkNotificationDbContext`), Fluent API, Factory (`BulkNotificationDbContextFactory`) và Migrations.
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

## 4. Chi tiết hiện thực P2.5 (Initial Migration & SQL Script)

### 4.1. Migration & Factory đã tạo
- **Design-Time Factory**: `src/Infrastructure/Data/BulkNotificationDbContextFactory.cs` cài đặt `IDesignTimeDbContextFactory<BulkNotificationDbContext>`, hỗ trợ nạp biến môi trường `ConnectionStrings__DefaultConnection` trước khi fallback sang chuỗi placeholder an toàn.
- **Migration Code**: `src/Infrastructure/Migrations/20261120001012_InitialCreate.cs` và `20261120001012_InitialCreate.Designer.cs`.
- **Model Snapshot**: `src/Infrastructure/Migrations/BulkNotificationDbContextModelSnapshot.cs` phản ánh đầy đủ và nhất quán toàn bộ cấu hình Fluent API.
- **SQL Script**: `database/scripts/InitialCreate.sql`.

---

## 5. Chi Tiết Thực Hiện P2.6 (PostgreSQL Docker Integration Test)

### 5.1. Thông Số Container PostgreSQL
- **Container Name:** `bulk-notification-postgres`
- **Image:** `postgres:16-alpine`
- **Port Mapping:** `5432:5432`
- **Named Volume:** `bulk_postgres_data` -> `/var/lib/postgresql/data`
- **Database:** `bulk_notification`
- **User:** `bulk_user`
- **Trạng thái:** Healthy / Accepting connections. Dữ liệu bảng và migration lịch sử được bảo toàn vẹn toàn.

### 5.2. Kết Quả Kiểm Thử Toàn Vẹn Database Thực Tế (SQL Verification)
Đã kiểm tra trực tiếp qua `psql` trong container `bulk-notification-postgres`:
1. **Lịch sử Migration:** Bảng `__EFMigrationsHistory` tồn tại, ghi nhận `20261120001012_InitialCreate` (ProductVersion `10.0.15`).
2. **Schema Bảng & Kiểu Dữ Liệu:** Đầy đủ các trường kiểu `uuid`, `varchar`, `text`, `integer`, `timestamptz`.
3. **Foreign Key `FK_deliveries_campaigns_campaign_id`:** `ON DELETE RESTRICT` hoạt động chính xác.
4. **Computed Column `normalized_email`:** Tự động sinh `lower(recipient_email::text) STORED`.
5. **Chống Trùng Email Không Phân Biệt Hoa Thường:** Chặn trùng lặp trong cùng Campaign qua unique constraint.
6. **Cho Phép Trùng Email Ở Hai Campaign Khác Nhau:** Insert thành công `INSERT 0 1`.
7. **Hệ Thống Partial Indexes:** Đầy đủ 3 partial indexes cho pending, stale recovery, retry scheduler.
8. **Ràng Buộc RESTRICT Chặn Xóa Campaign Đang Có Deliveries:** Bị từ chối chính xác bởi PostgreSQL.
9. **Dọn Dẹp Dữ Liệu Test:** Đã làm sạch toàn bộ dữ liệu mẫu sau khi xác nhận.

---

## 6. Chi Tiết Thực Hiện P2.7 (RabbitMQ Infrastructure Setup & Integration Test)

### 6.1. Thông Số Container RabbitMQ
- **Container Name:** `bulk-notification-rabbitmq`
- **Image:** `rabbitmq:4-management` (Phiên bản `4.4.1`)
- **Port Mapping:**
  - `5672:5672` (AMQP 0-9-1 & AMQP 1.0)
  - `15672:15672` (HTTP Management UI & REST API)
- **Named Volume:** `bulk_rabbitmq_data` gắn vào `/var/lib/rabbitmq`
- **Tài khoản riêng biệt cho môi trường local dev:**
  - User: `bulk_user`
  - Tags: `[administrator]`
  - Virtual Host: `/`
  - Mật khẩu: Cung cấp qua biến môi trường `RABBITMQ_DEFAULT_PASS` (không hardcode vào mã nguồn git).
- **Trạng thái:** Node `rabbit@db62fc546afb` fully booted and running; `rabbitmq-diagnostics ping` thành công.

### 6.2. Kiểm Tra Trạng Thái & Quản Trị
- **Kết nối Port:** Cả hai port `5672` và `15672` đều phản hồi thành công từ WSL / localhost (`Connection succeeded`).
- **Management UI & REST API:**
  - Truy cập `http://localhost:15672` hoạt động.
  - Endpoint `GET /api/whoami` với Basic Auth `bulk_user` xác nhận quyền `administrator`.
  - Endpoint `GET /api/overview` xác nhận RabbitMQ version `4.4.1`.
  - Endpoint `GET /api/queues` xác nhận không có hàng đợi rác tồn dư.

### 6.3. Kiểm Thử Giao Tiếp Thực Tế Bằng .NET 10 (Smoke Test)
Đã triển khai một ứng dụng Console độc lập tại thư mục tạm để smoke test giao tiếp AMQP với package `RabbitMQ.Client` version `7.1.2`:
1. **Nạp Cấu Hình:** Đọc `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `RABBITMQ_VHOST` từ biến môi trường.
2. **Khởi Tạo Kết Nối & Kênh:** Mở kết nối và channel bất đồng bộ qua `CreateConnectionAsync()` và `CreateChannelAsync()`.
3. **Khai Báo Hàng Đợi Bền Vững:** `QueueDeclareAsync` với `durable: true`, `exclusive: false`, `autoDelete: false`.
4. **Publish Tin Nhắn Persistent:** Tin nhắn mang `DeliveryMode = DeliveryModes.Persistent`, `MessageId`, `ContentType = "text/plain"`, publish qua `BasicPublishAsync()`.
5. **Consumer & Manual Acknowledgment:** Sử dụng `AsyncEventingBasicConsumer`, nhận tin nhắn bất đồng bộ, kiểm tra nội dung và gọi `BasicAckAsync(deliveryTag, multiple: false)`.
6. **Xác Minh Nội Dung:** Payload nhận được khớp 100% với payload đã gửi.
7. **Dọn Dẹp Hàng Đợi:** Hủy đăng ký consumer và gọi `QueueDeleteAsync()`. Hàng đợi được xóa hoàn toàn, 0 message tồn dư.
8. **Dọn Dẹp Ứng Dụng Tạm:** Thư mục project smoke test tạm thời được xóa sạch, không để lại bất kỳ thay đổi nào trong các project production (`Campaign.Api`, `Notification.Worker`, `Admin.Web`, `Infrastructure`, `Shared`).

### 6.4. Các Lệnh Quản Trị Container RabbitMQ
- **Khởi chạy mới (nếu chưa có container):**
  ```bash
  docker run -d \
    --name bulk-notification-rabbitmq \
    -p 5672:5672 \
    -p 15672:15672 \
    -v bulk_rabbitmq_data:/var/lib/rabbitmq \
    -e RABBITMQ_DEFAULT_USER=bulk_user \
    -e RABBITMQ_DEFAULT_PASS=<MAT_KHAU_DEV> \
    -e RABBITMQ_DEFAULT_VHOST=/ \
    rabbitmq:4-management
  ```
- **Khởi động container đã tồn tại:**
  ```bash
  docker start bulk-notification-rabbitmq
  ```
- **Kiểm tra trạng thái node:**
  ```bash
  docker exec bulk-notification-rabbitmq rabbitmq-diagnostics ping
  docker exec bulk-notification-rabbitmq rabbitmq-diagnostics check_running
  ```
- **Dừng container khi không sử dụng:**
  ```bash
  docker stop bulk-notification-rabbitmq
  ```

---

## 7. Giới Hạn Tuân Thủ Trong Giai Đoạn P2.7

- [x] Khởi chạy RabbitMQ bằng Docker CLI đơn lẻ (`docker run`), không tạo `docker-compose.yml`.
- [x] Không làm mất hoặc ảnh hưởng dữ liệu của PostgreSQL container hiện hữu.
- [x] Sử dụng tài khoản riêng (`bulk_user`), không dùng tài khoản `guest` từ bên ngoài container.
- [x] Không hardcode hoặc commit mật khẩu thật lên repository git.
- [x] Smoke test giao tiếp AMQP thực hiện trong project độc lập, không nhúng vào production code.
- [x] Không tạo Enqueuer, Consumer nghiệp vụ, Retry Scheduler hay gửi email SMTP.
- [x] Dừng lại đúng phạm vi P2.7, không tự ý chuyển sang P2.8 hay P3.

---

## 8. Kế Hoạch Công Việc Tiếp Theo (Roadmap)

1. **P3 – Shared Contracts, Enums & DTOs**:
   - Định nghĩa Message Contracts (`SendEmailNotificationCommand`) và API DTOs trong `src/Shared`.
2. **P4 – Campaign.Api Implementation**:
   - Tích hợp `CsvHelper` đọc stream CSV.
   - Minimal API endpoints và Enqueuer Transaction (`FOR UPDATE SKIP LOCKED`).
3. **P5 – Notification.Worker Implementation**:
   - RabbitMQ Consumer, Atomic Claim (`claim_version`), Lease, SMTP Client và Stale Recovery Job.
4. **P6 – Admin.Web Implementation**:
   - Giao diện Razor Pages quản lý chiến dịch và thống kê.
5. **P7 – Containerization & CI/CD**:
   - Dockerfile, `docker-compose.yml` (Postgres, RabbitMQ, Mailpit).
