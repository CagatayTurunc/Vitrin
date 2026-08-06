using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Auth;

namespace Vitrin.Product.Api.Products;

public static class AdminOperationsEndpoints
{
    public static void MapAdminOperationsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products/admin/dashboard", GetDashboard)
            .WithName("GetProductAdminDashboard")
            .RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

        app.MapGet("/api/products/admin/curation", GetCurationQueue)
            .WithName("GetProductCurationQueue")
            .RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

        app.MapPatch("/api/products/admin/launches/{launchId:guid}/feature", SetFeatured)
            .WithName("SetLaunchFeatured")
            .RequireAuthorization(VitrinAuthDefaults.AdminPolicy);
    }

    private static async Task<IResult> GetDashboard(ProductDbContext db, HttpContext context)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var seriesStart = today.AddDays(-29);

        var totalProducts = await db.Products.AsNoTracking().CountAsync(context.RequestAborted);
        var publishedProducts = await db.Products.AsNoTracking()
            .CountAsync(product => product.Status == ProductStatus.Published, context.RequestAborted);
        var pendingProducts = await db.Products.AsNoTracking()
            .CountAsync(product => product.Status == ProductStatus.UnderReview, context.RequestAborted);
        var scheduledProducts = await db.Products.AsNoTracking()
            .CountAsync(product => product.Status == ProductStatus.Scheduled, context.RequestAborted);
        var totalLaunches = await db.ProductLaunches.AsNoTracking().CountAsync(context.RequestAborted);
        var launchesToday = await db.ProductLaunches.AsNoTracking()
            .CountAsync(launch => launch.PublishedAtUtc >= today && launch.PublishedAtUtc < today.AddDays(1), context.RequestAborted);
        var totalViews = await db.Products.AsNoTracking().SumAsync(product => (long)product.ViewCount, context.RequestAborted);
        var totalFollowers = await db.ProductFollows.AsNoTracking().CountAsync(context.RequestAborted);

        var rawSeries = await db.Products.AsNoTracking()
            .Where(product => product.CreatedAt >= seriesStart)
            .GroupBy(product => product.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToListAsync(context.RequestAborted);
        var countsByDate = rawSeries.ToDictionary(item => DateOnly.FromDateTime(item.Date), item => item.Count);
        var submissionSeries = Enumerable.Range(0, 30)
            .Select(offset => DateOnly.FromDateTime(seriesStart.AddDays(offset)))
            .Select(date => new AdminDailyMetric(date, countsByDate.GetValueOrDefault(date)))
            .ToList();

        var recentProducts = await db.Products.AsNoTracking()
            .OrderByDescending(product => product.CreatedAt)
            .Take(8)
            .Select(product => new AdminRecentProduct(
                product.Id,
                product.Name,
                product.Slug,
                product.Status,
                product.CreatedAt,
                product.ScheduledLaunchAt))
            .ToListAsync(context.RequestAborted);

        return Results.Ok(new AdminDashboardResponse(
            now,
            totalProducts,
            publishedProducts,
            pendingProducts,
            scheduledProducts,
            totalLaunches,
            launchesToday,
            totalViews,
            totalFollowers,
            submissionSeries,
            recentProducts));
    }

    private static async Task<IResult> GetCurationQueue(ProductDbContext db, HttpContext context)
    {
        var rows = await db.Products.AsNoTracking()
            .Where(product => product.Status == ProductStatus.UnderReview || product.Status == ProductStatus.Scheduled)
            .OrderBy(product => product.Status == ProductStatus.UnderReview ? 0 : 1)
            .ThenBy(product => product.CreatedAt)
            .Take(200)
            .Select(product => new
            {
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
                product.ScheduledLaunchAt,
                TopicCount = product.Topics.Count,
                Categories = product.Categories
                    .OrderBy(category => category.SortOrder)
                    .Select(category => category.Name)
                    .ToList(),
                HasWebsite = product.Links.Any(link => link.Title == "Website"),
                Launch = product.Launches
                    .OrderByDescending(launch => launch.SequenceNumber)
                    .Select(launch => new
                    {
                        launch.Id,
                        launch.VersionLabel,
                        launch.Tagline,
                        launch.Status,
                        launch.ScheduledAtUtc,
                        launch.IsFeatured
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(context.RequestAborted);

        var queue = rows.Select(row =>
        {
            var signals = new List<string>();
            var score = 0;
            if (!string.IsNullOrWhiteSpace(row.ThumbnailUrl)) score += 20; else signals.Add("Thumbnail eksik");
            if (row.GalleryUrls.Count > 0) score += 15; else signals.Add("Galeri eksik");
            if (row.Description.Length >= 200) score += 20; else signals.Add("Açıklama kısa");
            if (row.Categories.Count > 0) score += 15; else signals.Add("Kategori eksik");
            if (row.TopicCount > 0) score += 15; else signals.Add("Topic eksik");
            if (row.HasWebsite) score += 15; else signals.Add("Web sitesi eksik");

            return new CurationQueueItem(
                row.Id,
                row.Name,
                row.Slug,
                row.Tagline,
                row.MakerId,
                row.Status,
                row.CreatedAt,
                row.ScheduledLaunchAt,
                score,
                signals,
                row.Categories,
                row.Launch is null ? null : new CurationLaunch(
                    row.Launch.Id,
                    row.Launch.VersionLabel,
                    row.Launch.Tagline,
                    row.Launch.Status,
                    row.Launch.ScheduledAtUtc,
                    row.Launch.IsFeatured));
        }).ToList();

        return Results.Ok(queue);
    }

    private static async Task<IResult> SetFeatured(
        Guid launchId,
        SetLaunchFeaturedRequest request,
        ProductDbContext db,
        HttpContext context,
        IAuditLogger auditLogger)
    {
        var launch = await db.ProductLaunches.FirstOrDefaultAsync(
            item => item.Id == launchId,
            context.RequestAborted);
        if (launch is null)
            return ApiProblemResults.NotFound("Launch not found.", "launch.not_found");

        launch.SetFeatured(request.Featured);
        await db.SaveChangesAsync(context.RequestAborted);
        await auditLogger.WriteAsync(
            new AuditEvent(
                request.Featured ? "admin.launch_featured" : "admin.launch_unfeatured",
                context.User.GetUserId(),
                "ProductLaunch",
                launchId.ToString(),
                "Succeeded",
                context.TraceIdentifier),
            context.RequestAborted);

        return Results.Ok(new { launch.Id, launch.IsFeatured });
    }
}

public sealed record AdminDailyMetric(DateOnly Date, int Count);
public sealed record AdminRecentProduct(Guid Id, string Name, string Slug, ProductStatus Status, DateTime CreatedAt, DateTime? ScheduledLaunchAt);
public sealed record AdminDashboardResponse(
    DateTime GeneratedAtUtc,
    int TotalProducts,
    int PublishedProducts,
    int PendingProducts,
    int ScheduledProducts,
    int TotalLaunches,
    int LaunchesToday,
    long TotalViews,
    int TotalFollowers,
    IReadOnlyList<AdminDailyMetric> SubmissionSeries,
    IReadOnlyList<AdminRecentProduct> RecentProducts);
public sealed record CurationLaunch(Guid Id, string VersionLabel, string Tagline, ProductLaunchStatus Status, DateTime? ScheduledAtUtc, bool IsFeatured);
public sealed record CurationQueueItem(
    Guid Id,
    string Name,
    string Slug,
    string Tagline,
    Guid MakerId,
    ProductStatus Status,
    DateTime CreatedAt,
    DateTime? ScheduledLaunchAt,
    int CompletenessScore,
    IReadOnlyList<string> Signals,
    IReadOnlyList<string> Categories,
    CurationLaunch? Launch);
public sealed record SetLaunchFeaturedRequest(bool Featured);
