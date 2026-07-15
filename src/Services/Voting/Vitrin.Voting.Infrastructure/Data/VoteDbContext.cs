using Microsoft.EntityFrameworkCore;
using Vitrin.Shared.Infrastructure.Outbox;
using Vitrin.Voting.Domain.Entities;

namespace Vitrin.Voting.Infrastructure.Data;

public class VoteDbContext : DbContext
{
    public VoteDbContext(DbContextOptions<VoteDbContext> options) : base(options)
    {
    }

    public DbSet<Vote> Votes { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vote>(builder =>
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.UserId).IsRequired();
            builder.Property(v => v.ProductId).IsRequired();
            builder.Property(v => v.CreatedAt).IsRequired();
            
            // Kullanıcı bir ürüne sadece 1 kez oy verebilir.
            builder.HasIndex(v => new { v.UserId, v.ProductId }).IsUnique();
            builder.HasIndex(v => new { v.ProductId, v.CreatedAt })
                .HasDatabaseName("IX_Votes_ProductId_CreatedAt");
        });

        modelBuilder.ConfigureVitrinOutbox();
    }
}
