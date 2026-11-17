using Microsoft.EntityFrameworkCore;
using Infrastructure.Entities;
using Shared.Enums;

namespace Infrastructure.Data;

public class BulkNotificationDbContext : DbContext
{
    public BulkNotificationDbContext(DbContextOptions<BulkNotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ====================================================================
        // 1. Cấu hình Campaign
        // ====================================================================
        modelBuilder.Entity<Campaign>(b =>
        {
            b.ToTable("campaigns");

            b.HasKey(c => c.Id);
            b.Property(c => c.Id)
                .HasColumnName("id");

            b.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            b.Property(c => c.Subject)
                .HasColumnName("subject")
                .HasMaxLength(500)
                .IsRequired();

            b.Property(c => c.Body)
                .HasColumnName("body")
                .HasColumnType("text")
                .IsRequired();

            b.Property(c => c.SenderEmail)
                .HasColumnName("sender_email")
                .HasMaxLength(320)
                .IsRequired();

            b.Property(c => c.SenderName)
                .HasColumnName("sender_name")
                .HasMaxLength(200)
                .IsRequired();

            b.Property(c => c.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            b.Property(c => c.TotalRecipients)
                .HasColumnName("total_recipients")
                .IsRequired();

            b.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            b.Property(c => c.StartedAt)
                .HasColumnName("started_at")
                .HasColumnType("timestamp with time zone");

            b.Property(c => c.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp with time zone");

            // Quan hệ: Một Campaign có nhiều Delivery
            b.HasMany(c => c.Deliveries)
                .WithOne(d => d.Campaign)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ====================================================================
        // 2. Cấu hình Delivery
        // ====================================================================
        modelBuilder.Entity<Delivery>(b =>
        {
            b.ToTable("deliveries");

            b.HasKey(d => d.Id);
            b.Property(d => d.Id)
                .HasColumnName("id");

            b.Property(d => d.CampaignId)
                .HasColumnName("campaign_id")
                .IsRequired();

            b.Property(d => d.RecipientEmail)
                .HasColumnName("recipient_email")
                .HasMaxLength(320)
                .IsRequired();

            b.Property(d => d.RecipientName)
                .HasColumnName("recipient_name")
                .HasMaxLength(200);

            b.Property(d => d.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            b.Property(d => d.AttemptCount)
                .HasColumnName("attempt_count")
                .IsRequired();

            b.Property(d => d.MaxAttempts)
                .HasColumnName("max_attempts")
                .IsRequired();

            b.Property(d => d.ClaimVersion)
                .HasColumnName("claim_version")
                .IsRequired();

            b.Property(d => d.ErrorMessage)
                .HasColumnName("error_message")
                .HasColumnType("text");

            b.Property(d => d.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            b.Property(d => d.QueuedAt)
                .HasColumnName("queued_at")
                .HasColumnType("timestamp with time zone");

            b.Property(d => d.SentAt)
                .HasColumnName("sent_at")
                .HasColumnType("timestamp with time zone");

            b.Property(d => d.LastAttemptAt)
                .HasColumnName("last_attempt_at")
                .HasColumnType("timestamp with time zone");

            b.Property(d => d.NextRetryAt)
                .HasColumnName("next_retry_at")
                .HasColumnType("timestamp with time zone");

            b.Property(d => d.LeaseExpiresAt)
                .HasColumnName("lease_expires_at")
                .HasColumnType("timestamp with time zone");

            b.Property(d => d.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Quan hệ: Một Delivery thuộc đúng một Campaign (Restrict delete)
            b.HasOne(d => d.Campaign)
                .WithMany(c => c.Deliveries)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================================================================
            // 3. Chuẩn hóa Email & Unique Constraint
            // Cột Stored Computed (Shadow Property): normalized_email = lower(recipient_email)
            // Bảo đảm UNIQUE(campaign_id, normalized_email) ở cấp cơ sở dữ liệu
            // mà không làm thay đổi định nghĩa class Delivery trong C#.
            // ================================================================
            b.Property<string>("NormalizedEmail")
                .HasColumnName("normalized_email")
                .HasMaxLength(320)
                .HasComputedColumnSql("lower(recipient_email)", stored: true);

            b.HasIndex("CampaignId", "NormalizedEmail")
                .IsUnique()
                .HasDatabaseName("ix_deliveries_campaign_id_normalized_email_unique");

            // ================================================================
            // 4. Indexes phục vụ truy vấn và tác vụ nền
            // ================================================================

            // Index 1: Truy vấn danh sách Delivery theo CampaignId và Status (Web Admin/API thống kê)
            b.HasIndex(d => new { d.CampaignId, d.Status })
                .HasDatabaseName("ix_deliveries_campaign_id_status");

            // Index 2 (Partial Index): Enqueuer tìm các bản ghi Pending để nạp việc vào RabbitMQ
            // Lọc chỉ chứa các dòng có status = 0 (Pending), giúp index siêu nhỏ và nạp lô cực nhanh
            b.HasIndex(d => new { d.CampaignId, d.Id })
                .HasFilter("status = 0")
                .HasDatabaseName("ix_deliveries_campaign_id_id_pending");

            // Index 3 (Partial Index): Retry Scheduler quét các bản ghi Retrying đã đến hạn gửi lại
            // Lọc chỉ chứa các dòng có status = 3 (Retrying) và next_retry_at IS NOT NULL
            b.HasIndex(d => new { d.Status, d.NextRetryAt })
                .HasFilter("status = 3 AND next_retry_at IS NOT NULL")
                .HasDatabaseName("ix_deliveries_status_next_retry_at");

            // Index 4 (Partial Index): Stale Recovery quét các bản ghi Sending đã hết hạn Lease (Worker treo)
            // Lọc chỉ chứa các dòng có status = 2 (Sending) và lease_expires_at IS NOT NULL
            b.HasIndex(d => new { d.Status, d.LeaseExpiresAt })
                .HasFilter("status = 2 AND lease_expires_at IS NOT NULL")
                .HasDatabaseName("ix_deliveries_status_lease_expires_at");
        });
    }
}
