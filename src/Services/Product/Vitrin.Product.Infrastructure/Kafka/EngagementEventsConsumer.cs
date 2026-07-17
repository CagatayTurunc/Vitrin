using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Inbox;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Product.Infrastructure.Kafka;

/// <summary>
/// Projects view and comment events into Product's read model. Inbox markers and
/// counters are committed together, so Kafka redelivery cannot inflate trends.
/// </summary>
public sealed class EngagementEventsConsumer : KafkaConsumerBase
{
    private const string GroupId = "product-engagement-consumer-group";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EngagementEventsConsumer> _logger;

    public EngagementEventsConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<EngagementEventsConsumer> logger)
        : base(configuration, logger, EventTopics.Analytics, GroupId)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ProcessMessageAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        var metadata = ExtractMetadata(key, value);
        if (metadata.EventId == Guid.Empty || string.IsNullOrWhiteSpace(metadata.EventType))
            throw new InvalidDataException("Analytics event metadata is missing or malformed.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.InboxMessages.AnyAsync(message => message.Id == metadata.EventId, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        Guid? productId = metadata.EventType switch
        {
            "analytics.product_viewed" => DeserializeMessage<ProductViewedEvent>(value)?.ProductId,
            "analytics.comment_created" => DeserializeMessage<CommentCreatedAnalyticsEvent>(value)?.ProductId,
            "analytics.search_performed" or "analytics.product_upvoted" => null,
            _ => throw new InvalidDataException(
                $"Unsupported event type '{metadata.EventType}' on {EventTopics.Analytics}.")
        };

        if (productId is { } id)
        {
            var product = await db.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new InvalidOperationException($"Product '{id}' was not found.");

            if (metadata.EventType == "analytics.product_viewed")
                product.RecordView();
            else
                product.RecordComment();
        }

        db.InboxMessages.Add(InboxMessage.CreateProcessed(
            metadata.EventId,
            metadata.EventType,
            _timeProvider.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Engagement read model updated. EventId={EventId}, EventType={EventType}, ProductId={ProductId}",
            metadata.EventId,
            metadata.EventType,
            productId);
    }

    private static EventMetadata ExtractMetadata(string key, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var eventId = Guid.TryParse(key, out var keyId)
                ? keyId
                : ReadGuid(root, "EventId", "eventId");
            return new EventMetadata(eventId, ReadString(root, "EventType", "eventType"));
        }
        catch (JsonException)
        {
            return new EventMetadata(Guid.Empty, string.Empty);
        }
    }

    private static Guid ReadGuid(JsonElement root, string pascalName, string camelName)
    {
        var value = root.TryGetProperty(pascalName, out var pascal)
            ? pascal.GetString()
            : root.TryGetProperty(camelName, out var camel) ? camel.GetString() : null;
        return Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
    }

    private static string ReadString(JsonElement root, string pascalName, string camelName) =>
        root.TryGetProperty(pascalName, out var pascal)
            ? pascal.GetString() ?? string.Empty
            : root.TryGetProperty(camelName, out var camel)
                ? camel.GetString() ?? string.Empty
                : string.Empty;

    private sealed record EventMetadata(Guid EventId, string EventType);
}
