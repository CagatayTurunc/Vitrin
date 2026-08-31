using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Auth.Domain.Entities;

public class Subscription : AggregateRoot
{
    public Guid UserId { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    
    // Billing cycle
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    
    // Payment provider integration
    public string? IyzicoCustomerId { get; private set; }
    public string? IyzicoSubscriptionId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    
    // Trial management
    public DateTime? TrialEndsAt { get; private set; }
    public bool IsTrialing => TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow && Status == SubscriptionStatus.Trialing;
    
    // Cancellation
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime? CanceledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    
    // Metadata
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Legacy users protection (grandfather clause)
    public DateTime? GrandfatherUntil { get; private set; }
    public bool IsGrandfathered => GrandfatherUntil.HasValue && GrandfatherUntil.Value > DateTime.UtcNow;

    // EF Core constructor
    protected Subscription() { }

    private Subscription(
        Guid id,
        Guid userId,
        SubscriptionTier tier,
        SubscriptionStatus status,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd,
        DateTime? trialEndsAt = null,
        DateTime? grandfatherUntil = null)
        : base(id)
    {
        UserId = userId;
        Tier = tier;
        Status = status;
        CurrentPeriodStart = currentPeriodStart;
        CurrentPeriodEnd = currentPeriodEnd;
        TrialEndsAt = trialEndsAt;
        GrandfatherUntil = grandfatherUntil;
        PaymentMethod = PaymentMethod.None;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a free-tier subscription for a new user.
    /// </summary>
    public static Subscription CreateFree(Guid userId)
    {
        var now = DateTime.UtcNow;
        return new Subscription(
            Guid.NewGuid(),
            userId,
            SubscriptionTier.Free,
            SubscriptionStatus.Active,
            now,
            now.AddYears(100), // Free tier never expires
            trialEndsAt: null,
            grandfatherUntil: null);
    }

    /// <summary>
    /// Creates a subscription with a trial period.
    /// After trial ends, user must provide payment or downgrade to Free.
    /// </summary>
    public static Subscription CreateTrial(Guid userId, SubscriptionTier tier, int trialDays)
    {
        if (tier == SubscriptionTier.Free)
            throw new InvalidOperationException("Free tier does not have trials.");

        var now = DateTime.UtcNow;
        var trialEnd = now.AddDays(trialDays);

        return new Subscription(
            Guid.NewGuid(),
            userId,
            tier,
            SubscriptionStatus.Trialing,
            now,
            trialEnd,
            trialEnd,
            grandfatherUntil: null);
    }

    /// <summary>
    /// Creates a subscription with grandfather clause (legacy users).
    /// These users bypass limits until the grandfather period expires.
    /// </summary>
    public static Subscription CreateGrandfathered(Guid userId, DateTime grandfatherUntil)
    {
        var now = DateTime.UtcNow;
        return new Subscription(
            Guid.NewGuid(),
            userId,
            SubscriptionTier.Free,
            SubscriptionStatus.Active,
            now,
            now.AddYears(100),
            trialEndsAt: null,
            grandfatherUntil);
    }

    /// <summary>
    /// Upgrades subscription to a higher tier.
    /// Called after successful payment.
    /// </summary>
    public void Upgrade(
        SubscriptionTier newTier,
        string iyzicoCustomerId,
        string iyzicoSubscriptionId,
        PaymentMethod paymentMethod)
    {
        if (newTier <= Tier)
            throw new InvalidOperationException($"Cannot upgrade from {Tier} to {newTier}.");

        Tier = newTier;
        Status = SubscriptionStatus.Active;
        IyzicoCustomerId = iyzicoCustomerId;
        IyzicoSubscriptionId = iyzicoSubscriptionId;
        PaymentMethod = paymentMethod;
        
        // Reset billing cycle
        var now = DateTime.UtcNow;
        CurrentPeriodStart = now;
        CurrentPeriodEnd = now.AddMonths(1);
        
        // Clear trial and cancellation flags
        TrialEndsAt = null;
        CancelAtPeriodEnd = false;
        CanceledAt = null;
        
        UpdatedAt = now;
    }

    /// <summary>
    /// Downgrades subscription to a lower tier.
    /// Takes effect at the end of current billing period.
    /// </summary>
    public void Downgrade(SubscriptionTier newTier)
    {
        if (newTier >= Tier)
            throw new InvalidOperationException($"Cannot downgrade from {Tier} to {newTier}.");

        if (newTier == SubscriptionTier.Free)
        {
            // Immediate downgrade to Free
            Tier = SubscriptionTier.Free;
            Status = SubscriptionStatus.Active;
            IyzicoCustomerId = null;
            IyzicoSubscriptionId = null;
            PaymentMethod = PaymentMethod.None;
            CurrentPeriodEnd = DateTime.UtcNow.AddYears(100);
        }
        else
        {
            // Schedule downgrade for end of period
            Tier = newTier;
            CancelAtPeriodEnd = true;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Schedules subscription cancellation at the end of current period.
    /// User retains access until period ends.
    /// </summary>
    public void ScheduleCancellation(string reason)
    {
        if (Status == SubscriptionStatus.Canceled || Status == SubscriptionStatus.Expired)
            throw new InvalidOperationException("Subscription is already canceled or expired.");

        CancelAtPeriodEnd = true;
        CanceledAt = DateTime.UtcNow;
        CancellationReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a canceled subscription before period ends.
    /// </summary>
    public void ReactivateSubscription()
    {
        if (!CancelAtPeriodEnd)
            throw new InvalidOperationException("Subscription is not scheduled for cancellation.");

        CancelAtPeriodEnd = false;
        CanceledAt = null;
        CancellationReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mevcut bir aboneliğe grandfather clause uygular.
    /// Admin tarafından özel kullanıcılara erken erişim veya ücretsiz dönem tanımlamak için kullanılır.
    /// </summary>
    public void ApplyGrandfatherClause(DateTime grandfatherUntil)
    {
        GrandfatherUntil = grandfatherUntil;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Renews billing cycle after successful payment.
    /// Called by payment webhook handler.
    /// </summary>
    public void RenewBillingCycle()
    {
        if (Status != SubscriptionStatus.Active && Status != SubscriptionStatus.Trialing)
            throw new InvalidOperationException($"Cannot renew subscription with status {Status}.");

        CurrentPeriodStart = CurrentPeriodEnd;
        CurrentPeriodEnd = CurrentPeriodEnd.AddMonths(1);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks subscription as expired when billing period ends without payment.
    /// Called by background worker (SubscriptionExpirationWorker).
    /// </summary>
    public void MarkAsExpired()
    {
        if (Tier == SubscriptionTier.Free)
            return; // Free tier never expires

        Status = SubscriptionStatus.Expired;
        
        // Downgrade to Free after expiration
        Tier = SubscriptionTier.Free;
        IyzicoCustomerId = null;
        IyzicoSubscriptionId = null;
        PaymentMethod = PaymentMethod.None;
        CurrentPeriodEnd = DateTime.UtcNow.AddYears(100);
        
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks subscription as past due when payment fails.
    /// Retry payment after 3, 7, and 14 days.
    /// </summary>
    public void MarkAsPastDue()
    {
        Status = SubscriptionStatus.PastDue;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Converts trial to active paid subscription.
    /// Called after first successful payment.
    /// </summary>
    public void ConvertTrialToPaid(
        string iyzicoCustomerId,
        string iyzicoSubscriptionId,
        PaymentMethod paymentMethod)
    {
        if (Status != SubscriptionStatus.Trialing)
            throw new InvalidOperationException("Subscription is not in trial.");

        Status = SubscriptionStatus.Active;
        IyzicoCustomerId = iyzicoCustomerId;
        IyzicoSubscriptionId = iyzicoSubscriptionId;
        PaymentMethod = paymentMethod;
        TrialEndsAt = null;
        
        // Start billing cycle
        var now = DateTime.UtcNow;
        CurrentPeriodStart = now;
        CurrentPeriodEnd = now.AddMonths(1);
        
        UpdatedAt = now;
    }

    /// <summary>
    /// Pauses subscription temporarily (e.g., payment issue resolution period).
    /// </summary>
    public void Pause()
    {
        Status = SubscriptionStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resumes a paused subscription.
    /// </summary>
    public void Resume()
    {
        if (Status != SubscriptionStatus.Paused)
            throw new InvalidOperationException("Subscription is not paused.");

        Status = SubscriptionStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum SubscriptionTier
{
    Free = 0,
    ProMaker = 1,
    Enterprise = 2
}

public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and user has full access.
    /// </summary>
    Active = 0,

    /// <summary>
    /// User is in trial period (no payment yet).
    /// </summary>
    Trialing = 1,

    /// <summary>
    /// Payment failed, retrying. User still has access during grace period.
    /// </summary>
    PastDue = 2,

    /// <summary>
    /// User canceled subscription. Takes effect at period end.
    /// </summary>
    Canceled = 3,

    /// <summary>
    /// Billing period ended without payment. Downgraded to Free.
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Temporarily paused (e.g., dispute resolution).
    /// </summary>
    Paused = 5
}

public enum PaymentMethod
{
    None = 0,
    CreditCard = 1,
    BankTransfer = 2,
    PayPal = 3
}
