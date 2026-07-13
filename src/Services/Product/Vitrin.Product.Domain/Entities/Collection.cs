using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

public class Collection : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<ProductItem> _products = new();
    public IReadOnlyList<ProductItem> Products => _products.AsReadOnly();
    
    private Collection() { } // EF Core
    
    public static Collection Create(Guid userId, string name, string slug, string description)
    {
        return new Collection
        {
            UserId = userId,
            Name = name,
            Slug = slug,
            Description = description ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void AddProduct(ProductItem product)
    {
        if (!_products.Any(p => p.Id == product.Id))
        {
            _products.Add(product);
        }
    }
    
    public void RemoveProduct(Guid productId)
    {
        var product = _products.FirstOrDefault(p => p.Id == productId);
        if (product != null)
        {
            _products.Remove(product);
        }
    }
}
