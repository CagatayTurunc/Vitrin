using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Product.Domain.Entities;

/// <summary>
/// Controlled, hierarchical product taxonomy. Unlike topics, categories describe
/// what a product is and which primary problem it solves.
/// </summary>
public sealed class ProductCategory : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    private ProductCategory() { }

    public static ProductCategory Create(
        string name,
        string slug,
        string description,
        Guid? parentId = null,
        int sortOrder = 0,
        Guid? id = null)
    {
        var category = new ProductCategory
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description.Trim(),
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = true
        };
        if (id is { } stableId) category.Id = stableId;
        return category;
    }
}
