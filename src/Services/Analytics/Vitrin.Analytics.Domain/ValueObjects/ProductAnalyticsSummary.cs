namespace Vitrin.Analytics.Domain.ValueObjects;

/// <summary>
/// Bir ürünün analytics özetini tutan değer nesnesi.
/// </summary>
public record ProductAnalyticsSummary(
    Guid ProductId,
    int Views,
    int Upvotes,
    int Downvotes,
    int Comments,
    DateTime ComputedAt)
{
    public int NetUpvotes => Upvotes - Downvotes;
}

/// <summary>
/// Platform genelinde özet istatistikler.
/// </summary>
public record PlatformAnalyticsSummary(
    int TotalEvents,
    int TotalProductViews,
    int TotalUpvotes,
    int TotalSearches,
    int TotalComments,
    int TotalUserRegistrations,
    DateTime ComputedAt);
