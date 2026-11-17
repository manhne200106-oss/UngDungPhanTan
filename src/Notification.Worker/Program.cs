using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Notification.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Đăng ký PostgreSQL DbContext với kiểm tra cấu hình bắt buộc
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

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
