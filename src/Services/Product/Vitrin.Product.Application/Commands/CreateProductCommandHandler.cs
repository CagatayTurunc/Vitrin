using MediatR;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public interface IProductRepository
{
    Task AddAsync(ProductItem product, CancellationToken cancellationToken);
    Task<bool> IsSlugUniqueAsync(string slug, CancellationToken cancellationToken);
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
            request.Slug);

        await _repository.AddAsync(product, cancellationToken);
        
        // We can publish ProductCreatedEvent here or via an Outbox pattern in Infrastructure layer
        
        return Result<Guid>.Success(product.Id);
    }
}
