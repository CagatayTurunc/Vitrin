using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Auth;

namespace Vitrin.Product.Api.Products;

public static class ProductExperienceEndpoints
{
    public static void MapProductExperienceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products/{productId:guid}/reviews", GetReviews);
        app.MapPut("/api/products/{productId:guid}/reviews/me", UpsertReview).RequireAuthorization();
        app.MapDelete("/api/products/{productId:guid}/reviews/me", DeleteReview).RequireAuthorization();
        app.MapPut("/api/reviews/{reviewId:guid}/helpful", ToggleHelpful).RequireAuthorization();
        app.MapGet("/api/products/{productId:guid}/changelog", GetChangelog);
        app.MapPost("/api/products/{productId:guid}/changelog", CreateChangelog).RequireAuthorization();
        app.MapGet("/api/maker/dashboard", GetMakerDashboard).RequireAuthorization();
        app.MapGet("/api/collections/following", GetFollowedCollections).RequireAuthorization();
        app.MapGet("/api/collections/editorial", GetEditorialCollections);
        app.MapGet("/api/collections/{collectionId:guid}/follow", GetCollectionFollowStatus);
        app.MapPut("/api/collections/{collectionId:guid}/follow", FollowCollection).RequireAuthorization();
        app.MapDelete("/api/collections/{collectionId:guid}/follow", UnfollowCollection).RequireAuthorization();
        app.MapPatch("/api/collections/{collectionId:guid}/editorial", SetEditorial).RequireAuthorization();
    }

    private static async Task<IResult> GetReviews(Guid productId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        var items = await db.ProductReviews.AsNoTracking().Where(item => item.ProductId == productId)
            .OrderByDescending(item => db.ProductReviewHelpfulVotes.Count(vote => vote.ReviewId == item.Id))
            .ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => new ProductReviewResponse(item.Id, item.UserId, item.Rating, item.Title, item.Body,
                item.UsageStatus, item.IsVerified,
                db.ProductReviewHelpfulVotes.Count(vote => vote.ReviewId == item.Id),
                userId != null && db.ProductReviewHelpfulVotes.Any(vote => vote.ReviewId == item.Id && vote.UserId == userId),
                userId != null && item.UserId == userId, item.CreatedAtUtc, item.UpdatedAtUtc))
            .ToListAsync(context.RequestAborted);
        return Results.Ok(new
        {
            AverageRating = items.Count == 0 ? 0 : Math.Round(items.Average(item => item.Rating), 1),
            ReviewCount = items.Count,
            Items = items
        });
    }

    private static async Task<IResult> UpsertReview(Guid productId, UpsertProductReviewRequest request, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == productId && item.Status == ProductStatus.Published, context.RequestAborted);
        if (product is null) return ApiProblemResults.NotFound("Product not found.", "product.not_found");
        if (product.MakerId == userId) return ApiProblemResults.BadRequest("Makers cannot review their own product.", "review.own_product");
        var review = await db.ProductReviews.FirstOrDefaultAsync(item => item.ProductId == productId && item.UserId == userId, context.RequestAborted);
        if (review is null)
        {
            var result = ProductReview.Create(productId, userId.Value, request.Rating, request.Title, request.Body, request.UsageStatus);
            if (!result.IsSuccess) return ApiProblemResults.BadRequest(result.Error, "review.invalid");
            review = result.Value!; db.ProductReviews.Add(review);
        }
        else
        {
            var result = review.Update(request.Rating, request.Title, request.Body, request.UsageStatus);
            if (!result.IsSuccess) return ApiProblemResults.BadRequest(result.Error, "review.invalid");
        }
        await db.SaveChangesAsync(context.RequestAborted);
        return Results.Ok(new { review.Id, review.UpdatedAtUtc });
    }

    private static async Task<IResult> DeleteReview(Guid productId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        await db.ProductReviews.Where(item => item.ProductId == productId && item.UserId == userId).ExecuteDeleteAsync(context.RequestAborted);
        return Results.NoContent();
    }

    private static async Task<IResult> ToggleHelpful(Guid reviewId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        var review = await db.ProductReviews.AsNoTracking().FirstOrDefaultAsync(item => item.Id == reviewId, context.RequestAborted);
        if (review is null) return Results.NotFound();
        if (review.UserId == userId) return ApiProblemResults.BadRequest("You cannot vote for your own review.", "review.own_helpful_vote");
        var vote = await db.ProductReviewHelpfulVotes.FirstOrDefaultAsync(item => item.ReviewId == reviewId && item.UserId == userId, context.RequestAborted);
        if (vote is null) db.ProductReviewHelpfulVotes.Add(ProductReviewHelpful.Create(reviewId, userId.Value)); else db.ProductReviewHelpfulVotes.Remove(vote);
        await db.SaveChangesAsync(context.RequestAborted);
        return Results.Ok(new { IsHelpful = vote is null, Count = await db.ProductReviewHelpfulVotes.CountAsync(item => item.ReviewId == reviewId, context.RequestAborted) });
    }

    private static async Task<IResult> GetChangelog(Guid productId, ProductDbContext db, HttpContext context)
    {
        var entries = await db.ProductChangelogEntries.AsNoTracking().Where(item => item.ProductId == productId)
            .OrderByDescending(item => item.PublishedAtUtc).Take(100)
            .Select(item => new ProductChangelogResponse(item.Id, item.AuthorId, item.Version, item.Title, item.Body, item.PublishedAtUtc))
            .ToListAsync(context.RequestAborted);
        return Results.Ok(entries);
    }

    private static async Task<IResult> CreateChangelog(Guid productId, CreateProductChangelogRequest request, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        var product = await db.Products.Include(item => item.TeamMembers).FirstOrDefaultAsync(item => item.Id == productId, context.RequestAborted);
        if (product is null) return Results.NotFound();
        if (!product.CanEdit(userId.Value)) return Results.Forbid();
        var result = ProductChangelogEntry.Create(productId, userId.Value, request.Version, request.Title, request.Body);
        if (!result.IsSuccess) return ApiProblemResults.BadRequest(result.Error, "changelog.invalid");
        db.ProductChangelogEntries.Add(result.Value!); await db.SaveChangesAsync(context.RequestAborted);
        return Results.Created($"/api/products/{productId}/changelog", new { result.Value!.Id, result.Value.PublishedAtUtc });
    }

    private static async Task<IResult> GetMakerDashboard(HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        var products = await db.Products.AsNoTracking().Where(item => item.MakerId == userId || item.TeamMembers.Any(member => member.UserId == userId))
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new MakerProductControlResponse(
                item.Id, item.Name, item.Slug, item.Status, item.ScheduledLaunchAt,
                item.ThumbnailUrl != "", item.Description.Length >= 100, item.GalleryUrls.Count > 0,
                item.Links.Any(link => link.Title == "Website"), item.Categories.Count > 0, item.Topics.Count > 0,
                db.ProductFollows.Count(follow => follow.ProductId == item.Id),
                db.ProductReviews.Count(review => review.ProductId == item.Id),
                db.ProductReviews.Where(review => review.ProductId == item.Id).Average(review => (double?)review.Rating) ?? 0,
                db.ProductChangelogEntries.Count(entry => entry.ProductId == item.Id),
                db.CommunityThreads.Count(thread => thread.ProductId == item.Id),
                db.CommunityReplies.Count(reply => db.CommunityThreads.Where(thread => thread.ProductId == item.Id).Select(thread => thread.Id).Contains(reply.ThreadId)),
                0))
            .ToListAsync(context.RequestAborted);
        return Results.Ok(products.Select(item => item with
        {
            CompletenessScore = new[] { item.HasLogo, item.HasLongDescription, item.HasGallery, item.HasWebsite, item.HasCategory, item.HasTopics }.Count(value => value) * 100 / 6
        }));
    }

    private static async Task<IResult> GetFollowedCollections(HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        var ids = await db.CollectionFollows.AsNoTracking().Where(item => item.UserId == userId).OrderByDescending(item => item.CreatedAtUtc).Select(item => item.CollectionId).ToListAsync(context.RequestAborted);
        return Results.Ok(ids);
    }
    private static async Task<IResult> GetEditorialCollections(HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        var items = await db.Collections.AsNoTracking()
            .Where(item => item.IsEditorial && item.Visibility == CollectionVisibility.Public)
            .OrderByDescending(item => db.CollectionFollows.Count(follow => follow.CollectionId == item.Id))
            .Select(item => new EditorialCollectionResponse(item.Id, item.Name, item.Slug, item.Description, item.CoverImageUrl,
                item.Products.Count, db.CollectionFollows.Count(follow => follow.CollectionId == item.Id),
                userId != null && db.CollectionFollows.Any(follow => follow.CollectionId == item.Id && follow.UserId == userId)))
            .ToListAsync(context.RequestAborted);
        return Results.Ok(items);
    }
    private static async Task<IResult> FollowCollection(Guid collectionId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        if (!await db.Collections.AnyAsync(item => item.Id == collectionId && item.Visibility == CollectionVisibility.Public, context.RequestAborted)) return Results.NotFound();
        if (!await db.CollectionFollows.AnyAsync(item => item.CollectionId == collectionId && item.UserId == userId, context.RequestAborted))
        { db.CollectionFollows.Add(CollectionFollow.Create(collectionId, userId.Value)); await db.SaveChangesAsync(context.RequestAborted); }
        return Results.Ok(new { IsFollowing = true, Count = await db.CollectionFollows.CountAsync(item => item.CollectionId == collectionId, context.RequestAborted) });
    }
    private static async Task<IResult> GetCollectionFollowStatus(Guid collectionId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        if (!await db.Collections.AnyAsync(item => item.Id == collectionId && item.Visibility == CollectionVisibility.Public, context.RequestAborted)) return Results.NotFound();
        return Results.Ok(new
        {
            IsFollowing = userId != null && await db.CollectionFollows.AnyAsync(item => item.CollectionId == collectionId && item.UserId == userId, context.RequestAborted),
            Count = await db.CollectionFollows.CountAsync(item => item.CollectionId == collectionId, context.RequestAborted)
        });
    }
    private static async Task<IResult> UnfollowCollection(Guid collectionId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        await db.CollectionFollows.Where(item => item.CollectionId == collectionId && item.UserId == userId).ExecuteDeleteAsync(context.RequestAborted);
        return Results.Ok(new { IsFollowing = false, Count = await db.CollectionFollows.CountAsync(item => item.CollectionId == collectionId, context.RequestAborted) });
    }
    private static async Task<IResult> SetEditorial(Guid collectionId, SetCollectionEditorialRequest request, HttpContext context, ProductDbContext db)
    {
        if (!context.User.IsInRole("Admin")) return Results.Forbid();
        var collection = await db.Collections.FindAsync([collectionId], context.RequestAborted); if (collection is null) return Results.NotFound();
        collection.SetEditorial(request.IsEditorial, request.CoverImageUrl); await db.SaveChangesAsync(context.RequestAborted); return Results.NoContent();
    }
}

public sealed record UpsertProductReviewRequest(int Rating, string Title, string Body, ProductUsageStatus UsageStatus);
public sealed record ProductReviewResponse(Guid Id, Guid UserId, int Rating, string Title, string Body, ProductUsageStatus UsageStatus, bool IsVerified, int HelpfulCount, bool IsHelpful, bool IsMine, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateProductChangelogRequest(string Version, string Title, string Body);
public sealed record ProductChangelogResponse(Guid Id, Guid AuthorId, string Version, string Title, string Body, DateTime PublishedAtUtc);
public sealed record SetCollectionEditorialRequest(bool IsEditorial, string? CoverImageUrl);
public sealed record EditorialCollectionResponse(Guid Id, string Name, string Slug, string Description, string CoverImageUrl, int ProductCount, int FollowerCount, bool IsFollowing);
public sealed record MakerProductControlResponse(Guid Id, string Name, string Slug, ProductStatus Status, DateTime? ScheduledLaunchAt, bool HasLogo, bool HasLongDescription, bool HasGallery, bool HasWebsite, bool HasCategory, bool HasTopics, int FollowerCount, int ReviewCount, double AverageRating, int ChangelogCount, int ForumThreadCount, int ForumReplyCount, int CompletenessScore = 0);
