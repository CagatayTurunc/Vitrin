using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;

namespace Vitrin.Product.Infrastructure.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<ProductItem> Products { get; set; }
    public DbSet<ProductLink> ProductLinks { get; set; }
    public DbSet<Topic> Topics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductItem>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Slug).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => p.Slug).IsUnique();
            builder.Property(p => p.Tagline).IsRequired().HasMaxLength(200);
            
            builder.HasMany(p => p.Links)
                   .WithOne()
                   .HasForeignKey(l => l.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Topics)
                   .WithMany();
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
            builder.HasIndex(t => t.Slug).IsUnique();
        });
    }
}
