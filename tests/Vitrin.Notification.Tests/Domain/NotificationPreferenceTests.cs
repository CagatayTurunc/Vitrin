using FluentAssertions;
using Vitrin.Notification.Domain.Entities;
using Xunit;

namespace Vitrin.Notification.Tests.Domain;

public class NotificationPreferenceTests
{
    [Fact]
    public void Update_Should_Normalize_Email_And_Apply_Category_Preferences()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        var now = DateTime.UtcNow;

        preference.Update(
            "  Maker@Example.COM ",
            inAppEnabled: true,
            emailEnabled: true,
            EmailDigestFrequency.Daily,
            productUpdatesEnabled: true,
            commentsEnabled: false,
            mentionsEnabled: true,
            reactionsEnabled: false,
            socialEnabled: true,
            moderationEnabled: true,
            utcNow: now);

        preference.EmailAddress.Should().Be("maker@example.com");
        preference.EmailEnabled.Should().BeTrue();
        preference.DigestFrequency.Should().Be(EmailDigestFrequency.Daily);
        preference.AllowsType("comment_on_product").Should().BeFalse();
        preference.AllowsType("comment_mention").Should().BeTrue();
        preference.AllowsType("comment_reaction").Should().BeFalse();
        preference.AllowsType("saved_search_match").Should().BeTrue();
    }

    [Fact]
    public void Digest_Should_Be_Due_After_The_Selected_Interval()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid(), "maker@example.com");
        var now = DateTime.UtcNow;
        preference.Update(
            preference.EmailAddress, true, true, EmailDigestFrequency.Weekly,
            true, true, true, true, true, true, now);

        preference.IsDigestDue(now).Should().BeTrue();
        preference.MarkDigestSent(now);
        preference.IsDigestDue(now.AddDays(6)).Should().BeFalse();
        preference.IsDigestDue(now.AddDays(7)).Should().BeTrue();
    }

    [Fact]
    public void Email_Channel_Should_Turn_Off_When_Address_Is_Empty()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid(), "maker@example.com");

        preference.Update(
            null, true, true, EmailDigestFrequency.Daily,
            true, true, true, true, true, true, DateTime.UtcNow);

        preference.EmailEnabled.Should().BeFalse();
        preference.DigestFrequency.Should().Be(EmailDigestFrequency.Off);
    }
}
