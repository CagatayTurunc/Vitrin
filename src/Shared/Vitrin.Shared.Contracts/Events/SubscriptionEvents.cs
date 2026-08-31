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

/// <summary>
/// Ödeme başarılı — abonelik bir ay uzatıldığında yayınlanır.
/// Topic: subscription-events
/// </summary>
public class SubscriptionRenewedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public DateTime NewPeriodEnd { get; set; }
    public decimal PaidAmount { get; set; }
    public string Currency { get; set; } = "TRY";

    public SubscriptionRenewedEvent() : base("subscription.renewed") { }
}

/// <summary>
/// Ödeme alınamadı — abonelik expired olarak işaretlendi, Free'ye düşürüldü.
/// Topic: subscription-events
/// </summary>
public class SubscriptionExpiredEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string ExpiredTier { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }

    public SubscriptionExpiredEvent() : base("subscription.expired") { }
}

/// <summary>
/// Ödeme başarısız — past due duruma geçildi, retry planlandı.
/// Topic: subscription-events
/// </summary>
public class SubscriptionPaymentFailedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public SubscriptionPaymentFailedEvent() : base("subscription.payment_failed") { }
}
