using Microsoft.Extensions.Logging;
using Vitrin.Product.Application.Commands;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Product.Infrastructure.Kafka;

/// <summary>
/// IProductEventPublisher'ın Kafka implementasyonu.
/// analytics-events ve shared topic'lerine publish eder.
/// </summary>
public class ProductEventPublisher : IProductEventPublisher
{
    private readonly IEventPublisher _kafkaProducer;
    private readonly ILogger<ProductEventPublisher> _logger;

    public ProductEventPublisher(IEventPublisher kafkaProducer, ILogger<ProductEventPublisher> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger        = logger;
    }

    public async Task PublishUpvoteToggled(ProductUpvotedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await _kafkaProducer.PublishAsync(@event);
            _logger.LogInformation(
                "[ProductEventPublisher] ProductUpvotedEvent published. ProductId={ProductId}, IsUpvote={IsUpvote}",
                @event.ProductId, @event.IsUpvote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ProductEventPublisher] Failed to publish ProductUpvotedEvent. ProductId={ProductId}",
                @event.ProductId);
        }
    }

    public async Task PublishProductPublished(ProductPublishedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await _kafkaProducer.PublishAsync(@event);
            _logger.LogInformation(
                "[ProductEventPublisher] ProductPublishedEvent published. ProductId={ProductId}",
                @event.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ProductEventPublisher] Failed to publish ProductPublishedEvent. ProductId={ProductId}",
                @event.ProductId);
        }
    }
}
