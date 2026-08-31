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
/// Auth service'ten gelen SubscriptionUpgradedEvent ve SubscriptionCanceledEvent'lerini tüketerek
/// ilgili maker'ın tüm ürünlerindeki MakerTierSnapshot alanını günceller.
/// Bu sayede ürün kartlarında premium badge'ler otomatik gösterilir.
/// </summary>
public sealed class SubscriptionEventsConsumer : KafkaConsumerBase
{
    private const string GroupId = "product-subscription-consumer-group";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SubscriptionEventsConsumer> _logger;

    public SubscriptionEventsConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<SubscriptionEventsConsumer> logger)
        : base(configuration, logger, EventTopics.Subscription, GroupId)
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
        var metadata = ExtractMetadata(value);
        if (metadata.EventId == Guid.Empty || string.IsNullOrWhiteSpace(metadata.EventType))
        {
            _logger.LogWarning("Subscription event metadata eksik veya bozuk. Value={Value}", value);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Idempotency check — aynı event'i iki kez işleme
        if (await db.InboxMessages.AnyAsync(m => m.Id == metadata.EventId, cancellationToken))
        {
            _logger.LogDebug("Duplicate subscription event ignored. EventId={EventId}", metadata.EventId);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var updatedCount = metadata.EventType switch
        {
            "subscription.upgraded" => await HandleUpgraded(value, db, cancellationToken),
            "subscription.canceled" => await HandleCanceled(value, db, cancellationToken),
            "subscription.expired"  => await HandleExpired(value, db, cancellationToken),
            "subscription.renewed"  => 0, // Tier değişmez, sadece dönem uzar — no-op
            _ => 0
        };

        db.InboxMessages.Add(InboxMessage.CreateProcessed(
            metadata.EventId,
            metadata.EventType,
            _timeProvider.GetUtcNow().UtcDateTime));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription event işlendi. EventId={EventId}, EventType={EventType}, UpdatedProducts={Count}",
            metadata.EventId, metadata.EventType, updatedCount);
    }

    private static async Task<int> HandleUpgraded(
        string json,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var @event = DeserializeMessage<SubscriptionUpgradedEvent>(json)
            ?? throw new InvalidDataException("SubscriptionUpgradedEvent deserialize edilemedi.");

        // Maker'ın tüm ürünlerini bul ve tier'ı güncelle
        var products = await db.Products
            .Where(p => p.MakerId == @event.UserId)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.UpdateMakerTier(@event.NewTier);
        }

        return products.Count;
    }

    private static async Task<int> HandleCanceled(
        string json,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var @event = DeserializeMessage<SubscriptionCanceledEvent>(json)
            ?? throw new InvalidDataException("SubscriptionCanceledEvent deserialize edilemedi.");

        // Abonelik iptalinde Free tier'a düşür
        var products = await db.Products
            .Where(p => p.MakerId == @event.UserId)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.UpdateMakerTier("Free");
        }

        return products.Count;
    }

    private static async Task<int> HandleExpired(
        string json,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var @event = DeserializeMessage<SubscriptionExpiredEvent>(json)
            ?? throw new InvalidDataException("SubscriptionExpiredEvent deserialize edilemedi.");

        // Expire olunca da Free tier'a düşür
        var products = await db.Products
            .Where(p => p.MakerId == @event.UserId)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.UpdateMakerTier("Free");
        }

        return products.Count;
    }

    private static EventMetadata ExtractMetadata(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return new EventMetadata(
                ReadGuid(root, "EventId", "eventId"),
                ReadString(root, "EventType", "eventType"));
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
