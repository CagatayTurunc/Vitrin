using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

public class ProductLink : Entity
{
    public Guid ProductId { get; private set; }
    public string Title { get; private set; } = string.Empty; // e.g., "Website", "App Store", "GitHub"
    public string Url { get; private set; } = string.Empty;

    private ProductLink() { } // EF Core
    
    internal ProductLink(Guid productId, string title, string url)
    {
        ProductId = productId;
        Title = title;
        Url = url;
    }
}
