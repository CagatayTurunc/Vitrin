using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Domain.Entities;
using Xunit;

namespace Vitrin.Comment.Tests.Application;

public class AddCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _repositoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AddCommentCommandHandler _handler;

    public AddCommentCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICommentRepository>();
        _configMock = new Mock<IConfiguration>();

        // Configuration mock — notification ve product URL'leri
        _configMock.Setup(c => c["ServiceUrls:Notification"]).Returns("http://localhost:5101");
        _configMock.Setup(c => c["ServiceUrls:Product"]).Returns("http://localhost:5102");

        _handler = new AddCommentCommandHandler(_repositoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_Should_Return_Success_With_CommentId()
    {
        // Arrange
        var command = new AddCommentCommand(
            ProductId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "testuser",
            Content: "Gerçekten çok iyi bir ürün!");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyContent_Should_Return_Failure()
    {
        // Arrange
        var command = new AddCommentCommand(
            ProductId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "testuser",
            Content: "   "); // boş içerik

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithReply_Should_Save_Comment_With_ParentId()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentComment = CommentItem.Create(Guid.NewGuid(), Guid.NewGuid(), "parentuser", "Orijinal yorum").Value;

        var command = new AddCommentCommand(
            ProductId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "replyuser",
            Content: "Cevap veriyorum",
            ParentCommentId: parentId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentComment);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
