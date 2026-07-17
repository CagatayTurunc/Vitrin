using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Notification.Application.Commands;
using Vitrin.Notification.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Inbox;
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
    private readonly TimeProvider _timeProvider;

    private const string Topic   = EventTopics.Notification;
    private const string GroupId = "notification-consumer-group";

    public NotificationKafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<NotificationKafkaConsumer> logger)
        : base(configuration, logger, Topic, GroupId)
    {
        _scopeFactory    = scopeFactory;
        _consumerLogger  = logger;
        _timeProvider    = timeProvider;
    }

    protected override async Task ProcessMessageAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        var eventType = ExtractEventType(value);

        if (eventType != "notification.send")
        {
            throw new InvalidDataException(
                $"Unsupported event type '{eventType}' on {EventTopics.Notification}.");
        }

        var @event = DeserializeMessage<SendNotificationEvent>(value);
        if (@event is null)
        {
            throw new InvalidDataException("SendNotificationEvent could not be deserialized.");
        }

        if (@event.RecipientUserId == Guid.Empty || string.IsNullOrWhiteSpace(@event.Message))
        {
            throw new InvalidDataException("SendNotificationEvent has an empty recipient or message.");
        }

        var integrationEventId = ExtractEventId(key, value);
        if (integrationEventId == Guid.Empty)
        {
            throw new InvalidDataException(
                "SendNotificationEvent does not contain a valid integration event id.");
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.InboxMessages.AnyAsync(
                message => message.Id == integrationEventId,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            _consumerLogger.LogInformation(
                "[Notification] Duplicate event skipped. IntegrationEventId={IntegrationEventId}",
                integrationEventId);
            return;
        }

        var result = await mediator.Send(
            new SendNotificationCommand(@event.RecipientUserId, @event.Message, @event.NotificationType),
            cancellationToken);

        if (result.IsSuccess)
        {
            dbContext.InboxMessages.Add(InboxMessage.CreateProcessed(
                integrationEventId,
                eventType,
                _timeProvider.GetUtcNow().UtcDateTime));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _consumerLogger.LogInformation(
                "[Notification] Notification saved. IntegrationEventId={IntegrationEventId}, RecipientUserId={UserId}, Type={Type}, NotificationId={Id}",
                integrationEventId, @event.RecipientUserId, @event.NotificationType ?? "generic", result.Value);
        }
        else
        {
            throw new InvalidOperationException(
                $"Notification could not be persisted: {result.Error}");
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

    private static Guid ExtractEventId(string key, string json)
    {
        if (Guid.TryParse(key, out var keyEventId))
        {
            return keyEventId;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("EventId", out var eventId)
                || doc.RootElement.TryGetProperty("eventId", out eventId))
            {
                return Guid.TryParse(eventId.GetString(), out var parsedEventId)
                    ? parsedEventId
                    : Guid.Empty;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return Guid.Empty;
        }

        return Guid.Empty;
    }
}
