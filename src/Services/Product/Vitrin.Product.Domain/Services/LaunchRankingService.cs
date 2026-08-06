namespace Vitrin.Product.Domain.Services;

public sealed record LaunchRankingSignals(
    int Upvotes,
    int Comments,
    int Views,
    DateTime PublishedAtUtc,
    bool IsFeatured = false);

public sealed record LaunchRankingScore(
    double Total,
    double Engagement,
    double Discovery,
    double Freshness);

/// <summary>
/// Deterministic launch ranking. Editorial featuring is intentionally not part
/// of the score so sponsored/editorial placement cannot change community rank.
/// </summary>
public static class LaunchRankingService
{
    public static LaunchRankingScore Calculate(LaunchRankingSignals signals, DateTime nowUtc)
    {
        var ageHours = Math.Max(0, (nowUtc - signals.PublishedAtUtc).TotalHours);
        var engagement = Math.Max(0, signals.Upvotes) + Math.Max(0, signals.Comments) * 1.75d;
        var discovery = Math.Log10(Math.Max(0, signals.Views) + 1d) * 2.5d;
        var freshness = Math.Max(0, 2d - ageHours / 12d);
        var total = engagement + discovery + freshness;

        return new LaunchRankingScore(
            Math.Round(total, 3),
            Math.Round(engagement, 3),
            Math.Round(discovery, 3),
            Math.Round(freshness, 3));
    }
}
