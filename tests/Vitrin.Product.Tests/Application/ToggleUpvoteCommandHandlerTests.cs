using FluentAssertions;
using Moq;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Contracts.Events;
using Xunit;

namespace Vitrin.Product.Tests.Application;

public class ToggleUpvoteCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IProductEventPublisher> _eventPublisherMock;
    private readonly ToggleUpvoteCommandHandler _handler;

    public ToggleUpvoteCommandHandlerTests()
    {
        _repositoryMock     = new Mock<IProductRepository>();
        _eventPublisherMock = new Mock<IProductEventPublisher>();
        _handler = new ToggleUpvoteCommandHandler(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    private static ProductItem CreatePublishedProduct(Guid makerId)
    {
        var p = ProductItem.Create(makerId, "Test Ürün", "Tagline", "Açıklama", "test-urun");
        p.SubmitForReview();
        p.Approve();
        return p;
    }

    [Fact]
    public async Task Handle_ProductNotFound_Should_Return_Failure()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdWithUpvotesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductItem?)null);

        var command = new ToggleUpvoteCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NewUpvote_Should_Return_Updated_Count()
    {
        // Arrange
        var makerId   = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var product   = CreatePublishedProduct(makerId);
        var productId = product.Id;

        _repositoryMock
            .Setup(r => r.GetByIdWithUpvotesAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.ToggleUpvoteAsync(productId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.GetUpvoteCountAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _eventPublisherMock
            .Setup(e => e.PublishUpvoteToggled(It.IsAny<ProductUpvotedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ToggleUpvoteCommand(productId, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NewUpvote_Should_Publish_Upvote_Event_With_IsUpvote_True()
    {
        // Arrange
        var makerId   = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var product   = CreatePublishedProduct(makerId);
        var productId = product.Id;
        // product.Upvotes boş → isNewUpvote = true

        _repositoryMock
            .Setup(r => r.GetByIdWithUpvotesAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _repositoryMock
            .Setup(r => r.ToggleUpvoteAsync(productId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.GetUpvoteCountAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        ProductUpvotedEvent? capturedEvent = null;
        _eventPublisherMock
            .Setup(e => e.PublishUpvoteToggled(It.IsAny<ProductUpvotedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ProductUpvotedEvent, CancellationToken>((ev, _) => capturedEvent = ev)
            .Returns(Task.CompletedTask);

        var command = new ToggleUpvoteCommand(productId, userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.ProductId.Should().Be(productId);
        capturedEvent.UserId.Should().Be(userId);
        capturedEvent.IsUpvote.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RemoveUpvote_Should_Publish_Upvote_Event_With_IsUpvote_False()
    {
        // Arrange — ürüne zaten oy verilmiş
        var makerId = Guid.NewGuid();
        var userId  = Guid.NewGuid();
        var product = CreatePublishedProduct(makerId);
        product.ToggleUpvote(userId); // oy ekle → Upvotes koleksiyonu dolu

        var productId = product.Id;

        _repositoryMock
            .Setup(r => r.GetByIdWithUpvotesAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _repositoryMock
            .Setup(r => r.ToggleUpvoteAsync(productId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.GetUpvoteCountAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ProductUpvotedEvent? capturedEvent = null;
        _eventPublisherMock
            .Setup(e => e.PublishUpvoteToggled(It.IsAny<ProductUpvotedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ProductUpvotedEvent, CancellationToken>((ev, _) => capturedEvent = ev)
            .Returns(Task.CompletedTask);

        var command = new ToggleUpvoteCommand(productId, userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — oy geri alındığı için IsUpvote false olmalı
        capturedEvent.Should().NotBeNull();
        capturedEvent!.IsUpvote.Should().BeFalse();
        capturedEvent.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task Handle_Should_Always_Call_ToggleUpvoteAsync()
    {
        // Arrange
        var product   = CreatePublishedProduct(Guid.NewGuid());
        var productId = product.Id;
        var userId    = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdWithUpvotesAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _repositoryMock
            .Setup(r => r.ToggleUpvoteAsync(productId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.GetUpvoteCountAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _eventPublisherMock
            .Setup(e => e.PublishUpvoteToggled(It.IsAny<ProductUpvotedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ToggleUpvoteCommand(productId, userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.ToggleUpvoteAsync(productId, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
