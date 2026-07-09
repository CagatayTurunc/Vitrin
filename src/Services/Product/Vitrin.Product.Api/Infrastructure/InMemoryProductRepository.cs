using Vitrin.Product.Application.Commands;
using Vitrin.Product.Domain.Entities;

namespace Vitrin.Product.Api.Infrastructure;

public class InMemoryProductRepository : IProductRepository
{
    private static readonly List<ProductItem> _products = new();

    public Task AddAsync(ProductItem product, CancellationToken cancellationToken)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }

    public Task<bool> IsSlugUniqueAsync(string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(!_products.Any(p => p.Slug == slug));
    }
    
    public Task<Topic?> GetTopicBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult<Topic?>(null);
    }
    
    public Task<ProductItem?> GetByIdWithUpvotesAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
    }

    public Task UpdateAsync(ProductItem product, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public IEnumerable<ProductItem> GetAll()
    {
        return _products;
    }
}
