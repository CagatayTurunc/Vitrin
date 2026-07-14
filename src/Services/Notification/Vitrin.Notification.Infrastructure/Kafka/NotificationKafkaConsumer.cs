using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Notification.Application.Commands;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Notification.Infrastructure.Kafka;

/// <summary>
/// "notification-events" topic'ini dinler.
/// Auth, Comment ve Product servislerinin publish ettiği
/// SendNotificationEvent'leri consume ederek SQLite'a kaydeder.
/// </summary>
public class NotificationKafkaConsumer : KafkaConsumerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationKafkaConsumer> _consumerLogger;

    private const string Topic   = "notification-events";
    private const string GroupId = "notification-consumer-group";

    public NotificationKafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationKafkaConsumer> logger)
        : base(configuration, logger, Topic, GroupId)
    {
        _scopeFactory    = scopeFactory;
        _consumerLogger  = logger;
    }

    protected override async Task ProcessMessageAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        var eventType = ExtractEventType(value);

        if (eventType != "notification.send")
        {
            _consumerLogger.LogDebug(
                "[Notification] Unknown event type '{EventType}', skipping.", eventType);
            return;
        }

        var @event = DeserializeMessage<SendNotificationEvent>(value);
        if (@event is null)
        {
            _consumerLogger.LogWarning("[Notification] Failed to deserialize SendNotificationEvent.");
            return;
        }

        if (@event.RecipientUserId == Guid.Empty || string.IsNullOrWhiteSpace(@event.Message))
        {
            _consumerLogger.LogWarning("[Notification] Invalid event data: empty RecipientUserId or Message.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new SendNotificationCommand(@event.RecipientUserId, @event.Message),
            cancellationToken);

        if (result.IsSuccess)
        {
            _consumerLogger.LogInformation(
                "[Notification] Notification saved. RecipientUserId={UserId}, Type={Type}, NotificationId={Id}",
                @event.RecipientUserId, @event.NotificationType ?? "generic", result.Value);
        }
        else
        {
            _consumerLogger.LogWarning(
                "[Notification] Failed to save notification. Error={Error}", result.Error);
        }
    }

    private static string ExtractEventType(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("EventType", out var et))
                return et.GetString() ?? string.Empty;
            if (doc.RootElement.TryGetProperty("eventType", out var et2))
                return et2.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }
}
