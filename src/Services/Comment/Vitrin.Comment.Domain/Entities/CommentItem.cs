using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Comment.Domain.Entities;

public class CommentItem : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Guid? ParentCommentId { get; private set; }
    public bool IsDeleted { get; private set; } = false;
    public DateTime? UpdatedAt { get; private set; }
    public CommentModerationStatus ModerationStatus { get; private set; } = CommentModerationStatus.Visible;
    public string? ModerationReason { get; private set; }
    public Guid? ModeratedByUserId { get; private set; }
    public DateTime? ModeratedAtUtc { get; private set; }

    private readonly List<CommentMention> _mentions = new();
    public IReadOnlyCollection<CommentMention> Mentions => _mentions.AsReadOnly();

    private readonly List<CommentReaction> _reactions = new();
    public IReadOnlyCollection<CommentReaction> Reactions => _reactions.AsReadOnly();

    private CommentItem() { } // EF Core

    public static Result<CommentItem> Create(Guid productId, Guid userId, string userName, string content, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result<CommentItem>.Failure("Comment content cannot be empty.");

        var comment = new CommentItem
        {
            ProductId = productId,
            UserId = userId,
            UserName = userName,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            ParentCommentId = parentCommentId
        };

        return Result<CommentItem>.Success(comment);
    }

    public Result<string> UpdateContent(string newContent)
    {
        if (IsDeleted)
            return Result<string>.Failure("Cannot update a deleted comment.");
        
        if (string.IsNullOrWhiteSpace(newContent))
            return Result<string>.Failure("Comment content cannot be empty.");

        Content = newContent;
        UpdatedAt = DateTime.UtcNow;

        return Result<string>.Success("Comment updated.");
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        // Optionally clear content for GDPR compliance, but soft delete usually leaves it or replaces it.
        // We'll let the frontend handle IsDeleted flag.
    }

    public void AddMention(Guid mentionedUserId, string mentionedUsername)
    {
        var normalizedUsername = mentionedUsername.Trim().TrimStart('@').ToLowerInvariant();
        if (mentionedUserId == UserId || string.IsNullOrWhiteSpace(normalizedUsername)) return;
        if (_mentions.Any(mention => mention.MentionedUserId == mentionedUserId)) return;

        _mentions.Add(CommentMention.Create(Id, mentionedUserId, normalizedUsername));
    }

    public bool SetReaction(Guid userId, string userName, CommentReactionType reactionType)
    {
        var existing = _reactions.FirstOrDefault(reaction => reaction.UserId == userId);
        if (existing is not null)
        {
            existing.Change(reactionType);
            return false;
        }

        _reactions.Add(CommentReaction.Create(Id, userId, userName, reactionType));
        return true;
    }

    public bool RemoveReaction(Guid userId)
    {
        var existing = _reactions.FirstOrDefault(reaction => reaction.UserId == userId);
        return existing is not null && _reactions.Remove(existing);
    }

    public void Moderate(CommentModerationStatus status, Guid moderatorUserId, string reason)
    {
        ModerationStatus = status;
        ModerationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ModeratedByUserId = moderatorUserId;
        ModeratedAtUtc = DateTime.UtcNow;
    }
}

public enum CommentModerationStatus
{
    Visible = 0,
    Hidden = 1
}
