namespace Vitrin.Shared.Contracts.Events;

/// <summary>
/// Bir ürün sayfası görüntülendiğinde yayınlanır.
/// Topic: analytics-events
/// </summary>
public class ProductViewedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductSlug { get; set; } = string.Empty;
    public Guid? UserId { get; set; }       // null → anonim ziyaret
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }

    public ProductViewedEvent() : base("analytics.product_viewed") { }
}

/// <summary>
/// Bir ürün upvote/downvote aldığında yayınlanır.
/// Topic: analytics-events
/// </summary>
public class ProductUpvotedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string ProductSlug { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public bool IsUpvote { get; set; }      // true = yeni oy, false = geri alınan oy

    public ProductUpvotedEvent() : base("analytics.product_upvoted") { }
}

/// <summary>
/// Bir arama yapıldığında yayınlanır.
/// Topic: analytics-events
/// </summary>
public class SearchPerformedEvent : BaseEvent
{
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public Guid? UserId { get; set; }

    public SearchPerformedEvent() : base("analytics.search_performed") { }
}

/// <summary>
/// Bir ürüne yorum yapıldığında yayınlanır.
/// Topic: analytics-events
/// </summary>
public class CommentCreatedAnalyticsEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    public bool IsReply { get; set; }

    public CommentCreatedAnalyticsEvent() : base("analytics.comment_created") { }
}
