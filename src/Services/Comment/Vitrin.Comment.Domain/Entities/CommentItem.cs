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
}
