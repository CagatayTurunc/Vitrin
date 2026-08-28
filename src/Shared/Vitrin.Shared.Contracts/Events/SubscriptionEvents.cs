namespace Vitrin.Shared.Contracts.Events;

/// <summary>
/// Auth service'ten yayınlanır; kullanıcı aboneliğini yükselttiğinde.
/// Product service bu event'i tüketerek ürünlerin MakerTierSnapshot alanını günceller.
/// Topic: subscription-events
/// </summary>
public class SubscriptionUpgradedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string OldTier { get; set; } = string.Empty;
    public string NewTier { get; set; } = string.Empty;

    public SubscriptionUpgradedEvent() : base("subscription.upgraded") { }
}

/// <summary>
/// Abonelik iptal edildiğinde yayınlanır.
/// Topic: subscription-events
/// </summary>
public class SubscriptionCanceledEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public DateTime CanceledAt { get; set; }

    public SubscriptionCanceledEvent() : base("subscription.canceled") { }
}
