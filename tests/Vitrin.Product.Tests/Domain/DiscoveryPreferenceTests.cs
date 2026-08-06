using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public class DiscoveryPreferenceTests
{
    [Fact]
    public void SavedSearch_Should_Normalize_Topics_And_Match_A_Published_Product()
    {
        var now = DateTime.UtcNow;
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "  Tasarım araçları  ",
            "studio",
            [" Design ", "design", "AI"],
            minUpvotes: 1,
            minComments: 1,
            minViews: 1,
            publishedFrom: now.AddDays(-1),
            publishedTo: now.AddDays(1),
            sort: "TRENDING",
            notifyOnNewMatches: true,
            utcNow: now);

        var product = ProductItem.Create(Guid.NewGuid(), "Pixel Studio", "Design faster", "A creative studio", "pixel-studio");
        product.AddTopic(Topic.Create("Design", "design"));
        product.ToggleUpvote(Guid.NewGuid());
        product.RecordComment();
        product.RecordView();
        product.Publish().IsSuccess.Should().BeTrue();

        search.Name.Should().Be("Tasarım araçları");
        search.Sort.Should().Be("trending");
        search.GetTopicSlugs().Should().Equal("design", "ai");
        search.Matches(product).Should().BeTrue();
    }

    [Fact]
    public void SavedSearch_Should_Not_Match_When_A_Threshold_Is_Missing()
    {
        var now = DateTime.UtcNow;
        var search = SavedSearch.Create(
            Guid.NewGuid(), "Popüler", null, null,
            minUpvotes: 2, minComments: null, minViews: null,
            publishedFrom: null, publishedTo: null,
            sort: "newest", notifyOnNewMatches: true, utcNow: now);
        var product = ProductItem.Create(Guid.NewGuid(), "Ürün", "Tagline", "Açıklama", "urun");
        product.ToggleUpvote(Guid.NewGuid());

        search.Matches(product).Should().BeFalse();
    }

    [Fact]
    public void TopicFollow_Should_Keep_User_Topic_And_Creation_Time()
    {
        var userId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var follow = TopicFollow.Create(userId, topicId, now);

        follow.UserId.Should().Be(userId);
        follow.TopicId.Should().Be(topicId);
        follow.CreatedAtUtc.Should().Be(now);
    }
}
