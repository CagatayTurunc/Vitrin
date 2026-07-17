using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

public enum CollectionVisibility
{
    Private = 0,
    Public = 1,
    Shared = 2
}

public enum CollectionCollaboratorRole
{
    Viewer = 0,
    Editor = 1
}

public class Collection : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public CollectionVisibility Visibility { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<ProductItem> _products = new();
    public IReadOnlyList<ProductItem> Products => _products.AsReadOnly();

    private readonly List<CollectionCollaborator> _collaborators = new();
    public IReadOnlyList<CollectionCollaborator> Collaborators => _collaborators.AsReadOnly();
    
    private Collection() { } // EF Core
    
    public static Collection Create(
        Guid userId,
        string name,
        string slug,
        string description,
        CollectionVisibility visibility = CollectionVisibility.Public)
    {
        return new Collection
        {
            UserId = userId,
            Name = name,
            Slug = slug,
            Description = description ?? string.Empty,
            Visibility = visibility,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool CanView(Guid? userId) =>
        Visibility == CollectionVisibility.Public ||
        userId == UserId ||
        (userId is not null && _collaborators.Any(member => member.UserId == userId));

    public bool CanEdit(Guid userId) =>
        userId == UserId ||
        _collaborators.Any(member =>
            member.UserId == userId && member.Role == CollectionCollaboratorRole.Editor);

    public void SetVisibility(CollectionVisibility visibility) => Visibility = visibility;

    public void AddOrUpdateCollaborator(Guid userId, CollectionCollaboratorRole role)
    {
        if (userId == UserId) return;

        var existing = _collaborators.FirstOrDefault(member => member.UserId == userId);
        if (existing is null)
        {
            _collaborators.Add(CollectionCollaborator.Create(Id, userId, role));
            return;
        }

        existing.ChangeRole(role);
    }

    public void RemoveCollaborator(Guid userId)
    {
        var collaborator = _collaborators.FirstOrDefault(member => member.UserId == userId);
        if (collaborator is not null) _collaborators.Remove(collaborator);
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

public sealed class CollectionCollaborator : Entity
{
    public Guid CollectionId { get; private set; }
    public Guid UserId { get; private set; }
    public CollectionCollaboratorRole Role { get; private set; }
    public DateTime AddedAt { get; private set; }

    private CollectionCollaborator() { }

    internal static CollectionCollaborator Create(
        Guid collectionId,
        Guid userId,
        CollectionCollaboratorRole role) => new()
    {
        CollectionId = collectionId,
        UserId = userId,
        Role = role,
        AddedAt = DateTime.UtcNow
    };

    internal void ChangeRole(CollectionCollaboratorRole role) => Role = role;
}
