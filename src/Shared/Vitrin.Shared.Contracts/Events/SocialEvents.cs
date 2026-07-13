namespace Vitrin.Shared.Contracts.Events;

public class ProductPublishedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public Guid MakerId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;

    public ProductPublishedEvent() : base("product.published") { }
}

public class CommentAddedEvent : BaseEvent
{
    public Guid CommentId { get; set; }
    public Guid ProductId { get; set; }
    public Guid MakerId { get; set; } // The maker of the product
    public Guid UserId { get; set; } // The person who commented
    public string UserName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public CommentAddedEvent() : base("comment.added") { }
}

public class CommentRepliedEvent : BaseEvent
{
    public Guid ReplyCommentId { get; set; }
    public Guid ParentCommentId { get; set; }
    public Guid ParentCommentUserId { get; set; } // The person who owns the parent comment
    public Guid ReplyUserId { get; set; } // The person who replied
    public string ReplyUserName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public CommentRepliedEvent() : base("comment.replied") { }
}
