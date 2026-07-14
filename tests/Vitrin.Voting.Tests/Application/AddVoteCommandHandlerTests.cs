using FluentAssertions;
using Moq;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Domain.Entities;
using Xunit;

namespace Vitrin.Voting.Tests.Application;

public class AddVoteCommandHandlerTests
{
    private readonly Mock<IVoteRepository> _repositoryMock;
    private readonly Mock<IVoteEventPublisher> _eventPublisherMock;
    private readonly AddVoteCommandHandler _handler;

    public AddVoteCommandHandlerTests()
    {
        _repositoryMock     = new Mock<IVoteRepository>();
        _eventPublisherMock = new Mock<IVoteEventPublisher>();
        _handler = new AddVoteCommandHandler(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WithNewVote_Should_Return_Success()
    {
        // Arrange
        var command = new AddVoteCommand(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Vote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(e => e.PublishVoteAddedAsync(It.IsAny<VoteAddedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Vote>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishVoteAddedAsync(It.IsAny<VoteAddedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyVoted_Should_Return_Failure()
    {
        // Arrange
        var command = new AddVoteCommand(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(command.UserId, command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already voted");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Vote>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(
            e => e.PublishVoteAddedAsync(It.IsAny<VoteAddedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NewVote_Should_Create_Vote_With_Correct_Ids()
    {
        // Arrange
        var userId    = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command   = new AddVoteCommand(userId, productId);

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Vote? capturedVote = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Vote>(), It.IsAny<CancellationToken>()))
            .Callback<Vote, CancellationToken>((v, _) => capturedVote = v)
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(e => e.PublishVoteAddedAsync(It.IsAny<VoteAddedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedVote.Should().NotBeNull();
        capturedVote!.UserId.Should().Be(userId);
        capturedVote.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task Handle_NewVote_Should_Publish_VoteAddedEvent_With_Correct_Ids()
    {
        // Arrange
        var userId    = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command   = new AddVoteCommand(userId, productId);

        _repositoryMock
            .Setup(r => r.HasUserVotedAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Vote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        VoteAddedEvent? capturedEvent = null;
        _eventPublisherMock
            .Setup(e => e.PublishVoteAddedAsync(It.IsAny<VoteAddedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<VoteAddedEvent, CancellationToken>((ev, _) => capturedEvent = ev)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — event doğru data ile publish edilmeli
        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be(userId);
        capturedEvent.ProductId.Should().Be(productId);
    }
}
