using Microsoft.EntityFrameworkCore;
using Vitrin.Ai.Domain.Entities;

namespace Vitrin.Ai.Infrastructure.Data;

public class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
    {
    }

    public DbSet<AiAnalysisResult> AiAnalysisResults { get; set; }
    public DbSet<AiUsageQuota> AiUsageQuotas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AiAnalysisResult>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.ProductId).IsRequired();
            builder.Property(a => a.Summary).IsRequired().HasMaxLength(1000);
            builder.Property(a => a.Tags).IsRequired().HasMaxLength(500);
            builder.Property(a => a.AnalyzedAt).IsRequired();
        });

        modelBuilder.Entity<AiUsageQuota>(builder =>
        {
            builder.HasKey(quota => quota.Id);
            builder.Property(quota => quota.UserId).IsRequired();
            builder.Property(quota => quota.PeriodStartUtc).IsRequired();
            builder.Property(quota => quota.RequestCount).IsRequired();
            builder.Property(quota => quota.LastRequestedAtUtc).IsRequired();
            builder.HasIndex(quota => new { quota.UserId, quota.PeriodStartUtc }).IsUnique();
        });
    }
}
