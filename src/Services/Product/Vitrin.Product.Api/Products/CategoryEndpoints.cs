using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Api;

namespace Vitrin.Product.Api.Products;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/categories", async (ProductDbContext db, HttpContext context) =>
        {
            var categories = await db.ProductCategories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.ParentId != null)
                .ThenBy(category => category.SortOrder)
                .ThenBy(category => category.Name)
                .Select(category => new CategoryDetailsResponse(
                    category.Id,
                    category.Name,
                    category.Slug,
                    category.Description,
                    category.ParentId,
                    category.SortOrder,
                    category.IsActive,
                    db.Products.Count(product => product.Status == ProductStatus.Published && product.Categories.Any(item => item.Id == category.Id))))
                .ToListAsync(context.RequestAborted);

            return Results.Ok(categories);
        })
        .WithName("GetProductCategories")
        .WithOpenApi();

        app.MapGet("/api/categories/{slug}", async (string slug, ProductDbContext db, HttpContext context) =>
        {
            var category = await db.ProductCategories
                .AsNoTracking()
                .Where(item => item.IsActive && item.Slug == slug.ToLower())
                .Select(item => new CategoryDetailsResponse(
                    item.Id,
                    item.Name,
                    item.Slug,
                    item.Description,
                    item.ParentId,
                    item.SortOrder,
                    item.IsActive,
                    db.Products.Count(product => product.Status == ProductStatus.Published && product.Categories.Any(category => category.Id == item.Id))))
                .FirstOrDefaultAsync(context.RequestAborted);

            return category is null ? Results.NotFound() : Results.Ok(category);
        })
        .WithName("GetProductCategory")
        .WithOpenApi();

        app.MapGet("/api/categories/{slug}/products", async (
            string slug,
            string? period,
            int? limit,
            ProductDbContext db,
            HttpContext context) =>
        {
            var requestedLimit = limit ?? 30;
            if (requestedLimit is < 1 or > 100)
                return ApiProblemResults.BadRequest("Limit must be between 1 and 100.", "category.invalid_limit");

            var normalizedPeriod = period?.Trim().ToLowerInvariant() ?? "all";
            var fromUtc = normalizedPeriod switch
            {
                "today" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddDays(-30),
                "all" => (DateTime?)null,
                _ => DateTime.MinValue
            };
            if (fromUtc == DateTime.MinValue)
                return ApiProblemResults.BadRequest("Period must be today, week, month or all.", "category.invalid_period");

            var query = db.Products.AsNoTracking()
                .Where(product => product.Status == ProductStatus.Published && product.Categories.Any(category => category.Slug == slug));
            if (fromUtc is { } from) query = query.Where(product => product.PublishedAt >= from);

            var products = await query
                .OrderByDescending(product => product.Upvotes.Count * 2 + product.CommentCount)
                .ThenByDescending(product => product.PublishedAt)
                .Take(requestedLimit)
                .ProjectToResponse()
                .ToListAsync(context.RequestAborted);

            return Results.Ok(products);
        })
        .WithName("GetProductsByCategory")
        .WithOpenApi();
    }
}

public sealed record CategoryDetailsResponse(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    int ProductCount);
