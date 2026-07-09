using MediatR;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public interface IProductRepository
{
    Task AddAsync(ProductItem product, CancellationToken cancellationToken);
    Task<bool> IsSlugUniqueAsync(string slug, CancellationToken cancellationToken);
    Task<Topic?> GetTopicBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<ProductItem?> GetByIdWithUpvotesAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(ProductItem product, CancellationToken cancellationToken);
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;

    public CreateProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var isSlugUnique = await _repository.IsSlugUniqueAsync(request.Slug, cancellationToken);
        if (!isSlugUnique)
        {
            return Result<Guid>.Failure("This slug is already in use.");
        }

        var product = ProductItem.Create(
            request.MakerId,
            request.Name,
            request.Tagline,
            request.Description,
            request.Slug,
            request.ThumbnailUrl);

        // Auto submit for review
        product.SubmitForReview();

        if (request.Topics != null)
        {
            foreach (var t in request.Topics)
            {
                var topicName = t.Trim();
                if (!string.IsNullOrEmpty(topicName))
                {
                    var slug = topicName.ToLower().Replace(" ", "-");
                    var existingTopic = await _repository.GetTopicBySlugAsync(slug, cancellationToken);
                    if (existingTopic != null)
                    {
                        product.AddTopic(existingTopic);
                    }
                    else
                    {
                        product.AddTopic(Topic.Create(topicName, slug));
                    }
                }
            }
        }

        await _repository.AddAsync(product, cancellationToken);
        
        // We can publish ProductCreatedEvent here or via an Outbox pattern in Infrastructure layer
        
        return Result<Guid>.Success(product.Id);
    }
}
