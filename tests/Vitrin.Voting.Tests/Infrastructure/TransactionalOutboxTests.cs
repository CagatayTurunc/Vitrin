using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Infrastructure.Data;
using Vitrin.Voting.Infrastructure.Kafka;
using Vitrin.Voting.Infrastructure.Repositories;
using Xunit;

namespace Vitrin.Voting.Tests.Infrastructure;

public sealed class TransactionalOutboxTests
{
    [Fact]
    public async Task AddVote_Should_Commit_Vote_And_Event_In_One_SaveChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        var handler = CreateHandler(dbContext, new FixedTimeProvider(now));
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var result = await handler.Handle(
            new AddVoteCommand(userId, productId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dbContext.ChangeTracker.Clear();

        var vote = await dbContext.Votes.SingleAsync();
        var outbox = await dbContext.OutboxMessages.SingleAsync();
        var payload = JsonSerializer.Deserialize<VoteAddedEvent>(outbox.Payload);

        vote.UserId.Should().Be(userId);
        vote.ProductId.Should().Be(productId);
        outbox.Topic.Should().Be(EventTopics.Voting);
        outbox.EventType.Should().Be("voting.vote_added");
        outbox.CreatedAtUtc.Should().Be(now.UtcDateTime);
        payload.Should().NotBeNull();
        payload!.VoteId.Should().Be(vote.Id);
        payload.UserId.Should().Be(userId);
        payload.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task RemoveVote_Should_Commit_Deletion_And_Removal_Event_Together()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var handler = CreateHandler(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero)));
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await handler.Handle(new AddVoteCommand(userId, productId), CancellationToken.None);

        var removeHandler = CreateRemoveHandler(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 14, 12, 5, 0, TimeSpan.Zero)));
        var result = await removeHandler.Handle(
            new RemoveVoteCommand(userId, productId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dbContext.ChangeTracker.Clear();
        (await dbContext.Votes.CountAsync()).Should().Be(0);
        var events = await dbContext.OutboxMessages
            .OrderBy(message => message.CreatedAtUtc)
            .ToListAsync();
        events.Should().HaveCount(2);
        events[1].EventType.Should().Be("voting.vote_removed");
    }

    private static AddVoteCommandHandler CreateHandler(
        VoteDbContext dbContext,
        TimeProvider timeProvider)
    {
        var repository = new VoteRepository(dbContext);
        var publisher = new VoteEventPublisher(
            dbContext,
            timeProvider,
            NullLogger<VoteEventPublisher>.Instance);
        return new AddVoteCommandHandler(repository, publisher);
    }

    private static RemoveVoteCommandHandler CreateRemoveHandler(
        VoteDbContext dbContext,
        TimeProvider timeProvider)
    {
        var repository = new VoteRepository(dbContext);
        var publisher = new VoteEventPublisher(
            dbContext,
            timeProvider,
            NullLogger<VoteEventPublisher>.Instance);
        return new RemoveVoteCommandHandler(repository, publisher);
    }

    private static VoteDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<VoteDbContext>()
            .UseSqlite(connection)
            .Options;
        return new VoteDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
