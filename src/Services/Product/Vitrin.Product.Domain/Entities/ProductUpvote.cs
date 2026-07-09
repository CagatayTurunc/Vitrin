using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

public class ProductUpvote : Entity
{
    public Guid ProductItemId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductUpvote() { }

    public ProductUpvote(Guid productItemId, Guid userId)
    {
        Id = Guid.NewGuid();
        ProductItemId = productItemId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }
}
