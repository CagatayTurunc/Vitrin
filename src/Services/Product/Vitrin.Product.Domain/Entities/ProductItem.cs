using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Domain.Entities;

public class ProductItem : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Tagline { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ThumbnailUrl { get; private set; } = string.Empty;
    public List<string> GalleryUrls { get; private set; } = new();
    
    public Guid MakerId { get; private set; }
    public ProductStatus Status { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    
    private readonly List<ProductLink> _links = new();
    public IReadOnlyList<ProductLink> Links => _links.AsReadOnly();
    
    private readonly List<Topic> _topics = new();
    public IReadOnlyList<Topic> Topics => _topics.AsReadOnly();

    private ProductItem() { } // EF Core
    
    public static ProductItem Create(Guid makerId, string name, string tagline, string description, string slug)
    {
        var product = new ProductItem
        {
            MakerId = makerId,
            Name = name,
            Tagline = tagline,
            Description = description,
            Slug = slug,
            Status = ProductStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        
        // Add Domain Event if necessary (e.g., ProductCreatedEvent)
        return product;
    }

    public Result Publish()
    {
        if (Status == ProductStatus.Published)
            return Result.Failure("Product is already published.");
            
        Status = ProductStatus.Published;
        PublishedAt = DateTime.UtcNow;
        
        // Add ProductPublishedEvent
        return Result.Success();
    }
    
    public void AddLink(string title, string url)
    {
        _links.Add(new ProductLink(Id, title, url));
    }
    
    public void AddTopic(Topic topic)
    {
        if (!_topics.Any(t => t.Id == topic.Id))
        {
            _topics.Add(topic);
        }
    }
}
