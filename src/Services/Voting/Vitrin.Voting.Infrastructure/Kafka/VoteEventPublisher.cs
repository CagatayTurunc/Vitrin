using Microsoft.Extensions.Logging;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Outbox;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Infrastructure.Data;

namespace Vitrin.Voting.Infrastructure.Kafka;

/// <summary>
/// Adds the integration event to the same DbContext that tracks the vote mutation.
/// SaveChanges therefore commits the vote and its outbox record atomically.
/// </summary>
public sealed class VoteEventPublisher(
    VoteDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<VoteEventPublisher> logger) : IVoteEventPublisher
{
    public Task PublishVoteAddedAsync(
        VoteAddedEvent @event,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(@event, cancellationToken);

    public Task PublishVoteRemovedAsync(
        VoteRemovedEvent @event,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(@event, cancellationToken);

    private async Task EnqueueAsync(
        IEvent @event,
        CancellationToken cancellationToken)
    {
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Vote mutation and outbox event committed atomically. EventId={EventId}, EventType={EventType}",
            @event.EventId,
            @event.EventType);
    }
}
