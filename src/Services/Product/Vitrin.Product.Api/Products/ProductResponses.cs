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
    int Upvotes,
    int ViewCount,
    int CommentCount,
    double TrendScore = 0,
    double SearchScore = 0,
    string? MatchType = null);

public sealed record CollectionCollaboratorResponse(
    Guid UserId,
    CollectionCollaboratorRole Role,
    DateTime AddedAt);

public sealed record CollectionSummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    Guid UserId,
    CollectionVisibility Visibility,
    DateTime CreatedAt,
    int ProductCount,
    int CollaboratorCount,
    bool IsOwner,
    bool CanEdit);

public sealed record CollectionDetailsResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    Guid UserId,
    CollectionVisibility Visibility,
    DateTime CreatedAt,
    IReadOnlyList<ProductResponse> Products,
    IReadOnlyList<CollectionCollaboratorResponse> Collaborators,
    bool IsOwner = false,
    bool CanEdit = false);

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
            product.Upvotes.Count,
            product.ViewCount,
            product.CommentCount,
            0,
            0,
            null));
    }

    public static IQueryable<CollectionDetailsResponse> ProjectToDetailsResponse(this IQueryable<Collection> query)
    {
        return query.Select(collection => new CollectionDetailsResponse(
            collection.Id,
            collection.Name,
            collection.Slug,
            collection.Description,
            collection.UserId,
            collection.Visibility,
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
                product.Upvotes.Count,
                product.ViewCount,
                product.CommentCount,
                0,
                0,
                null)).ToList(),
            collection.Collaborators
                .OrderBy(member => member.AddedAt)
                .Select(member => new CollectionCollaboratorResponse(
                    member.UserId,
                    member.Role,
                    member.AddedAt))
                .ToList(),
            false,
            false));
    }
}
