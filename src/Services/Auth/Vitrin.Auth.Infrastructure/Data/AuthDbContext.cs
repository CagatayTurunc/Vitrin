using Microsoft.EntityFrameworkCore;
using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<MakerApplication> MakerApplications => Set<MakerApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.FullName).HasMaxLength(100);
            builder.Property(u => u.AvatarUrl).HasMaxLength(1000);
            
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Username).IsUnique();
        });
    }
}
