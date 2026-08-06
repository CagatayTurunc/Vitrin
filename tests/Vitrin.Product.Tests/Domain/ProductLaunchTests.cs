using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Domain.Services;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public sealed class ProductLaunchTests
{
    [Fact]
    public void CreatingProduct_ShouldCreateInitialDraftLaunch()
    {
        var product = ProductItem.Create(Guid.NewGuid(), "Vitrin", "Türkiye'nin ürün sahnesi", "Detaylı açıklama", "vitrin");

        product.Launches.Should().ContainSingle();
        product.Launches[0].ProductId.Should().Be(product.Id);
        product.Launches[0].SequenceNumber.Should().Be(1);
        product.Launches[0].Status.Should().Be(ProductLaunchStatus.Draft);
    }

    [Fact]
    public void CreatingProduct_ShouldUseLaunchSpecificVersionAndTagline()
    {
        var product = ProductItem.Create(
            Guid.NewGuid(),
            "Vitrin",
            "Kalıcı ürün kısa açıklaması",
            "Detaylı açıklama",
            "vitrin",
            initialLaunchVersionLabel: "v2.0",
            initialLaunchTagline: "Bu lansmana özel yeni deneyim");

        product.Launches.Should().ContainSingle();
        product.Launches[0].VersionLabel.Should().Be("v2.0");
        product.Launches[0].Tagline.Should().Be("Bu lansmana özel yeni deneyim");
        product.Tagline.Should().Be("Kalıcı ürün kısa açıklaması");
    }

    [Fact]
    public void ProductLifecycle_ShouldKeepInitialLaunchInSync()
    {
        var now = DateTime.UtcNow;
        var scheduled = now.AddHours(3);
        var product = ProductItem.Create(Guid.NewGuid(), "Vitrin", "Türkiye'nin ürün sahnesi", "Detaylı açıklama", "vitrin");

        product.SetScheduledLaunch(scheduled, now).IsSuccess.Should().BeTrue();
        product.SubmitForReview().IsSuccess.Should().BeTrue();
        product.Approve(now).IsSuccess.Should().BeTrue();

        product.Launches[0].Status.Should().Be(ProductLaunchStatus.Scheduled);
        product.Launches[0].ScheduledAtUtc.Should().Be(scheduled);

        product.PublishScheduled(scheduled).IsSuccess.Should().BeTrue();
        product.Launches[0].Status.Should().Be(ProductLaunchStatus.Published);
        product.Launches[0].PublishedAtUtc.Should().Be(scheduled);
    }

    [Fact]
    public void Product_ShouldAllowAtMostThreeControlledCategories()
    {
        var product = ProductItem.Create(Guid.NewGuid(), "Vitrin", "Türkiye'nin ürün sahnesi", "Detaylı açıklama", "vitrin");

        product.AddCategory(ProductCategory.Create("Bir", "bir", "Birinci kategori")).IsSuccess.Should().BeTrue();
        product.AddCategory(ProductCategory.Create("İki", "iki", "İkinci kategori")).IsSuccess.Should().BeTrue();
        product.AddCategory(ProductCategory.Create("Üç", "uc", "Üçüncü kategori")).IsSuccess.Should().BeTrue();
        var fourth = product.AddCategory(ProductCategory.Create("Dört", "dort", "Dördüncü kategori"));

        fourth.IsFailure.Should().BeTrue();
        product.Categories.Should().HaveCount(3);
    }

    [Fact]
    public void Ranking_ShouldRewardMeaningfulEngagementAndRemainIndependentFromFeaturing()
    {
        var published = DateTime.UtcNow.AddHours(-2);
        var organic = LaunchRankingService.Calculate(new LaunchRankingSignals(20, 5, 100, published, false), DateTime.UtcNow);
        var featured = LaunchRankingService.Calculate(new LaunchRankingSignals(20, 5, 100, published, true), DateTime.UtcNow);
        var lowEngagement = LaunchRankingService.Calculate(new LaunchRankingSignals(5, 0, 20, published, false), DateTime.UtcNow);

        organic.Total.Should().BeGreaterThan(lowEngagement.Total);
        featured.Total.Should().Be(organic.Total);
    }
}
