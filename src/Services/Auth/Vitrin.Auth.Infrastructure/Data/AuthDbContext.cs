using Microsoft.EntityFrameworkCore;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Auth.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<MakerApplication> MakerApplications => Set<MakerApplication>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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

        modelBuilder.Entity<UserFollow>(builder =>
        {
            builder.HasKey(uf => new { uf.FollowerId, uf.FollowingId });

            builder.HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(uf => uf.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserBadge>(builder =>
        {
            builder.HasKey(ub => ub.Id);
            builder.Property(ub => ub.Name).IsRequired().HasMaxLength(100);
            builder.Property(ub => ub.Icon).IsRequired().HasMaxLength(50);
            
            builder.HasOne(ub => ub.User)
                .WithMany(u => u.Badges)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.ConfigureVitrinOutbox();
    }
}
