using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Analytics.Application.Commands;
using Vitrin.Shared.Contracts.Events;
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

    private static readonly string Topic = "analytics-events";
    private static readonly string GroupId = "analytics-consumer-group";

    public AnalyticsKafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalyticsKafkaConsumer> logger)
        : base(configuration, logger, Topic, GroupId)
    {
        _scopeFactory = scopeFactory;
        _consumerLogger = logger;
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

        var command = eventType switch
        {
            "analytics.product_viewed"    => BuildFromProductViewed(value),
            "analytics.product_upvoted"   => BuildFromProductUpvoted(value),
            "analytics.search_performed"  => BuildFromSearchPerformed(value),
            "analytics.comment_created"   => BuildFromCommentCreated(value),
            // Shared social/user event'leri de izlenebilir
            "product.published"           => BuildFromProductPublished(value),
            "user.registered"             => BuildFromUserRegistered(value),
            _ => null
        };

        if (command is null)
        {
            _consumerLogger.LogWarning(
                "[Analytics] Unknown or unsupported event type '{EventType}', skipping.",
                eventType);
            return;
        }

        // Scoped servisler için (DbContext) yeni scope aç
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _consumerLogger.LogWarning(
                "[Analytics] TrackEvent failed for EventType={EventType}: {Error}",
                eventType, result.Error);
        }
        else
        {
            _consumerLogger.LogInformation(
                "[Analytics] Event tracked. EventId={EventId}, Type={EventType}",
                result.Value, eventType);
        }
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
}
