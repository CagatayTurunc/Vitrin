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
    public DbSet<ModerationReport> ModerationReports => Set<ModerationReport>();
    public DbSet<UserBan> UserBans => Set<UserBan>();
    public DbSet<ModerationAppeal> ModerationAppeals => Set<ModerationAppeal>();
    public DbSet<ModerationAuditEntry> ModerationAuditEntries => Set<ModerationAuditEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255).HasColumnType("citext");
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50).HasColumnType("citext");
            builder.Property(u => u.FullName).HasMaxLength(100);
            builder.Property(u => u.AvatarUrl).HasMaxLength(1000);
            builder.Property(u => u.SuspensionReason).HasMaxLength(500);
            
            builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_Users_Email");
            builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("UX_Users_Username");
            builder.HasIndex(u => u.GoogleId).IsUnique().HasDatabaseName("UX_Users_GoogleId");
            builder.HasIndex(u => u.GithubId).IsUnique().HasDatabaseName("UX_Users_GithubId");
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

            builder.HasIndex(uf => new { uf.FollowingId, uf.CreatedAt })
                .HasDatabaseName("IX_UserFollows_FollowingId_CreatedAt");
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

        modelBuilder.Entity<MakerApplication>(builder =>
        {
            builder.HasIndex(application => new { application.Status, application.CreatedAt })
                .HasDatabaseName("IX_MakerApplications_Status_CreatedAt");
        });

        modelBuilder.Entity<ModerationReport>(builder =>
        {
            builder.HasKey(report => report.Id);
            builder.Property(report => report.Details).IsRequired().HasMaxLength(2000);
            builder.Property(report => report.Resolution).HasMaxLength(1000);
            builder.HasIndex(report => new { report.Status, report.CreatedAtUtc });
            builder.HasIndex(report => new { report.ReporterUserId, report.TargetType, report.TargetId });
            builder.HasOne<User>().WithMany().HasForeignKey(report => report.ReporterUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserBan>(builder =>
        {
            builder.HasKey(ban => ban.Id);
            builder.Property(ban => ban.Reason).IsRequired().HasMaxLength(1000);
            builder.Property(ban => ban.RevocationReason).HasMaxLength(1000);
            builder.HasIndex(ban => new { ban.UserId, ban.CreatedAtUtc });
            builder.HasIndex(ban => new { ban.RevokedAtUtc, ban.ExpiresAtUtc });
            builder.HasOne<User>().WithMany().HasForeignKey(ban => ban.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ModerationAppeal>(builder =>
        {
            builder.HasKey(appeal => appeal.Id);
            builder.Property(appeal => appeal.Statement).IsRequired().HasMaxLength(3000);
            builder.Property(appeal => appeal.ReviewNote).HasMaxLength(1000);
            builder.HasIndex(appeal => new { appeal.Status, appeal.CreatedAtUtc });
            builder.HasIndex(appeal => new { appeal.BanId, appeal.UserId });
            builder.HasOne<UserBan>().WithMany().HasForeignKey(appeal => appeal.BanId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<User>().WithMany().HasForeignKey(appeal => appeal.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ModerationAuditEntry>(builder =>
        {
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Action).IsRequired().HasMaxLength(100);
            builder.Property(entry => entry.ResourceType).IsRequired().HasMaxLength(100);
            builder.Property(entry => entry.ResourceId).HasMaxLength(100);
            builder.Property(entry => entry.Outcome).IsRequired().HasMaxLength(50);
            builder.Property(entry => entry.TraceId).HasMaxLength(100);
            builder.Property(entry => entry.Details).HasMaxLength(4000);
            builder.HasIndex(entry => new { entry.OccurredAtUtc, entry.Id });
            builder.HasIndex(entry => new { entry.ResourceType, entry.ResourceId });
        });

        modelBuilder.ConfigureVitrinOutbox();
    }
}
