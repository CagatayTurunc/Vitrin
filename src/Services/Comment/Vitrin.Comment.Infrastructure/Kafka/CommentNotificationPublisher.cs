using Microsoft.Extensions.Logging;
using Vitrin.Comment.Application.Commands;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Comment.Infrastructure.Kafka;

/// <summary>
/// ICommentNotificationPublisher'ın Kafka implementasyonu.
/// HTTP yerine "notification-events" topic'ine publish eder.
/// </summary>
public class CommentNotificationPublisher : ICommentNotificationPublisher
{
    private readonly IEventPublisher _kafkaProducer;
    private readonly ILogger<CommentNotificationPublisher> _logger;

    public CommentNotificationPublisher(IEventPublisher kafkaProducer, ILogger<CommentNotificationPublisher> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger        = logger;
    }

    public async Task NotifyAsync(
        Guid recipientUserId,
        string message,
        string notificationType,
        CancellationToken ct = default)
    {
        var @event = new SendNotificationEvent
        {
            RecipientUserId  = recipientUserId,
            Message          = message,
            NotificationType = notificationType
        };

        try
        {
            await _kafkaProducer.PublishAsync(@event);
            _logger.LogInformation(
                "[Comment] SendNotificationEvent published. RecipientUserId={UserId}, Type={Type}",
                recipientUserId, notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Comment] Failed to publish SendNotificationEvent. RecipientUserId={UserId}",
                recipientUserId);
        }
    }
}
