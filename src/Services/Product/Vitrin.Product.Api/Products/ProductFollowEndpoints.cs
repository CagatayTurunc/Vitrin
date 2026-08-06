using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Auth;

namespace Vitrin.Product.Api.Products;

public static class ProductFollowEndpoints
{
    public static void MapProductFollowEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products/following", GetFollowedProducts)
            .WithName("GetFollowedProducts")
            .RequireAuthorization();

        app.MapGet("/api/products/{productId:guid}/follow", GetFollowStatus)
            .WithName("GetProductFollowStatus");

        app.MapPut("/api/products/{productId:guid}/follow", FollowProduct)
            .WithName("FollowProduct")
            .RequireAuthorization();

        app.MapDelete("/api/products/{productId:guid}/follow", UnfollowProduct)
            .WithName("UnfollowProduct")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetFollowedProducts(
        HttpContext context,
        ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var products = await db.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Published &&
                db.ProductFollows.Any(follow => follow.UserId == userId.Value && follow.ProductId == product.Id))
            .OrderByDescending(product => db.ProductFollows
                .Where(follow => follow.UserId == userId.Value && follow.ProductId == product.Id)
                .Select(follow => follow.CreatedAtUtc)
                .FirstOrDefault())
            .Take(200)
            .ProjectToResponse()
            .ToListAsync(context.RequestAborted);

        return Results.Ok(products);
    }

    private static async Task<IResult> GetFollowStatus(
        Guid productId,
        HttpContext context,
        ProductDbContext db)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(product => product.Id == productId, context.RequestAborted))
            return ApiProblemResults.NotFound("Product not found.", "product.not_found");

        var userId = context.User.GetUserId();
        var followerCount = await db.ProductFollows.AsNoTracking()
            .CountAsync(follow => follow.ProductId == productId, context.RequestAborted);
        var isFollowing = userId is not null && await db.ProductFollows.AsNoTracking()
            .AnyAsync(follow => follow.ProductId == productId && follow.UserId == userId.Value, context.RequestAborted);

        return Results.Ok(new ProductFollowStatusResponse(productId, isFollowing, followerCount));
    }

    private static async Task<IResult> FollowProduct(
        Guid productId,
        HttpContext context,
        ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        if (userId is null) return Results.Unauthorized();

        if (!await db.Products.AsNoTracking().AnyAsync(
                product => product.Id == productId && product.Status == ProductStatus.Published,
                context.RequestAborted))
            return ApiProblemResults.NotFound("Published product not found.", "product.not_found");

        var exists = await db.ProductFollows.AnyAsync(
            follow => follow.ProductId == productId && follow.UserId == userId.Value,
            context.RequestAborted);
        if (!exists)
        {
            db.ProductFollows.Add(ProductFollow.Create(userId.Value, productId));
            await db.SaveChangesAsync(context.RequestAborted);
        }

        var followerCount = await db.ProductFollows.CountAsync(
            follow => follow.ProductId == productId,
            context.RequestAborted);
        return Results.Ok(new ProductFollowStatusResponse(productId, true, followerCount));
    }

    private static async Task<IResult> UnfollowProduct(
        Guid productId,
        HttpContext context,
        ProductDbContext db)
    {
        var userId = context.User.GetUserId();
        if (userId is null) return Results.Unauthorized();

        await db.ProductFollows
            .Where(follow => follow.ProductId == productId && follow.UserId == userId.Value)
            .ExecuteDeleteAsync(context.RequestAborted);
        var followerCount = await db.ProductFollows.CountAsync(
            follow => follow.ProductId == productId,
            context.RequestAborted);

        return Results.Ok(new ProductFollowStatusResponse(productId, false, followerCount));
    }
}

public sealed record ProductFollowStatusResponse(Guid ProductId, bool IsFollowing, int FollowerCount);
