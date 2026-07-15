namespace Vitrin.Shared.Contracts.Events;

/// <summary>
/// Bir kullanıcı ürünü oyladığında Voting servisi bu event'i yayınlar.
/// Topic: voting-events
/// Product servisi consume eder → ProductUpvote tablosuna ekler.
/// Analytics servisi consume eder → upvote metriği kaydeder.
/// Notification servisi consume eder → maker'a bildirim gönderir.
/// </summary>
public class VoteAddedEvent : BaseEvent
{
    public Guid VoteId { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    public VoteAddedEvent() : base("voting.vote_added") { }
}

/// <summary>
/// Bir kullanıcı oyunu geri aldığında Voting servisi bu event'i yayınlar.
/// Topic: voting-events
/// Product servisi consume eder → ProductUpvote tablosundan siler.
/// </summary>
public class VoteRemovedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    public VoteRemovedEvent() : base("voting.vote_removed") { }
}
