using Microsoft.EntityFrameworkCore;
using Vitrin.Analytics.Domain.Entities;
using Vitrin.Shared.Infrastructure.Inbox;

namespace Vitrin.Analytics.Infrastructure.Data;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AnalyticsEvent>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.EventType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.CreatedAt).IsRequired();
        });

        modelBuilder.ConfigureVitrinInbox();
    }
}
