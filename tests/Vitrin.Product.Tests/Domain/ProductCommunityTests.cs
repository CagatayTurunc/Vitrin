using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public sealed class ProductCommunityTests
{
    [Fact]
    public void CommunityThreadCreate_ShouldCaptureProductForumContext()
    {
        var productId = Guid.NewGuid();

        var result = CommunityThread.Create(
            Guid.NewGuid(), productId, "Roadmap geri bildirimi", "roadmap-geri-bildirimi",
            "Önümüzdeki sürüm için hangi özelliği önceliklendirmeliyiz?",
            CommunityThreadCategory.Feedback, CommunityThreadKind.Feedback);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductId.Should().Be(productId);
        result.Value.Category.Should().Be(CommunityThreadCategory.Feedback);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ProductReviewCreate_ShouldRejectOutOfRangeRating(int rating)
    {
        var result = ProductReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), rating, "Gerçek deneyim",
            "Ürünü birkaç haftadır aktif olarak kullanıyorum.", ProductUsageStatus.Using);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ChangelogCreate_ShouldRequireMeaningfulBody()
    {
        var result = ProductChangelogEntry.Create(Guid.NewGuid(), Guid.NewGuid(), "2.0", "Yeni sürüm", "Kısa");

        result.IsSuccess.Should().BeFalse();
    }
}
