using Microsoft.Extensions.Logging;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Auth.Infrastructure.Kafka;

/// <summary>
/// Auth servisinin bildirim göndermek için kullandığı Kafka publisher.
/// "notification-events" topic'ine SendNotificationEvent publish eder.
/// </summary>
public interface IAuthNotificationPublisher
{
    Task NotifyAsync(Guid recipientUserId, string message, string notificationType, CancellationToken ct = default);
}

public class AuthNotificationPublisher : IAuthNotificationPublisher
{
    private readonly IEventPublisher _kafkaProducer;
    private readonly ILogger<AuthNotificationPublisher> _logger;

    public AuthNotificationPublisher(IEventPublisher kafkaProducer, ILogger<AuthNotificationPublisher> logger)
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
                "[Auth] SendNotificationEvent published. RecipientUserId={UserId}, Type={Type}",
                recipientUserId, notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Auth] Failed to publish SendNotificationEvent. RecipientUserId={UserId}",
                recipientUserId);
        }
    }
}
