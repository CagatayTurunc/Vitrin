using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Analytics.Application.Commands;
using Vitrin.Analytics.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Inbox;
using Vitrin.Shared.Infrastructure.Kafka;
using System.Text.Json;

namespace Vitrin.Analytics.Infrastructure.Kafka;

/// <summary>
/// "analytics-events" topic'ini dinleyip her event'i
/// TrackEventCommand olarak MediatR pipeline'ına iletir.
/// BackgroundService olarak uygulama başladığında devreye girer.
/// </summary>
public class AnalyticsKafkaConsumer : KafkaConsumerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalyticsKafkaConsumer> _consumerLogger;
    private readonly TimeProvider _timeProvider;

    private static readonly string[] Topics =
    [
        EventTopics.Analytics,
        EventTopics.Voting,
        EventTopics.Social,
        EventTopics.User
    ];
    private static readonly string GroupId = "analytics-consumer-group";

    public AnalyticsKafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<AnalyticsKafkaConsumer> logger)
        : base(configuration, logger, Topics, GroupId)
    {
        _scopeFactory = scopeFactory;
        _consumerLogger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ProcessMessageAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        // Event tipini header veya JSON "EventType" alanından oku
        var eventType = ExtractEventType(value);

        _consumerLogger.LogInformation(
            "[Analytics] Processing event: EventType={EventType}, Key={Key}",
            eventType, key);

        if (eventType is "comment.added" or "comment.replied" or "user.role_changed")
        {
            _consumerLogger.LogDebug(
                "[Analytics] Known event is outside the analytics projection and was ignored. EventType={EventType}",
                eventType);
            return;
        }

        var command = eventType switch
        {
            "analytics.product_viewed"    => BuildFromProductViewed(value),
            "analytics.product_upvoted"   => BuildFromProductUpvoted(value),
            "analytics.search_performed"  => BuildFromSearchPerformed(value),
            "analytics.comment_created"   => BuildFromCommentCreated(value),
            "voting.vote_added"           => BuildFromVoteAdded(value),
            "voting.vote_removed"         => BuildFromVoteRemoved(value),
            "product.published"           => BuildFromProductPublished(value),
            "user.registered"             => BuildFromUserRegistered(value),
            _ => null
        };

        if (command is null)
        {
            throw new InvalidDataException(
                $"Unknown or malformed event type '{eventType}' in the analytics consumer.");
        }

        // Scoped servisler için (DbContext) yeni scope aç
        var integrationEventId = ExtractEventId(key, value);
        if (integrationEventId == Guid.Empty)
        {
            throw new InvalidDataException(
                $"Event '{eventType}' does not contain a valid integration event id.");
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.InboxMessages.AnyAsync(
                message => message.Id == integrationEventId,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            _consumerLogger.LogInformation(
                "[Analytics] Duplicate event skipped. IntegrationEventId={IntegrationEventId}",
                integrationEventId);
            return;
        }

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Analytics event '{eventType}' could not be persisted: {result.Error}");
        }

        dbContext.InboxMessages.Add(InboxMessage.CreateProcessed(
            integrationEventId,
            eventType,
            _timeProvider.GetUtcNow().UtcDateTime));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _consumerLogger.LogInformation(
            "[Analytics] Event tracked. IntegrationEventId={IntegrationEventId}, AnalyticsEventId={AnalyticsEventId}, Type={EventType}",
            integrationEventId, result.Value, eventType);
    }

    // ─── Event Builder Metotları ───────────────────────────────────────────

    private static TrackEventCommand? BuildFromProductViewed(string json)
    {
        var e = DeserializeMessage<ProductViewedEvent>(json);
        if (e is null) return null;

        var data = JsonSerializer.Serialize(new
        {
            e.ProductSlug,
            e.IpAddress,
            e.UserAgent,
            e.Referrer,
            e.Timestamp
        });

        return new TrackEventCommand(
            EventType: "ProductView",
            EventData: data,
            ProductId: e.ProductId,
            UserId: e.UserId);
    }

    private static TrackEventCommand? BuildFromProductUpvoted(string json)
    {
        var e = DeserializeMessage<ProductUpvotedEvent>(json);
        if (e is null) return null;

        var eventTypeName = e.IsUpvote ? "ProductUpvote" : "ProductDownvote";
        var data = JsonSerializer.Serialize(new
        {
            e.ProductSlug,
            e.IsUpvote,
            e.Timestamp
        });

        return new TrackEventCommand(
            EventType: eventTypeName,
            EventData: data,
            ProductId: e.ProductId,
            UserId: e.UserId);
    }

    private static TrackEventCommand? BuildFromSearchPerformed(string json)
    {
        var e = DeserializeMessage<SearchPerformedEvent>(json);
        if (e is null) return null;

        var data = JsonSerializer.Serialize(new
        {
            e.Query,
            e.ResultCount,
            e.Timestamp
        });

        return new TrackEventCommand(
            EventType: "Search",
            EventData: data,
            ProductId: null,
            UserId: e.UserId);
    }

    private static TrackEventCommand? BuildFromCommentCreated(string json)
    {
        var e = DeserializeMessage<CommentCreatedAnalyticsEvent>(json);
        if (e is null) return null;

        var data = JsonSerializer.Serialize(new
        {
            e.CommentId,
            e.IsReply,
            e.Timestamp
        });

        return new TrackEventCommand(
            EventType: "Comment",
            EventData: data,
            ProductId: e.ProductId,
            UserId: e.UserId);
    }

    private static TrackEventCommand? BuildFromVoteAdded(string json)
    {
        var e = DeserializeMessage<VoteAddedEvent>(json);
        if (e is null) return null;

        return new TrackEventCommand(
            EventType: "ProductUpvote",
            EventData: JsonSerializer.Serialize(new { e.VoteId, e.Timestamp }),
            ProductId: e.ProductId,
            UserId: e.UserId);
    }

    private static TrackEventCommand? BuildFromVoteRemoved(string json)
    {
        var e = DeserializeMessage<VoteRemovedEvent>(json);
        if (e is null) return null;

        return new TrackEventCommand(
            EventType: "ProductDownvote",
            EventData: JsonSerializer.Serialize(new { e.Timestamp }),
            ProductId: e.ProductId,
            UserId: e.UserId);
    }

    private static TrackEventCommand? BuildFromProductPublished(string json)
    {
        var e = DeserializeMessage<ProductPublishedEvent>(json);
        if (e is null) return null;

        var data = JsonSerializer.Serialize(new
        {
            e.ProductName,
            e.ProductSlug,
            e.MakerId,
            e.Timestamp
        });

        return new TrackEventCommand(
            EventType: "ProductPublished",
            EventData: data,
            ProductId: e.ProductId,
            UserId: e.MakerId);
    }

    private static TrackEventCommand? BuildFromUserRegistered(string json)
    {
        var e = DeserializeMessage<UserRegisteredEvent>(json);
        if (e is null) return null;

        var data = JsonSerializer.Serialize(new
        {
            e.RegistrationMethod,
            e.IpAddress,
            e.UserAgent,
            e.Timestamp
        });

        return new TrackEventCommand(
            EventType: "UserRegistered",
            EventData: data,
            ProductId: null,
            UserId: e.UserId);
    }

    // ─── Yardımcı Metot ───────────────────────────────────────────────────

    private static string ExtractEventType(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("EventType", out var et))
                return et.GetString() ?? string.Empty;
            // camelCase fallback
            if (doc.RootElement.TryGetProperty("eventType", out var et2))
                return et2.GetString() ?? string.Empty;
        }
        catch
        {
            // JSON parse hatası → bilinmeyen event
        }
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
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("EventId", out var eventId)
                || doc.RootElement.TryGetProperty("eventId", out eventId))
            {
                return Guid.TryParse(eventId.GetString(), out var parsedEventId)
                    ? parsedEventId
                    : Guid.Empty;
            }
        }
        catch (JsonException)
        {
            return Guid.Empty;
        }

        return Guid.Empty;
    }
}
