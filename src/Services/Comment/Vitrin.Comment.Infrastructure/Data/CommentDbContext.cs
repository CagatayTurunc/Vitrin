using Microsoft.EntityFrameworkCore;
using Vitrin.Comment.Domain.Entities;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Comment.Infrastructure.Data;

public class CommentDbContext : DbContext
{
    public CommentDbContext(DbContextOptions<CommentDbContext> options) : base(options)
    {
    }

    public DbSet<CommentItem> Comments { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CommentItem>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.ProductId).IsRequired();
            builder.Property(c => c.UserId).IsRequired();
            builder.Property(c => c.Content).IsRequired().HasMaxLength(1000);
            builder.Property(c => c.CreatedAt).IsRequired();
        });

        modelBuilder.ConfigureVitrinOutbox();
    }
}
