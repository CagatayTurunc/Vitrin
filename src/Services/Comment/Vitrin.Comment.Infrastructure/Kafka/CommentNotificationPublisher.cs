using Microsoft.Extensions.Logging;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Comment.Infrastructure.Kafka;

/// <summary>
/// Adds notification events to Comment's unit of work. The command handler commits
/// the comment and every generated event in one SaveChanges operation.
/// </summary>
public sealed class CommentNotificationPublisher(
    CommentDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<CommentNotificationPublisher> logger) : ICommentNotificationPublisher
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
            "[Comment] Notification queued in Outbox. EventId={EventId}, RecipientUserId={UserId}, Type={Type}",
            @event.EventId,
            recipientUserId,
            notificationType);

        return Task.CompletedTask;
    }

    public Task RecordEngagementAsync(
        Guid productId,
        Guid commentId,
        Guid userId,
        bool isReply,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var @event = new CommentCreatedAnalyticsEvent
        {
            ProductId = productId,
            CommentId = commentId,
            UserId = userId,
            IsReply = isReply
        };

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));
        logger.LogInformation(
            "[Comment] Engagement event queued. EventId={EventId}, ProductId={ProductId}, IsReply={IsReply}",
            @event.EventId,
            productId,
            isReply);

        return Task.CompletedTask;
    }
}
