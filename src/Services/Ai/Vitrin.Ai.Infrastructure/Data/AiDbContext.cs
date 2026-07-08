using Microsoft.EntityFrameworkCore;
using Vitrin.Ai.Domain.Entities;

namespace Vitrin.Ai.Infrastructure.Data;

public class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
    {
    }

    public DbSet<AiAnalysisResult> AiAnalysisResults { get; set; }

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
    }
}
