using FluentAssertions;
using Moq;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Domain.Entities;
using Xunit;

namespace Vitrin.Comment.Tests.Application;

public class UpdateCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _repositoryMock;
    private readonly UpdateCommentCommandHandler _handler;

    public UpdateCommentCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICommentRepository>();
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _handler = new UpdateCommentCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenOwnerUpdatesWithValidContent_Should_Return_Success()
    {
        var userId  = Guid.NewGuid();
        var comment = CommentItem.Create(Guid.NewGuid(), userId, "user", "Eski içerik").Value;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        _repositoryMock
            .Setup(r => r.UpdateAsync(comment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new UpdateCommentCommand(comment.Id, userId, "Yeni içerik"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        comment.Content.Should().Be("Yeni içerik");
        _repositoryMock.Verify(r => r.UpdateAsync(comment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommentNotFound_Should_Return_Failure()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommentItem?)null);

        var result = await _handler.Handle(new UpdateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "Yeni içerik"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDifferentUser_Should_Return_Unauthorized_Failure()
    {
        var ownerId = Guid.NewGuid();
        var comment = CommentItem.Create(Guid.NewGuid(), ownerId, "owner", "İçerik").Value;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        var result = await _handler.Handle(new UpdateCommentCommand(comment.Id, Guid.NewGuid(), "Yeni içerik"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainAny("Unauthorized", "unauthorized");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenContentIsEmpty_Should_Return_Failure()
    {
        var userId  = Guid.NewGuid();
        var comment = CommentItem.Create(Guid.NewGuid(), userId, "user", "Mevcut içerik").Value;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        var result = await _handler.Handle(new UpdateCommentCommand(comment.Id, userId, "   "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
