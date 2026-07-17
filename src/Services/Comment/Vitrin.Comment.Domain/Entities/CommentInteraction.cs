using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Comment.Domain.Entities;

public enum CommentReactionType
{
    Like = 0,
    Love = 1,
    Insightful = 2,
    Celebrate = 3
}

public sealed class CommentReaction : Entity
{
    public Guid CommentId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public CommentReactionType ReactionType { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private CommentReaction() { }

    public static CommentReaction Create(
        Guid commentId,
        Guid userId,
        string userName,
        CommentReactionType reactionType) => new()
    {
        CommentId = commentId,
        UserId = userId,
        UserName = userName.Trim().ToLowerInvariant(),
        ReactionType = reactionType,
        CreatedAtUtc = DateTime.UtcNow
    };

    public void Change(CommentReactionType reactionType)
    {
        if (ReactionType == reactionType) return;
        ReactionType = reactionType;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class CommentMention : Entity
{
    public Guid CommentId { get; private set; }
    public Guid MentionedUserId { get; private set; }
    public string MentionedUsername { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private CommentMention() { }

    public static CommentMention Create(Guid commentId, Guid mentionedUserId, string mentionedUsername) => new()
    {
        CommentId = commentId,
        MentionedUserId = mentionedUserId,
        MentionedUsername = mentionedUsername,
        CreatedAtUtc = DateTime.UtcNow
    };
}

public sealed class CommentModerationAction : Entity
{
    public Guid CommentId { get; private set; }
    public Guid ModeratorUserId { get; private set; }
    public CommentModerationStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private CommentModerationAction() { }

    public static CommentModerationAction Create(
        Guid commentId,
        Guid moderatorUserId,
        CommentModerationStatus status,
        string reason) => new()
    {
        CommentId = commentId,
        ModeratorUserId = moderatorUserId,
        Status = status,
        Reason = reason.Trim(),
        CreatedAtUtc = DateTime.UtcNow
    };
}
