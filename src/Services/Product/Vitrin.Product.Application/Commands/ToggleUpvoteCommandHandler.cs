using MediatR;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

/// <summary>
/// Upvote/downvote event'lerini yayınlamak için abstraction.
/// Infrastructure katmanında KafkaProducer ile implement edilir.
/// </summary>
public interface IProductEventPublisher
{
    Task PublishUpvoteToggled(ProductUpvotedEvent @event, CancellationToken cancellationToken = default);
    Task PublishProductPublished(ProductPublishedEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ürüne oy ekler/çıkarır ve "analytics-events" topic'ine event publish eder.
/// ProductUpvote tablosunun sync'i artık Voting servisinden gelen
/// "voting-events" consumer tarafından yapılır — bu handler sadece
/// kendi DB'sindeki toggle işlemini yapar.
/// </summary>
public class ToggleUpvoteCommandHandler : IRequestHandler<ToggleUpvoteCommand, Result<int>>
{
    private readonly IProductRepository _repository;
    private readonly IProductEventPublisher _eventPublisher;

    public ToggleUpvoteCommandHandler(
        IProductRepository repository,
        IProductEventPublisher eventPublisher)
    {
        _repository     = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<int>> Handle(ToggleUpvoteCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdWithUpvotesAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<int>.Failure("Product not found.");

        bool isNewUpvote = !product.Upvotes.Any(u => u.UserId == request.UserId);

        await _repository.ToggleUpvoteAsync(request.ProductId, request.UserId, cancellationToken);
        var count = await _repository.GetUpvoteCountAsync(request.ProductId, cancellationToken);

        // Kafka'ya analytics event publish et
        await _eventPublisher.PublishUpvoteToggled(new ProductUpvotedEvent
        {
            ProductId   = product.Id,
            ProductSlug = product.Slug,
            UserId      = request.UserId,
            IsUpvote    = isNewUpvote
        }, cancellationToken);

        return Result<int>.Success(count);
    }
}
