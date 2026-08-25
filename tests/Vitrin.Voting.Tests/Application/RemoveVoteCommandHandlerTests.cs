using FluentAssertions;
using Moq;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Voting.Application.Commands;
using Xunit;

namespace Vitrin.Voting.Tests.Application;

public class RemoveVoteCommandHandlerTests
{
    private readonly Mock<IVoteRepository> _repositoryMock;
    private readonly Mock<IVoteEventPublisher> _eventPublisherMock;
    private readonly RemoveVoteCommandHandler _handler;

    public RemoveVoteCommandHandlerTests()
    {
        _repositoryMock     = new Mock<IVoteRepository>();
        _eventPublisherMock = new Mock<IVoteEventPublisher>();
        _handler = new RemoveVoteCommandHandler(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenVoteExists_Should_Return_Success()
    {
        var command = new RemoveVoteCommand(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.RemoveAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventPublisherMock
            .Setup(e => e.PublishVoteRemovedAsync(It.IsAny<VoteRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.RemoveAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVoteDoesNotExist_Should_Return_Failure()
    {
        var command = new RemoveVoteCommand(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No vote found");
        _repositoryMock.Verify(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(e => e.PublishVoteRemovedAsync(It.IsAny<VoteRemovedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVoteExists_Should_Publish_VoteRemovedEvent_With_Correct_Ids()
    {
        var userId    = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command   = new RemoveVoteCommand(userId, productId);

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.RemoveAsync(userId, productId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        VoteRemovedEvent? capturedEvent = null;
        _eventPublisherMock
            .Setup(e => e.PublishVoteRemovedAsync(It.IsAny<VoteRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<VoteRemovedEvent, CancellationToken>((ev, _) => capturedEvent = ev)
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be(userId);
        capturedEvent.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task Handle_WhenVoteExists_Should_Call_Remove_Before_PublishEvent()
    {
        var command = new RemoveVoteCommand(Guid.NewGuid(), Guid.NewGuid());
        var callOrder = new List<string>();

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.RemoveAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("remove"))
            .Returns(Task.CompletedTask);
        _eventPublisherMock
            .Setup(e => e.PublishVoteRemovedAsync(It.IsAny<VoteRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<VoteRemovedEvent, CancellationToken>((_, _) => callOrder.Add("publish"))
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        callOrder.Should().ContainInOrder("remove", "publish");
    }
}
