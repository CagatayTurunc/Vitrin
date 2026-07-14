using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Inbox;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Product.Infrastructure.Kafka;

/// <summary>
/// Builds the Product voting read model from Voting service events. The read-model
/// mutation and Inbox marker are committed together, making redelivery idempotent.
/// </summary>
public sealed class VotingEventsConsumer : KafkaConsumerBase
{
    private const string GroupId = "product-voting-consumer-group";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VotingEventsConsumer> _logger;

    public VotingEventsConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<VotingEventsConsumer> logger)
        : base(configuration, logger, EventTopics.Voting, GroupId)
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
            throw new InvalidDataException("Voting event metadata is missing or malformed.");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.InboxMessages.AnyAsync(message => message.Id == metadata.EventId, cancellationToken))
        {
            _logger.LogDebug("Duplicate voting event ignored. EventId={EventId}", metadata.EventId);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var productId = metadata.EventType switch
        {
            "voting.vote_added" => await HandleVoteAdded(value, db, cancellationToken),
            "voting.vote_removed" => await HandleVoteRemoved(value, db, cancellationToken),
            _ => throw new InvalidDataException(
                $"Unsupported event type '{metadata.EventType}' on {EventTopics.Voting}.")
        };

        db.InboxMessages.Add(InboxMessage.CreateProcessed(
            metadata.EventId,
            metadata.EventType,
            _timeProvider.GetUtcNow().UtcDateTime));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var count = await db.ProductUpvotes.CountAsync(
            upvote => upvote.ProductItemId == productId,
            cancellationToken);
        _logger.LogInformation(
            "Voting read model updated. EventId={EventId}, EventType={EventType}, ProductId={ProductId}, Count={Count}",
            metadata.EventId,
            metadata.EventType,
            productId,
            count);
    }

    private static async Task<Guid> HandleVoteAdded(
        string json,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var @event = DeserializeMessage<VoteAddedEvent>(json)
            ?? throw new InvalidDataException("VoteAddedEvent could not be deserialized.");

        if (!await db.Products.AnyAsync(product => product.Id == @event.ProductId, cancellationToken))
        {
            throw new InvalidOperationException($"Product '{@event.ProductId}' was not found.");
        }

        var alreadyExists = await db.ProductUpvotes.AnyAsync(
            upvote => upvote.ProductItemId == @event.ProductId && upvote.UserId == @event.UserId,
            cancellationToken);

        if (!alreadyExists)
        {
            db.ProductUpvotes.Add(new ProductUpvote(@event.ProductId, @event.UserId));
        }

        return @event.ProductId;
    }

    private static async Task<Guid> HandleVoteRemoved(
        string json,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var @event = DeserializeMessage<VoteRemovedEvent>(json)
            ?? throw new InvalidDataException("VoteRemovedEvent could not be deserialized.");

        var existing = await db.ProductUpvotes.FirstOrDefaultAsync(
            upvote => upvote.ProductItemId == @event.ProductId && upvote.UserId == @event.UserId,
            cancellationToken);

        if (existing is not null)
        {
            db.ProductUpvotes.Remove(existing);
        }

        return @event.ProductId;
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
