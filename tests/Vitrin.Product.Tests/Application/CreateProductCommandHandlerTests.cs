using FluentAssertions;
using Moq;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Contracts.Events;
using Xunit;

namespace Vitrin.Product.Tests.Application;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _handler = new CreateProductCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_Should_Create_Product_And_Return_Id()
    {
        // Arrange
        var command = new CreateProductCommand(
            MakerId: Guid.NewGuid(),
            Name: "Test Ürün",
            Tagline: "Harika bir ürün",
            Description: "Detaylı açıklama",
            Slug: "test-urun",
            Topics: new List<string> { "SaaS", "AI" },
            ThumbnailUrl: "https://img.example.com/thumb.jpg",
            GalleryUrls: null);

        _repositoryMock
            .Setup(r => r.IsSlugUniqueAsync(command.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.GetTopicBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Topic?)null); // Topics don't exist yet — will be created fresh

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ProductItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ProductItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateSlug_Should_Return_Failure()
    {
        // Arrange
        var command = new CreateProductCommand(
            MakerId: Guid.NewGuid(),
            Name: "Var Olan Ürün",
            Tagline: "Tagline",
            Description: "Açıklama",
            Slug: "var-olan-urun",
            Topics: new List<string>(),
            ThumbnailUrl: null,
            GalleryUrls: null);

        _repositoryMock
            .Setup(r => r.IsSlugUniqueAsync(command.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Slug already in use

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("slug");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ProductItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingTopics_Should_Reuse_Them()
    {
        // Arrange
        var existingTopic = Topic.Create("SaaS", "saas");

        var command = new CreateProductCommand(
            MakerId: Guid.NewGuid(),
            Name: "Ürün",
            Tagline: "Tagline",
            Description: "Açıklama",
            Slug: "urun",
            Topics: new List<string> { "SaaS" },
            ThumbnailUrl: null,
            GalleryUrls: null);

        _repositoryMock
            .Setup(r => r.IsSlugUniqueAsync(command.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.GetTopicBySlugAsync("saas", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);

        ProductItem? capturedProduct = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ProductItem>(), It.IsAny<CancellationToken>()))
            .Callback<ProductItem, CancellationToken>((p, _) => capturedProduct = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Topics.Should().ContainSingle(t => t.Id == existingTopic.Id);
    }

    [Fact]
    public async Task Handle_Should_Submit_Product_For_Review_Automatically()
    {
        // Arrange
        var command = new CreateProductCommand(
            MakerId: Guid.NewGuid(),
            Name: "Yeni Ürün",
            Tagline: "Tagline",
            Description: "Açıklama",
            Slug: "yeni-urun",
            Topics: new List<string>(),
            ThumbnailUrl: null,
            GalleryUrls: null);

        _repositoryMock
            .Setup(r => r.IsSlugUniqueAsync(command.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ProductItem? capturedProduct = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ProductItem>(), It.IsAny<CancellationToken>()))
            .Callback<ProductItem, CancellationToken>((p, _) => capturedProduct = p)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — ürün gönderildiğinde otomatik olarak inceleme kuyruğuna alınmalı
        capturedProduct!.Status.Should().Be(ProductStatus.UnderReview);
    }

    [Fact]
    public async Task Handle_WhenDatabaseDetectsConcurrentSlugConflict_ShouldReturnFailure()
    {
        var command = new CreateProductCommand(
            Guid.NewGuid(),
            "Concurrent Product",
            "Tagline",
            "Description",
            "concurrent-product",
            [],
            null,
            null);

        _repositoryMock
            .Setup(repository => repository.IsSlugUniqueAsync(command.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<ProductItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateSlugException("product", new InvalidOperationException()));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("slug");
    }
}
