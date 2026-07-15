using FluentAssertions;
using Moq;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Domain.Entities;
using Xunit;

namespace Vitrin.Comment.Tests.Application;

public class AddCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _repositoryMock;
    private readonly Mock<ICommentNotificationPublisher> _notificationPublisherMock;
    private readonly AddCommentCommandHandler _handler;

    public AddCommentCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICommentRepository>();
        _notificationPublisherMock = new Mock<ICommentNotificationPublisher>();
        _notificationPublisherMock
            .Setup(p => p.NotifyAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AddCommentCommandHandler(
            _repositoryMock.Object,
            _notificationPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_Should_Return_Success_With_CommentId()
    {
        var command = new AddCommentCommand(
            ProductId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "testuser",
            Content: "Gerçekten çok iyi bir ürün!");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyContent_Should_Return_Failure()
    {
        var command = new AddCommentCommand(
            ProductId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "testuser",
            Content: "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WithReply_Should_Save_Comment_And_Notify_ParentOwner()
    {
        var parentId = Guid.NewGuid();
        var parentComment = CommentItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "parentuser",
            "Orijinal yorum").Value;

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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _notificationPublisherMock.Verify(
            p => p.NotifyAsync(
                parentComment.UserId,
                It.IsAny<string>(),
                "comment_reply",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
