# BulkNotificationSystem

Hệ thống gửi thông báo hàng loạt (bulk email notification system) được xây dựng trên nền tảng .NET 10 với kiến trúc hướng sự kiện (Event-Driven Architecture), phân tách độc lập giữa dịch vụ quản lý chiến dịch, giao diện quản trị web, worker xử lý gửi email và tầng truy cập dữ liệu dùng chung.

---

## 1. Kiến trúc hệ thống

Solution `BulkNotificationSystem.sln` bao gồm 5 project trong thư mục `src/` và thư mục `tests/`:

- **`src/Admin.Web`**: Web ứng dụng quản trị sử dụng ASP.NET Core Razor Pages.
- **`src/Campaign.Api`**: Web API xây dựng theo phong cách ASP.NET Core Minimal API để quản lý các chiến dịch gửi thông báo.
- **`src/Notification.Worker`**: .NET Worker Service chạy nền để tiêu thụ message từ hàng đợi và thực hiện gửi email hàng loạt.
- **`src/Infrastructure`**: C# Class Library chứa tầng truy cập dữ liệu PostgreSQL, EF Core DbContext, Entity Configurations và Migrations dùng chung.
- **`src/Shared`**: C# Class Library chứa các Message Contracts, DTOs và Models dùng chung giữa các dịch vụ.
- **`tests/`**: Thư mục dành cho các dự án kiểm thử (Unit Tests, Integration Tests).

### Mô hình tham chiếu (Project References)

```text
Admin.Web          Campaign.Api          Notification.Worker
    │                   │                         │
    │                   ├───► Infrastructure ◄────┤
    │                   │           │             │
    ▼                   ▼           ▼             ▼
  Shared ◄──────────────┴───────────┴─────────────┘
```

- `Infrastructure` tham chiếu `Shared`.
- `Campaign.Api` và `Notification.Worker` tham chiếu `Infrastructure` và `Shared`.
- `Admin.Web` chỉ tham chiếu `Shared` (không tham chiếu `Infrastructure`).

---

## 2. Cấu trúc thư mục

```text
BulkNotificationSystem/
├── src/
│   ├── Admin.Web/              # ASP.NET Core Razor Pages (net10.0)
│   ├── Campaign.Api/           # ASP.NET Core Minimal API (net10.0)
│   ├── Infrastructure/         # EF Core 10 & PostgreSQL Data Layer (net10.0)
│   ├── Notification.Worker/    # .NET Worker Service (net10.0)
│   └── Shared/                 # Class Library chứa Contracts/DTOs (net10.0)
├── tests/                      # Thư mục cho test projects
├── BulkNotificationSystem.sln  # Solution file
├── .gitignore                  # Git ignore cho .NET, IDEs (Rider, VS, VS Code)
├── .env.example                # Template biến môi trường mẫu
├── README.md                   # Tài liệu hướng dẫn dự án
└── PROJECT_STATE.md            # Ghi nhận trạng thái dự án và roadmap
```

---

## 3. Yêu cầu môi trường

- **Hệ điều hành**: Ubuntu 24.04 LTS (WSL2 / Linux) hoặc tương thích.
- **.NET SDK**: `10.0.404` (Target Framework: `net10.0`).
- **CLI Tool**: `dotnet-ef` version `10.0.15`.
- **Database (Dự kiến)**: PostgreSQL.
- **Message Broker (Dự kiến)**: RabbitMQ.

---

## 4. Hướng dẫn khởi tạo và chạy dự án

### Bước 1: Thiết lập môi trường biến (Environment Variables)

Sao chép file `.env.example` thành `.env` và cập nhật các thông số cục bộ:

```bash
cp .env.example .env
```

### Bước 2: Khôi phục dependencies và Build solution

```bash
# Restore các dependencies
dotnet restore

# Build toàn bộ solution
dotnet build
```

### Bước 3: Chạy từng service trong môi trường phát triển

Chạy từng dịch vụ bằng lệnh `dotnet run` chỉ định project tương ứng:

```bash
# Chạy Campaign Minimal API
dotnet run --project src/Campaign.Api

# Chạy Admin Web (Razor Pages)
dotnet run --project src/Admin.Web

# Chạy Notification Worker Service
dotnet run --project src/Notification.Worker
```

---

## 5. Quy chuẩn phát triển

1. **Target Framework**: Mọi project đều sử dụng `net10.0`.
2. **C# Features**: Kích hoạt `Nullable` reference types và `ImplicitUsings`.
3. **Quản lý bí mật**: Tuyệt đối không commit file `.env`, mật khẩu hoặc connection string thật vào git. Sử dụng `.env.example` làm tài liệu tham khảo.
4. **Phụ thuộc**: Chỉ thêm NuGet package và tham chiếu khi thực sự cần thiết theo từng giai đoạn triển khai.
