using FluentAssertions;
using Vitrin.Auth.Domain.Entities;
using Xunit;

namespace Vitrin.Auth.Tests.Domain;

public class ModerationTests
{
    [Fact]
    public void Report_Should_Require_Meaningful_Details_And_Close_Once_Resolved()
    {
        ModerationReport.Create(
                Guid.NewGuid(),
                ModerationTargetType.Comment,
                Guid.NewGuid(),
                Guid.NewGuid(),
                ReportCategory.Harassment,
                "short")
            .IsSuccess.Should().BeFalse();

        var result = ModerationReport.Create(
            Guid.NewGuid(),
            ModerationTargetType.Comment,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportCategory.Harassment,
            "This comment contains targeted harassment.");

        result.IsSuccess.Should().BeTrue();
        result.Value.Resolve(Guid.NewGuid(), "Comment hidden", dismissed: false);
        result.Value.Status.Should().Be(ModerationCaseStatus.Resolved);
        result.Value.Resolution.Should().Be("Comment hidden");
    }

    [Fact]
    public void Ban_And_Approved_Appeal_Should_Lift_User_Suspension()
    {
        var user = User.CreateWithPassword("member@example.com", "member", "Member", "hash");
        var moderatorId = Guid.NewGuid();
        var ban = UserBan.Create(user.Id, moderatorId, "Repeated abuse", DateTime.UtcNow.AddDays(7));
        user.Suspend(ban.Id, ban.Reason, ban.ExpiresAtUtc);

        user.IsBanned(DateTime.UtcNow).Should().BeTrue();

        var appeal = ModerationAppeal.Create(
            ban.Id,
            user.Id,
            "I understand the rule and request a second review.").Value;
        appeal.Review(moderatorId, approved: true, "Context reviewed");
        ban.Revoke(moderatorId, "Appeal approved");
        user.LiftSuspension();

        appeal.Status.Should().Be(AppealStatus.Approved);
        ban.IsActive(DateTime.UtcNow).Should().BeFalse();
        user.IsBanned(DateTime.UtcNow).Should().BeFalse();
    }
}
