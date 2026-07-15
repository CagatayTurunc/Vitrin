using FluentAssertions;
using Vitrin.Ai.Domain.Entities;
using Xunit;

namespace Vitrin.Ai.Tests.Domain;

public sealed class AiUsageQuotaTests
{
    [Fact]
    public void TryConsume_stops_at_the_daily_limit()
    {
        var now = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var quota = AiUsageQuota.Create(Guid.NewGuid(), now.Date, now);

        quota.TryConsume(2, now.AddMinutes(1)).Should().BeTrue();
        quota.TryConsume(2, now.AddMinutes(2)).Should().BeFalse();
        quota.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Create_rejects_an_empty_user_id()
    {
        var act = () => AiUsageQuota.Create(Guid.Empty, DateTime.UtcNow.Date, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }
}
