using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations, SQL script generation, and database update.
/// Reads connection string from environment variable 'ConnectionStrings__DefaultConnection' if present.
/// Falls back to a safe placeholder for offline tooling (model building / script generation).
/// </summary>
public class BulkNotificationDbContextFactory : IDesignTimeDbContextFactory<BulkNotificationDbContext>
{
    public BulkNotificationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BulkNotificationDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Design-time connection string placeholder required by Npgsql provider when running offline
            connectionString = "Host=localhost;Port=5432;Database=bulk_notification_design;Username=postgres;Password=postgres";
        }

        optionsBuilder.UseNpgsql(connectionString);

        return new BulkNotificationDbContext(optionsBuilder.Options);
    }
}
