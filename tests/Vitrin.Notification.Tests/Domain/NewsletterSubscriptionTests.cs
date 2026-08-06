using FluentAssertions;
using Vitrin.Notification.Domain.Entities;
using Xunit;

namespace Vitrin.Notification.Tests.Domain;

public sealed class NewsletterSubscriptionTests
{
    [Fact]
    public void Create_ShouldNormalizeEmailAndEnableWeeklyRoundup()
    {
        var subscription = NewsletterSubscription.Create("  Maker@Example.com ", Guid.NewGuid());

        subscription.EmailAddress.Should().Be("maker@example.com");
        subscription.WeeklyRoundup.Should().BeTrue();
        subscription.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldPersistSelectedNewsletterMix()
    {
        var subscription = NewsletterSubscription.Create("maker@example.com");

        subscription.Update(Guid.NewGuid(), true, false, true, true, false, true, true);

        subscription.DailyLaunches.Should().BeTrue();
        subscription.WeeklyRoundup.Should().BeFalse();
        subscription.ProductUpdates.Should().BeTrue();
        subscription.DeveloperDigest.Should().BeTrue();
    }
}
