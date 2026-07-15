using Microsoft.Extensions.Logging;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Auth.Infrastructure.Kafka;

public interface IAuthNotificationPublisher
{
    Task NotifyAsync(
        Guid recipientUserId,
        string message,
        string notificationType,
        CancellationToken ct = default);
}

/// <summary>
/// Adds notification events to Auth's unit of work. The calling endpoint commits
/// its domain mutation and outbox messages in the same SaveChanges operation.
/// </summary>
public sealed class AuthNotificationPublisher(
    AuthDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<AuthNotificationPublisher> logger) : IAuthNotificationPublisher
{
    public Task NotifyAsync(
        Guid recipientUserId,
        string message,
        string notificationType,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var @event = new SendNotificationEvent
        {
            RecipientUserId = recipientUserId,
            Message = message,
            NotificationType = notificationType
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));
        logger.LogInformation(
            "[Auth] Notification queued in Outbox. EventId={EventId}, RecipientUserId={UserId}, Type={Type}",
            @event.EventId,
            recipientUserId,
            notificationType);

        return Task.CompletedTask;
    }
}
