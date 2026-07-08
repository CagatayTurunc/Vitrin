using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Voting.Domain.Entities;

public class Vote : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private Vote() { } // EF Core
    
    public static Vote Create(Guid userId, Guid productId)
    {
        return new Vote
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
