using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Comment.Domain.Entities;

public class CommentItem : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Guid? ParentCommentId { get; private set; }

    private CommentItem() { } // EF Core

    public static Result<CommentItem> Create(Guid productId, Guid userId, string content, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result<CommentItem>.Failure("Comment content cannot be empty.");

        var comment = new CommentItem
        {
            ProductId = productId,
            UserId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            ParentCommentId = parentCommentId
        };

        return Result<CommentItem>.Success(comment);
    }
}
