namespace Vitrin.Shared.Contracts.Subscription;

/// <summary>
/// Service for checking subscription-based quotas and limits.
/// Used by Product, Analytics, and AI services to enforce tier-based restrictions.
/// </summary>
public interface ISubscriptionQuotaService
{
    /// <summary>
    /// Checks if user can create a new product based on their subscription tier.
    /// Free: 5 products max, Pro/Enterprise: unlimited.
    /// </summary>
    Task<QuotaCheckResult> CanCreateProductAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if user can create a new collection.
    /// Free: 1 collection, Pro: 10, Enterprise: unlimited.
    /// </summary>
    Task<QuotaCheckResult> CanCreateCollectionAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if user can add a team member to their products.
    /// Free: solo only, Pro: 3 members, Enterprise: 10 members.
    /// </summary>
    Task<QuotaCheckResult> CanAddTeamMemberAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if user can schedule a product launch for the given date.
    /// Free: no scheduling, Pro: 7 days ahead, Enterprise: 30 days ahead.
    /// </summary>
    Task<QuotaCheckResult> CanScheduleLaunchAsync(
        Guid userId,
        DateTime launchDate,
        CancellationToken ct = default);

    /// <summary>
    /// Gets remaining AI analysis quota for today.
    /// Free: 5/day, Pro: 50/day, Enterprise: 200/day.
    /// </summary>
    Task<int> GetRemainingAiQuotaAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets user's current subscription tier.
    /// Used for feature gating and UI conditional rendering.
    /// </summary>
    Task<SubscriptionTier> GetUserTierAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets comprehensive subscription info including limits and usage.
    /// Used for /settings/billing page.
    /// </summary>
    Task<SubscriptionInfo> GetSubscriptionInfoAsync(Guid userId, CancellationToken ct = default);
}

public record QuotaCheckResult(
    bool IsAllowed,
    string? DenialReason,
    SubscriptionTier RequiredTier,
    int CurrentUsage,
    int Limit)
{
    public static QuotaCheckResult Allow(int currentUsage, int limit) =>
        new(true, null, SubscriptionTier.Free, currentUsage, limit);

    public static QuotaCheckResult Deny(
        string reason,
        SubscriptionTier requiredTier,
        int currentUsage,
        int limit) =>
        new(false, reason, requiredTier, currentUsage, limit);
}

public record SubscriptionInfo(
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    DateTime CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    bool IsGrandfathered,
    QuotaUsage Usage);

public record QuotaUsage(
    int ActiveProducts,
    int ProductLimit,
    int Collections,
    int CollectionLimit,
    int TeamMembers,
    int TeamMemberLimit,
    int AiQuotaUsedToday,
    int AiQuotaLimit);

public enum SubscriptionTier
{
    Free = 0,
    ProMaker = 1,
    Enterprise = 2
}

public enum SubscriptionStatus
{
    Active = 0,
    Trialing = 1,
    PastDue = 2,
    Canceled = 3,
    Expired = 4,
    Paused = 5
}
