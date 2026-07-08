using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

public class Topic : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    private Topic() { } // EF Core
    
    public static Topic Create(string name, string slug)
    {
        return new Topic
        {
            Name = name,
            Slug = slug
        };
    }
}
