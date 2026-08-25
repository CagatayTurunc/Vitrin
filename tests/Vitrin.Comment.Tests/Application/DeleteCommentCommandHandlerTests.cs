using FluentAssertions;
using Moq;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Domain.Entities;
using Xunit;

namespace Vitrin.Comment.Tests.Application;

public class DeleteCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _repositoryMock;
    private readonly DeleteCommentCommandHandler _handler;

    public DeleteCommentCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICommentRepository>();
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _handler = new DeleteCommentCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommentExistsAndOwner_Should_Return_Success()
    {
        var userId  = Guid.NewGuid();
        var comment = CommentItem.Create(Guid.NewGuid(), userId, "user", "Silinecek yorum").Value;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        _repositoryMock
            .Setup(r => r.UpdateAsync(comment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new DeleteCommentCommand(comment.Id, userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        comment.IsDeleted.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(comment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommentNotFound_Should_Return_Failure()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommentItem?)null);

        var result = await _handler.Handle(new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDifferentUser_Should_Return_Unauthorized_Failure()
    {
        var ownerId = Guid.NewGuid();
        var comment = CommentItem.Create(Guid.NewGuid(), ownerId, "owner", "Başkasının yorumu").Value;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        var result = await _handler.Handle(new DeleteCommentCommand(comment.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainAny("Unauthorized", "unauthorized");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<CommentItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
