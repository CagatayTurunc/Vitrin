namespace Vitrin.Product.Domain.Services;

public static class ProductTrendScore
{
    public const string Formula =
        "((vote×4) + (comment×6) + log10(view+1)×3 + 1) / (ageHours+2)^1.15 × 100";

    public static double Calculate(
        int votes,
        int comments,
        int views,
        DateTime publishedAt,
        DateTime utcNow)
    {
        var ageHours = Math.Max(0, (utcNow - publishedAt).TotalHours);
        var engagement =
            (Math.Max(0, votes) * 4d) +
            (Math.Max(0, comments) * 6d) +
            (Math.Log10(Math.Max(0, views) + 1d) * 3d) +
            1d;
        var freshnessDecay = Math.Pow(ageHours + 2d, 1.15d);
        return Math.Round((engagement / freshnessDecay) * 100d, 2);
    }
}
