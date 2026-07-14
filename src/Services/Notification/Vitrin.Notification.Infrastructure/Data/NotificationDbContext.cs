using Microsoft.EntityFrameworkCore;
using Vitrin.Notification.Domain.Entities;
using Vitrin.Shared.Infrastructure.Inbox;

namespace Vitrin.Notification.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationItem> Notifications { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationItem>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.UserId).IsRequired();
            builder.Property(n => n.Message).IsRequired().HasMaxLength(500);
            builder.Property(n => n.IsRead).IsRequired();
            builder.Property(n => n.CreatedAt).IsRequired();
            builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt, n.Id })
                .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt_Id");
        });

        modelBuilder.ConfigureVitrinInbox();
    }
}
