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
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationItem>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.UserId).IsRequired();
            builder.Property(n => n.Message).IsRequired().HasMaxLength(500);
            builder.Property(n => n.NotificationType).HasMaxLength(50);
            builder.Property(n => n.RelatedEntityId);
            builder.Property(n => n.IsRead).IsRequired();
            builder.Property(n => n.CreatedAt).IsRequired();
            builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt, n.Id })
                .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt_Id");
        });

        modelBuilder.Entity<NotificationPreference>(builder =>
        {
            builder.HasKey(preference => preference.Id);
            builder.Property(preference => preference.EmailAddress).HasMaxLength(254);
            builder.Property(preference => preference.DigestFrequency).IsRequired();
            builder.HasIndex(preference => preference.UserId)
                .IsUnique()
                .HasDatabaseName("UX_NotificationPreferences_UserId");
        });

        modelBuilder.Entity<NewsletterSubscription>(builder =>
        {
            builder.HasKey(subscription => subscription.Id);
            builder.Property(subscription => subscription.EmailAddress).IsRequired().HasMaxLength(254);
            builder.HasIndex(subscription => subscription.EmailAddress).IsUnique()
                .HasDatabaseName("UX_NewsletterSubscriptions_EmailAddress");
            builder.HasIndex(subscription => subscription.UserId)
                .IsUnique()
                .HasFilter("UserId IS NOT NULL")
                .HasDatabaseName("UX_NewsletterSubscriptions_UserId");
        });

        modelBuilder.ConfigureVitrinInbox();
    }
}
