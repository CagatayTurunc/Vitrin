using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Infrastructure.Inbox;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Product.Infrastructure.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<ProductItem> Products { get; set; }
    public DbSet<ProductLink> ProductLinks { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<ProductUpvote> ProductUpvotes { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<ProductItem>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Slug).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("UX_Products_Slug");
            builder.Property(p => p.Tagline).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).IsRequired();

            builder.HasIndex(p => new { p.Status, p.PublishedAt, p.Id })
                .HasDatabaseName("IX_Products_Status_PublishedAt_Id");
            builder.HasIndex(p => p.MakerId)
                .HasDatabaseName("IX_Products_MakerId");
            builder.HasIndex(p => p.Name)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasDatabaseName("IX_Products_Name_Trgm");
            builder.HasIndex(p => p.Tagline)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasDatabaseName("IX_Products_Tagline_Trgm");
            builder.HasIndex(p => p.Description)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasDatabaseName("IX_Products_Description_Trgm");
            
            builder.HasMany(p => p.Links)
                   .WithOne()
                   .HasForeignKey(l => l.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Topics)
                   .WithMany();
                   
            builder.HasMany(p => p.Upvotes)
                   .WithOne()
                   .HasForeignKey(u => u.ProductItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductLink>(builder =>
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Title).IsRequired().HasMaxLength(50);
            builder.Property(l => l.Url).IsRequired().HasMaxLength(500);
        });

        modelBuilder.Entity<Topic>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Slug).IsRequired().HasMaxLength(50);
            builder.HasIndex(t => t.Slug)
                .IsUnique()
                .HasDatabaseName("UX_Topics_Slug");
            builder.HasIndex(t => t.Name)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops")
                .HasDatabaseName("IX_Topics_Name_Trgm");
        });

        modelBuilder.Entity<ProductUpvote>(builder =>
        {
            builder.HasKey(upvote => upvote.Id);
            builder.HasIndex(upvote => new { upvote.ProductItemId, upvote.UserId })
                .IsUnique()
                .HasDatabaseName("UX_ProductUpvotes_ProductId_UserId");
        });

        modelBuilder.Entity<Collection>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Slug).IsRequired().HasMaxLength(100);
            builder.HasIndex(c => c.Slug)
                .IsUnique()
                .HasDatabaseName("UX_Collections_Slug");
            builder.Property(c => c.Description).HasMaxLength(500);
            builder.HasIndex(c => new { c.UserId, c.CreatedAt })
                .HasDatabaseName("IX_Collections_UserId_CreatedAt");
            
            builder.HasMany(c => c.Products)
                   .WithMany();
        });

        modelBuilder.ConfigureVitrinInbox();
        modelBuilder.ConfigureVitrinOutbox();
    }
}
