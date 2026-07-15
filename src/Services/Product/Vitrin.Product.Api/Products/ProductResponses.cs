using Vitrin.Product.Domain.Entities;

namespace Vitrin.Product.Api.Products;

public sealed record TopicResponse(Guid Id, string Name, string Slug);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string Tagline,
    string Description,
    string ThumbnailUrl,
    IReadOnlyList<string> GalleryUrls,
    Guid MakerId,
    ProductStatus Status,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    IReadOnlyList<TopicResponse> Topics,
    int Upvotes);

public sealed record CollectionDetailsResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    Guid UserId,
    DateTime CreatedAt,
    IReadOnlyList<ProductResponse> Products);

public static class ProductQueryExtensions
{
    public static IQueryable<ProductResponse> ProjectToResponse(this IQueryable<ProductItem> query)
    {
        return query.Select(product => new ProductResponse(
            product.Id,
            product.Name,
            product.Slug,
            product.Tagline,
            product.Description,
            product.ThumbnailUrl,
            product.GalleryUrls,
            product.MakerId,
            product.Status,
            product.CreatedAt,
            product.PublishedAt,
            product.Topics
                .OrderBy(topic => topic.Name)
                .Select(topic => new TopicResponse(topic.Id, topic.Name, topic.Slug))
                .ToList(),
            product.Upvotes.Count));
    }

    public static IQueryable<CollectionDetailsResponse> ProjectToDetailsResponse(this IQueryable<Collection> query)
    {
        return query.Select(collection => new CollectionDetailsResponse(
            collection.Id,
            collection.Name,
            collection.Slug,
            collection.Description,
            collection.UserId,
            collection.CreatedAt,
            collection.Products.Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Slug,
                product.Tagline,
                product.Description,
                product.ThumbnailUrl,
                product.GalleryUrls,
                product.MakerId,
                product.Status,
                product.CreatedAt,
                product.PublishedAt,
                product.Topics
                    .OrderBy(topic => topic.Name)
                    .Select(topic => new TopicResponse(topic.Id, topic.Name, topic.Slug))
                    .ToList(),
                product.Upvotes.Count)).ToList()));
    }
}
