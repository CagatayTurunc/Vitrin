using FluentAssertions;
using Vitrin.Comment.Domain.Entities;
using Xunit;

namespace Vitrin.Comment.Tests.Domain;

public class CommentInteractionTests
{
    [Fact]
    public void Mention_Should_Be_Unique_And_Should_Not_Mention_Author()
    {
        var authorId = Guid.NewGuid();
        var mentionedId = Guid.NewGuid();
        var comment = CommentItem.Create(Guid.NewGuid(), authorId, "author", "Hello @maker").Value;

        comment.AddMention(authorId, "author");
        comment.AddMention(mentionedId, "@Maker");
        comment.AddMention(mentionedId, "maker");

        comment.Mentions.Should().ContainSingle();
        comment.Mentions.Single().MentionedUsername.Should().Be("maker");
    }

    [Fact]
    public void Reaction_Should_Be_Upserted_Per_User()
    {
        var comment = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "author", "Useful").Value;
        var reactingUserId = Guid.NewGuid();

        comment.SetReaction(reactingUserId, "reader", CommentReactionType.Like).Should().BeTrue();
        comment.SetReaction(reactingUserId, "reader", CommentReactionType.Insightful).Should().BeFalse();

        comment.Reactions.Should().ContainSingle();
        comment.Reactions.Single().ReactionType.Should().Be(CommentReactionType.Insightful);
        comment.RemoveReaction(reactingUserId).Should().BeTrue();
        comment.Reactions.Should().BeEmpty();
    }

    [Fact]
    public void Moderate_Should_Keep_Reason_And_Moderator()
    {
        var comment = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "author", "Content").Value;
        var moderatorId = Guid.NewGuid();

        comment.Moderate(CommentModerationStatus.Hidden, moderatorId, "Harassment");

        comment.ModerationStatus.Should().Be(CommentModerationStatus.Hidden);
        comment.ModeratedByUserId.Should().Be(moderatorId);
        comment.ModerationReason.Should().Be("Harassment");
        comment.ModeratedAtUtc.Should().NotBeNull();
    }
}
