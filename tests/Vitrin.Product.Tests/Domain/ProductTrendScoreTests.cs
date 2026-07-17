using FluentAssertions;
using Vitrin.Product.Domain.Services;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public sealed class ProductTrendScoreTests
{
    [Fact]
    public void More_Engagement_Should_Increase_Trend_Score()
    {
        var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
        var publishedAt = now.AddHours(-6);

        var quietScore = ProductTrendScore.Calculate(2, 0, 10, publishedAt, now);
        var activeScore = ProductTrendScore.Calculate(12, 4, 500, publishedAt, now);

        activeScore.Should().BeGreaterThan(quietScore);
    }

    [Fact]
    public void Older_Product_Should_Decay_With_Same_Engagement()
    {
        var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

        var freshScore = ProductTrendScore.Calculate(10, 3, 300, now.AddHours(-3), now);
        var oldScore = ProductTrendScore.Calculate(10, 3, 300, now.AddDays(-7), now);

        freshScore.Should().BeGreaterThan(oldScore);
    }

    [Fact]
    public void Views_Should_Have_Logarithmic_Influence()
    {
        var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
        var publishedAt = now.AddHours(-2);

        var firstIncrease = ProductTrendScore.Calculate(0, 0, 100, publishedAt, now)
            - ProductTrendScore.Calculate(0, 0, 0, publishedAt, now);
        var laterIncrease = ProductTrendScore.Calculate(0, 0, 10_000, publishedAt, now)
            - ProductTrendScore.Calculate(0, 0, 9_900, publishedAt, now);

        firstIncrease.Should().BeGreaterThan(laterIncrease);
    }
}
