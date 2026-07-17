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
    public DbSet<CommentReaction> CommentReactions { get; set; }
    public DbSet<CommentMention> CommentMentions { get; set; }
    public DbSet<CommentModerationAction> CommentModerationActions { get; set; }
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
            builder.Property(c => c.ModerationReason).HasMaxLength(500);
            builder.HasIndex(c => new { c.ProductId, c.CreatedAt, c.Id })
                .HasDatabaseName("IX_Comments_ProductId_CreatedAt_Id");
            builder.HasIndex(c => c.ParentCommentId)
                .HasDatabaseName("IX_Comments_ParentCommentId");

            builder.HasMany(c => c.Mentions)
                .WithOne()
                .HasForeignKey(mention => mention.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Reactions)
                .WithOne()
                .HasForeignKey(reaction => reaction.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommentReaction>(builder =>
        {
            builder.HasKey(reaction => reaction.Id);
            builder.Property(reaction => reaction.UserName).IsRequired().HasMaxLength(50);
            builder.HasIndex(reaction => new { reaction.CommentId, reaction.UserId }).IsUnique();
            builder.HasIndex(reaction => new { reaction.CreatedAtUtc, reaction.Id });
        });

        modelBuilder.Entity<CommentMention>(builder =>
        {
            builder.HasKey(mention => mention.Id);
            builder.Property(mention => mention.MentionedUsername).IsRequired().HasMaxLength(50);
            builder.HasIndex(mention => new { mention.CommentId, mention.MentionedUserId }).IsUnique();
            builder.HasIndex(mention => mention.MentionedUserId);
        });

        modelBuilder.Entity<CommentModerationAction>(builder =>
        {
            builder.HasKey(action => action.Id);
            builder.Property(action => action.Reason).IsRequired().HasMaxLength(500);
            builder.HasIndex(action => new { action.CreatedAtUtc, action.Id });
            builder.HasIndex(action => action.CommentId);
        });

        modelBuilder.ConfigureVitrinOutbox();
    }
}
