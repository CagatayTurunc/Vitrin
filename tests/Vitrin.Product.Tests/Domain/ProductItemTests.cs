using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public class ProductItemTests
{
    private static ProductItem CreateTestProduct(string? makerId = null)
    {
        var id = makerId is not null ? Guid.Parse(makerId) : Guid.NewGuid();
        return ProductItem.Create(id, "Test Ürün", "Harika bir ürün", "Detaylı açıklama", "test-urun");
    }

    [Fact]
    public void Create_Should_Create_Product_With_Draft_Status()
    {
        // Act
        var product = ProductItem.Create(Guid.NewGuid(), "Ürün Adı", "Tagline", "Açıklama", "urun-adi");

        // Assert
        product.Name.Should().Be("Ürün Adı");
        product.Tagline.Should().Be("Tagline");
        product.Slug.Should().Be("urun-adi");
        product.Status.Should().Be(ProductStatus.Draft);
        product.Id.Should().NotBeEmpty();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SubmitForReview_From_Draft_Should_Succeed()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Status.Should().Be(ProductStatus.Draft);

        // Act
        var result = product.SubmitForReview();

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.UnderReview);
    }

    [Fact]
    public void SubmitForReview_From_UnderReview_Should_Fail()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SubmitForReview();

        // Act
        var result = product.SubmitForReview();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("review");
    }

    [Fact]
    public void Approve_From_UnderReview_Should_Publish()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SubmitForReview();

        // Act
        var result = product.Approve();

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Published);
        product.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_From_Draft_Should_Fail()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var result = product.Approve();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("review");
    }

    [Fact]
    public void Reject_From_UnderReview_Should_Reject()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SubmitForReview();

        // Act
        var result = product.Reject();

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Rejected);
    }

    [Fact]
    public void Reject_From_Published_Should_Fail()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SubmitForReview();
        product.Approve();

        // Act
        var result = product.Reject();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SubmitForReview_From_Rejected_Should_Succeed()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SubmitForReview();
        product.Reject();
        product.Status.Should().Be(ProductStatus.Rejected);

        // Act
        var result = product.SubmitForReview();

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.UnderReview);
    }

    [Fact]
    public void Publish_Already_Published_Should_Fail()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SubmitForReview();
        product.Approve();

        // Act
        var result = product.Publish();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already published");
    }

    [Fact]
    public void ToggleUpvote_NewUser_Should_Add_Upvote()
    {
        // Arrange
        var product = CreateTestProduct();
        var userId = Guid.NewGuid();

        // Act
        product.ToggleUpvote(userId);

        // Assert
        product.Upvotes.Should().HaveCount(1);
        product.Upvotes.First().UserId.Should().Be(userId);
    }

    [Fact]
    public void ToggleUpvote_SameUser_Twice_Should_Remove_Upvote()
    {
        // Arrange
        var product = CreateTestProduct();
        var userId = Guid.NewGuid();

        // Act
        product.ToggleUpvote(userId);
        product.ToggleUpvote(userId);

        // Assert
        product.Upvotes.Should().BeEmpty();
    }

    [Fact]
    public void AddTopic_Should_Add_Topic()
    {
        // Arrange
        var product = CreateTestProduct();
        var topic = Topic.Create("SaaS", "saas");

        // Act
        product.AddTopic(topic);

        // Assert
        product.Topics.Should().HaveCount(1);
        product.Topics.First().Name.Should().Be("SaaS");
    }

    [Fact]
    public void AddTopic_Duplicate_Should_Not_Add_Again()
    {
        // Arrange
        var product = CreateTestProduct();
        var topic = Topic.Create("SaaS", "saas");

        // Act
        product.AddTopic(topic);
        product.AddTopic(topic);

        // Assert
        product.Topics.Should().HaveCount(1);
    }

    [Fact]
    public void SetGalleryUrls_Should_Replace_Existing()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SetGalleryUrls(new[] { "https://img1.com", "https://img2.com" });

        // Act
        product.SetGalleryUrls(new[] { "https://new.com" });

        // Assert
        product.GalleryUrls.Should().HaveCount(1);
        product.GalleryUrls.First().Should().Be("https://new.com");
    }

    [Fact]
    public void AddLink_Should_Add_Link()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.AddLink("Website", "https://example.com");

        // Assert
        product.Links.Should().HaveCount(1);
        product.Links.First().Title.Should().Be("Website");
    }
}
