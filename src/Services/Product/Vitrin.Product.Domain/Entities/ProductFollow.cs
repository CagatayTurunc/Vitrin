using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

public sealed class ProductFollow : Entity
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ProductFollow() { }

    public static ProductFollow Create(Guid userId, Guid productId, DateTime? utcNow = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (productId == Guid.Empty) throw new ArgumentException("Product id is required.", nameof(productId));

        return new ProductFollow
        {
            UserId = userId,
            ProductId = productId,
            CreatedAtUtc = utcNow ?? DateTime.UtcNow
        };
    }
}
