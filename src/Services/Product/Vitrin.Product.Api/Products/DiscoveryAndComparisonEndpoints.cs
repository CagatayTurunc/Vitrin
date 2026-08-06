using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Auth;

namespace Vitrin.Product.Api.Products;

public static class DiscoveryAndComparisonEndpoints
{
    public static void MapDiscoveryAndComparisonEndpoints(this WebApplication app)
    {
        app.MapGet("/api/discover/personalized", GetPersonalizedDiscover).RequireAuthorization();
        app.MapPut("/api/discover/products/{productId:guid}/signal", SetDiscoverySignal).RequireAuthorization();
        app.MapDelete("/api/discover/products/{productId:guid}/signal", ClearDiscoverySignal).RequireAuthorization();
        app.MapGet("/api/community/users/{userId:guid}/reputation", GetUserReputation);
        app.MapGet("/api/products/{productId:guid}/market-profile", GetMarketProfile);
        app.MapPut("/api/products/{productId:guid}/market-profile", UpdateMarketProfile).RequireAuthorization();
        app.MapGet("/api/products/compare/details", GetComparisonDetails);
        app.MapGet("/api/products/{slug}/alternatives", GetAlternatives);
    }

    private static async Task<IResult> GetPersonalizedDiscover(string? mode, int? take, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        if (userId is null) return Results.Unauthorized();
        var limit = Math.Clamp(take ?? 30, 1, 60);
        var hiddenIds = await db.ProductDiscoverySignals.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => item.ProductId).ToListAsync(context.RequestAborted);
        var followedProductIds = await db.ProductFollows.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => item.ProductId).ToListAsync(context.RequestAborted);
        var followedTopicIds = await db.TopicFollows.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => item.TopicId).ToListAsync(context.RequestAborted);
        var votedProductIds = await db.ProductUpvotes.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => item.ProductItemId).ToListAsync(context.RequestAborted);
        var affinityTopicIds = await db.Products.AsNoTracking().Where(item => votedProductIds.Contains(item.Id))
            .SelectMany(item => item.Topics.Select(topic => topic.Id)).Distinct().ToListAsync(context.RequestAborted);
        var interestTopicIds = followedTopicIds.Concat(affinityTopicIds).Distinct().ToHashSet();

        var query = db.Products.AsNoTracking().Where(item => item.Status == ProductStatus.Published && !hiddenIds.Contains(item.Id));
        var normalizedMode = mode?.Trim().ToLowerInvariant() ?? "for-you";
        if (normalizedMode == "following") query = query.Where(item => followedProductIds.Contains(item.Id));
        if (normalizedMode == "undiscovered") query = query.Where(item => item.Upvotes.Count <= 15);
        var candidates = await query.OrderByDescending(item => item.PublishedAt).Take(250).ProjectToResponse().ToListAsync(context.RequestAborted);

        var items = candidates.Select(product =>
        {
            var matchedTopics = product.Topics.Where(topic => interestTopicIds.Contains(topic.Id)).Select(topic => topic.Name).Take(2).ToArray();
            var followed = followedProductIds.Contains(product.Id);
            var score = matchedTopics.Length * 20d + (followed ? 35 : 0) + Math.Log10(product.Upvotes + 1) * 8 +
                        Math.Log10(product.CommentCount + 1) * 5 + Math.Log10(product.ViewCount + 1) * 2;
            if (normalizedMode == "undiscovered") score += Math.Max(0, 20 - product.Upvotes);
            var reason = followed ? "Takip ettiğin ürün" : matchedTopics.Length > 0
                ? $"{string.Join(" ve ", matchedTopics)} ilgine göre"
                : normalizedMode == "undiscovered" ? "Yeni ve henüz az keşfedilmiş" : "Toplulukta yükseliyor";
            return new DiscoverRecommendationResponse(product, reason, Math.Round(score, 2));
        }).OrderByDescending(item => item.Score).ThenByDescending(item => item.Product.PublishedAt).Take(limit).ToList();
        return Results.Ok(items);
    }

    private static async Task<IResult> SetDiscoverySignal(Guid productId, SetDiscoverySignalRequest request, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        var signal = await db.ProductDiscoverySignals.FirstOrDefaultAsync(item => item.UserId == userId && item.ProductId == productId, context.RequestAborted);
        if (signal is null) db.ProductDiscoverySignals.Add(ProductDiscoverySignal.Create(userId.Value, productId, request.Kind));
        else signal.ChangeKind(request.Kind);
        await db.SaveChangesAsync(context.RequestAborted); return Results.NoContent();
    }
    private static async Task<IResult> ClearDiscoverySignal(Guid productId, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        await db.ProductDiscoverySignals.Where(item => item.UserId == userId && item.ProductId == productId).ExecuteDeleteAsync(context.RequestAborted);
        return Results.NoContent();
    }

    private static async Task<IResult> GetUserReputation(Guid userId, ProductDbContext db, HttpContext context)
    {
        var productCount = await db.Products.CountAsync(item => item.MakerId == userId && item.Status == ProductStatus.Published, context.RequestAborted);
        var reviewCount = await db.ProductReviews.CountAsync(item => item.UserId == userId, context.RequestAborted);
        var helpfulVotes = await db.ProductReviewHelpfulVotes.CountAsync(vote => db.ProductReviews.Where(review => review.UserId == userId).Select(review => review.Id).Contains(vote.ReviewId), context.RequestAborted);
        var threadCount = await db.CommunityThreads.CountAsync(item => item.AuthorId == userId, context.RequestAborted);
        var replyCount = await db.CommunityReplies.CountAsync(item => item.AuthorId == userId, context.RequestAborted);
        var officialReplyCount = await db.CommunityReplies.CountAsync(item => item.AuthorId == userId && item.IsOfficial, context.RequestAborted);
        var productFollowers = await db.ProductFollows.CountAsync(follow => db.Products.Where(product => product.MakerId == userId).Select(product => product.Id).Contains(follow.ProductId), context.RequestAborted);
        var score = productCount * 40 + reviewCount * 12 + helpfulVotes * 4 + threadCount * 8 + replyCount * 3 + officialReplyCount * 8 + Math.Min(productFollowers, 500);
        var level = score >= 500 ? "Topluluk lideri" : score >= 200 ? "Deneyimli maker" : score >= 75 ? "Aktif katkıcı" : "Yeni keşifçi";
        return Results.Ok(new UserReputationResponse(score, level, productCount, reviewCount, helpfulVotes, threadCount, replyCount, officialReplyCount, productFollowers));
    }

    private static async Task<IResult> GetMarketProfile(Guid productId, ProductDbContext db, HttpContext context)
    {
        var profile = await db.ProductMarketProfiles.AsNoTracking().FirstOrDefaultAsync(item => item.ProductId == productId, context.RequestAborted);
        return Results.Ok(ToMarketProfile(productId, profile));
    }
    private static async Task<IResult> UpdateMarketProfile(Guid productId, UpdateMarketProfileRequest request, HttpContext context, ProductDbContext db)
    {
        var userId = context.User.GetUserId(); if (userId is null) return Results.Unauthorized();
        var product = await db.Products.Include(item => item.TeamMembers).FirstOrDefaultAsync(item => item.Id == productId, context.RequestAborted);
        if (product is null) return Results.NotFound(); if (!product.CanEdit(userId.Value)) return Results.Forbid();
        var profile = await db.ProductMarketProfiles.FirstOrDefaultAsync(item => item.ProductId == productId, context.RequestAborted);
        if (profile is null) { profile = ProductMarketProfile.Create(productId); db.ProductMarketProfiles.Add(profile); }
        profile.Update(request.PricingModel, request.Platforms, request.HasFreePlan, request.HasTurkishSupport, request.SupportsTryPricing,
            request.IsKvkkCompliant, request.SupportsEFatura, request.SupportsLocalPayments, request.IsOpenSource);
        await db.SaveChangesAsync(context.RequestAborted); return Results.Ok(ToMarketProfile(productId, profile));
    }

    private static async Task<IResult> GetComparisonDetails(string ids, ProductDbContext db, HttpContext context)
    {
        var requested = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().Take(4).ToList();
        if (requested.Count == 0) return ApiProblemResults.BadRequest("Choose at least one product.", "product.invalid_comparison_size");
        var products = await db.Products.AsNoTracking().Where(item => requested.Contains(item.Id) && item.Status == ProductStatus.Published)
            .ProjectToResponse().ToListAsync(context.RequestAborted);
        var profiles = await db.ProductMarketProfiles.AsNoTracking().Where(item => requested.Contains(item.ProductId)).ToListAsync(context.RequestAborted);
        var items = products.Select(product => new ProductComparisonDetailsResponse(
            product, ToMarketProfile(product.Id, profiles.FirstOrDefault(item => item.ProductId == product.Id)),
            db.ProductReviews.Count(review => review.ProductId == product.Id),
            Math.Round(db.ProductReviews.Where(review => review.ProductId == product.Id).Average(review => (double?)review.Rating) ?? 0, 1),
            db.ProductFollows.Count(follow => follow.ProductId == product.Id))).ToList();
        return Results.Ok(new { Items = items, MaxProducts = 4 });
    }

    private static async Task<IResult> GetAlternatives(string slug, int? take, ProductDbContext db, HttpContext context)
    {
        var source = await db.Products.AsNoTracking().Where(item => item.Slug == slug && item.Status == ProductStatus.Published)
            .Select(item => new { item.Id, TopicIds = item.Topics.Select(topic => topic.Id).ToList(), CategoryIds = item.Categories.Select(category => category.Id).ToList() })
            .FirstOrDefaultAsync(context.RequestAborted);
        if (source is null) return Results.NotFound();
        var alternatives = await db.Products.AsNoTracking().Where(item => item.Id != source.Id && item.Status == ProductStatus.Published &&
            (item.Topics.Any(topic => source.TopicIds.Contains(topic.Id)) || item.Categories.Any(category => source.CategoryIds.Contains(category.Id))))
            .OrderByDescending(item => item.Upvotes.Count * 3 + item.CommentCount * 2 + item.ViewCount / 10)
            .Take(Math.Clamp(take ?? 8, 1, 20)).ProjectToResponse().ToListAsync(context.RequestAborted);
        return Results.Ok(alternatives);
    }

    private static ProductMarketProfileResponse ToMarketProfile(Guid productId, ProductMarketProfile? profile) => profile is null
        ? new(productId, "unspecified", ["web"], false, false, false, false, false, false, false)
        : new(productId, profile.PricingModel, profile.GetPlatforms(), profile.HasFreePlan, profile.HasTurkishSupport,
            profile.SupportsTryPricing, profile.IsKvkkCompliant, profile.SupportsEFatura, profile.SupportsLocalPayments, profile.IsOpenSource);
}

public sealed record SetDiscoverySignalRequest(DiscoverySignalKind Kind);
public sealed record DiscoverRecommendationResponse(ProductResponse Product, string Reason, double Score);
public sealed record UserReputationResponse(int Score, string Level, int ProductCount, int ReviewCount, int HelpfulVotes, int ThreadCount, int ReplyCount, int OfficialReplyCount, int ProductFollowers);
public sealed record UpdateMarketProfileRequest(string PricingModel, IReadOnlyList<string>? Platforms, bool HasFreePlan, bool HasTurkishSupport, bool SupportsTryPricing, bool IsKvkkCompliant, bool SupportsEFatura, bool SupportsLocalPayments, bool IsOpenSource);
public sealed record ProductMarketProfileResponse(Guid ProductId, string PricingModel, IReadOnlyList<string> Platforms, bool HasFreePlan, bool HasTurkishSupport, bool SupportsTryPricing, bool IsKvkkCompliant, bool SupportsEFatura, bool SupportsLocalPayments, bool IsOpenSource);
public sealed record ProductComparisonDetailsResponse(ProductResponse Product, ProductMarketProfileResponse MarketProfile, int ReviewCount, double AverageRating, int FollowerCount);
