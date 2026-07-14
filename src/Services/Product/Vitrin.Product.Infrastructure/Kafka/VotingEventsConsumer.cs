using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Product.Infrastructure.Kafka;

/// <summary>
/// Voting servisinin "voting-events" topic'ini dinler.
/// VoteAddedEvent   → ProductUpvote kaydı ekler
/// VoteRemovedEvent → ProductUpvote kaydı siler
/// Bu sayede Product DB'deki upvote sayısı Voting servisindeki
/// gerçek oy kaydıyla her zaman senkronize kalır.
/// </summary>
public class VotingEventsConsumer : KafkaConsumerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VotingEventsConsumer> _consumerLogger;

    private const string Topic   = "voting-events";
    private const string GroupId = "product-voting-consumer-group";

    public VotingEventsConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<VotingEventsConsumer> logger)
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

        _consumerLogger.LogInformation(
            "[Product/VotingConsumer] Processing event: EventType={EventType}", eventType);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        switch (eventType)
        {
            case "voting.vote_added":
                await HandleVoteAdded(value, db, cancellationToken);
                break;

            case "voting.vote_removed":
                await HandleVoteRemoved(value, db, cancellationToken);
                break;

            default:
                _consumerLogger.LogDebug(
                    "[Product/VotingConsumer] Unknown event type '{EventType}', skipping.", eventType);
                break;
        }
    }

    // ─── Handlers ──────────────────────────────────────────────────────────

    private async Task HandleVoteAdded(
        string json,
        ProductDbContext db,
        CancellationToken ct)
    {
        var e = DeserializeMessage<VoteAddedEvent>(json);
        if (e is null)
        {
            _consumerLogger.LogWarning("[Product/VotingConsumer] Failed to deserialize VoteAddedEvent.");
            return;
        }

        // Ürün var mı kontrol et
        var productExists = await db.Products.AnyAsync(p => p.Id == e.ProductId, ct);
        if (!productExists)
        {
            _consumerLogger.LogWarning(
                "[Product/VotingConsumer] ProductId={ProductId} not found, skipping VoteAdded.",
                e.ProductId);
            return;
        }

        // Idempotency: zaten kayıt varsa tekrar ekleme
        var alreadyExists = await db.ProductUpvotes.AnyAsync(
            u => u.ProductItemId == e.ProductId && u.UserId == e.UserId, ct);

        if (alreadyExists)
        {
            _consumerLogger.LogDebug(
                "[Product/VotingConsumer] Upvote already exists for UserId={UserId}, ProductId={ProductId}. Skipping.",
                e.UserId, e.ProductId);
            return;
        }

        db.ProductUpvotes.Add(new ProductUpvote(e.ProductId, e.UserId));
        await db.SaveChangesAsync(ct);

        var count = await db.ProductUpvotes.CountAsync(u => u.ProductItemId == e.ProductId, ct);
        _consumerLogger.LogInformation(
            "[Product/VotingConsumer] Upvote added. ProductId={ProductId}, UserId={UserId}, TotalUpvotes={Count}",
            e.ProductId, e.UserId, count);
    }

    private async Task HandleVoteRemoved(
        string json,
        ProductDbContext db,
        CancellationToken ct)
    {
        var e = DeserializeMessage<VoteRemovedEvent>(json);
        if (e is null)
        {
            _consumerLogger.LogWarning("[Product/VotingConsumer] Failed to deserialize VoteRemovedEvent.");
            return;
        }

        var existing = await db.ProductUpvotes.FirstOrDefaultAsync(
            u => u.ProductItemId == e.ProductId && u.UserId == e.UserId, ct);

        if (existing is null)
        {
            _consumerLogger.LogDebug(
                "[Product/VotingConsumer] No upvote found to remove for UserId={UserId}, ProductId={ProductId}.",
                e.UserId, e.ProductId);
            return;
        }

        db.ProductUpvotes.Remove(existing);
        await db.SaveChangesAsync(ct);

        var count = await db.ProductUpvotes.CountAsync(u => u.ProductItemId == e.ProductId, ct);
        _consumerLogger.LogInformation(
            "[Product/VotingConsumer] Upvote removed. ProductId={ProductId}, UserId={UserId}, TotalUpvotes={Count}",
            e.ProductId, e.UserId, count);
    }

    // ─── Yardımcı ──────────────────────────────────────────────────────────

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
        catch { /* parse hatası → boş döner, bilinmeyen event olarak işlenir */ }
        return string.Empty;
    }
}
