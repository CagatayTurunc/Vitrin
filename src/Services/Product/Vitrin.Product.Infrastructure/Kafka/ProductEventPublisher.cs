using Microsoft.Extensions.Logging;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Product.Infrastructure.Kafka;

public sealed class ProductEventPublisher(
    ProductDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ProductEventPublisher> logger)
{
    public async Task PublishProductPublished(
        ProductPublishedEvent @event,
        CancellationToken cancellationToken = default)
    {
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(@event, timeProvider.GetUtcNow().UtcDateTime));
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Product mutation and outbox event committed atomically. EventId={EventId}, ProductId={ProductId}",
            @event.EventId,
            @event.ProductId);
    }
}
