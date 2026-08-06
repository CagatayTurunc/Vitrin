using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Domain.Services;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Api;

namespace Vitrin.Product.Api.Products;

public static class LaunchEndpoints
{
    public static void MapLaunchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/launches/daily", GetDailyLaunches)
            .WithName("GetDailyLaunches")
            .WithOpenApi();

        app.MapGet("/api/launches/archive", GetLaunchArchive)
            .WithName("GetLaunchArchive")
            .WithOpenApi();

        app.MapGet("/api/launches/upcoming", GetUpcomingLaunches)
            .WithName("GetUpcomingLaunches")
            .WithOpenApi();
    }

    private static async Task<IResult> GetDailyLaunches(
        DateOnly? date,
        int? limit,
        ProductDbContext db,
        HttpContext context)
    {
        var requestedLimit = limit ?? 100;
        if (requestedLimit is < 1 or > 100)
            return ApiProblemResults.BadRequest("Limit must be between 1 and 100.", "launch.invalid_limit");

        var localToday = IstanbulClock.Today();
        var requestedDate = date ?? localToday;
        var (startUtc, endUtc) = IstanbulClock.GetUtcDay(requestedDate);
        var nowUtc = DateTime.UtcNow;

        var rows = await QueryPublishedLaunches(db, startUtc, endUtc)
            .Take(500)
            .ToListAsync(context.RequestAborted);

        var ranked = Rank(rows, nowUtc < endUtc ? nowUtc : endUtc)
            .Take(requestedLimit)
            .ToList();

        return Results.Ok(new DailyLaunchesResponse(
            requestedDate,
            startUtc,
            endUtc,
            requestedDate == localToday,
            ranked));
    }

    private static async Task<IResult> GetLaunchArchive(
        int? days,
        ProductDbContext db,
        HttpContext context)
    {
        var requestedDays = days ?? 30;
        if (requestedDays is < 1 or > 365)
            return ApiProblemResults.BadRequest("Days must be between 1 and 365.", "launch.invalid_days");

        var today = IstanbulClock.Today();
        var firstDay = today.AddDays(-(requestedDays - 1));
        var (startUtc, _) = IstanbulClock.GetUtcDay(firstDay);
        var (_, endUtc) = IstanbulClock.GetUtcDay(today);

        var rows = await QueryPublishedLaunches(db, startUtc, endUtc)
            .Take(10_000)
            .ToListAsync(context.RequestAborted);

        var grouped = Enumerable.Range(0, requestedDays)
            .Select(offset => today.AddDays(-offset))
            .Select(localDate =>
            {
                var (dayStart, dayEnd) = IstanbulClock.GetUtcDay(localDate);
                var dayRows = rows.Where(row => row.PublishedAtUtc >= dayStart && row.PublishedAtUtc < dayEnd).ToList();
                var winner = Rank(dayRows, dayEnd).FirstOrDefault();
                return new LaunchDaySummary(localDate, dayRows.Count, winner);
            })
            .ToList();

        return Results.Ok(new LaunchArchiveResponse(firstDay, today, grouped));
    }

    private static async Task<IResult> GetUpcomingLaunches(
        int? days,
        int? limit,
        ProductDbContext db,
        HttpContext context)
    {
        var requestedDays = days ?? 30;
        var requestedLimit = limit ?? 100;
        if (requestedDays is < 1 or > 90 || requestedLimit is < 1 or > 100)
            return ApiProblemResults.BadRequest("Upcoming range or limit is invalid.", "launch.invalid_upcoming_range");

        var nowUtc = DateTime.UtcNow;
        var untilUtc = nowUtc.AddDays(requestedDays);
        var rows = await (
            from launch in db.ProductLaunches.AsNoTracking()
            join product in db.Products.AsNoTracking() on launch.ProductId equals product.Id
            where launch.Status == ProductLaunchStatus.Scheduled
                  && launch.ScheduledAtUtc >= nowUtc
                  && launch.ScheduledAtUtc < untilUtc
            orderby launch.ScheduledAtUtc, launch.Id
            select new UpcomingLaunchResponse(
                launch.Id,
                product.Id,
                product.Name,
                product.Slug,
                launch.VersionLabel,
                launch.Tagline,
                launch.ThumbnailUrl,
                launch.ScheduledAtUtc!.Value,
                product.Categories.OrderBy(category => category.SortOrder)
                    .Select(category => new CategoryResponse(category.Id, category.Name, category.Slug, category.ParentId))
                    .ToList()))
            .Take(requestedLimit)
            .ToListAsync(context.RequestAborted);

        return Results.Ok(new UpcomingLaunchesResponse(nowUtc, untilUtc, rows));
    }

    private static IQueryable<LaunchProjection> QueryPublishedLaunches(
        ProductDbContext db,
        DateTime startUtc,
        DateTime endUtc)
    {
        return
            from launch in db.ProductLaunches.AsNoTracking()
            join product in db.Products.AsNoTracking() on launch.ProductId equals product.Id
            where launch.Status == ProductLaunchStatus.Published
                  && launch.PublishedAtUtc >= startUtc
                  && launch.PublishedAtUtc < endUtc
            select new LaunchProjection(
                launch.Id,
                product.Id,
                product.Name,
                product.Slug,
                launch.SequenceNumber,
                launch.VersionLabel,
                launch.Tagline,
                launch.ThumbnailUrl,
                launch.PublishedAtUtc!.Value,
                launch.IsFeatured,
                product.Upvotes.Count(upvote => upvote.CreatedAt >= startUtc && upvote.CreatedAt < endUtc),
                product.CommentCount,
                product.ViewCount,
                product.Categories.OrderBy(category => category.SortOrder)
                    .Select(category => new CategoryResponse(category.Id, category.Name, category.Slug, category.ParentId))
                    .ToList());
    }

    private static IEnumerable<LaunchRankedResponse> Rank(IEnumerable<LaunchProjection> rows, DateTime anchorUtc)
    {
        return rows
            .Select(row => new
            {
                Row = row,
                Score = LaunchRankingService.Calculate(
                    new LaunchRankingSignals(row.Upvotes, row.Comments, row.Views, row.PublishedAtUtc, row.IsFeatured),
                    anchorUtc)
            })
            .OrderByDescending(item => item.Score.Total)
            .ThenByDescending(item => item.Row.Upvotes)
            .ThenBy(item => item.Row.PublishedAtUtc)
            .ThenBy(item => item.Row.LaunchId)
            .Select((item, index) => new LaunchRankedResponse(
                item.Row.LaunchId,
                item.Row.ProductId,
                item.Row.ProductName,
                item.Row.ProductSlug,
                item.Row.SequenceNumber,
                item.Row.VersionLabel,
                item.Row.Tagline,
                item.Row.ThumbnailUrl,
                item.Row.PublishedAtUtc,
                item.Row.IsFeatured,
                index + 1,
                item.Score.Total,
                item.Score.Engagement,
                item.Score.Discovery,
                item.Score.Freshness,
                item.Row.Upvotes,
                item.Row.Comments,
                item.Row.Views,
                item.Row.Categories));
    }
}

public sealed record LaunchProjection(
    Guid LaunchId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    int SequenceNumber,
    string VersionLabel,
    string Tagline,
    string ThumbnailUrl,
    DateTime PublishedAtUtc,
    bool IsFeatured,
    int Upvotes,
    int Comments,
    int Views,
    IReadOnlyList<CategoryResponse> Categories);

public sealed record LaunchRankedResponse(
    Guid LaunchId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    int SequenceNumber,
    string VersionLabel,
    string Tagline,
    string ThumbnailUrl,
    DateTime PublishedAtUtc,
    bool IsFeatured,
    int Rank,
    double Score,
    double EngagementScore,
    double DiscoveryScore,
    double FreshnessScore,
    int Upvotes,
    int Comments,
    int Views,
    IReadOnlyList<CategoryResponse> Categories);

public sealed record DailyLaunchesResponse(
    DateOnly Date,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsToday,
    IReadOnlyList<LaunchRankedResponse> Items);

public sealed record LaunchDaySummary(DateOnly Date, int LaunchCount, LaunchRankedResponse? Winner);
public sealed record LaunchArchiveResponse(DateOnly From, DateOnly To, IReadOnlyList<LaunchDaySummary> Days);

public sealed record UpcomingLaunchResponse(
    Guid LaunchId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string VersionLabel,
    string Tagline,
    string ThumbnailUrl,
    DateTime ScheduledAtUtc,
    IReadOnlyList<CategoryResponse> Categories);

public sealed record UpcomingLaunchesResponse(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<UpcomingLaunchResponse> Items);

internal static class IstanbulClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone));

    public static (DateTime StartUtc, DateTime EndUtc) GetUtcDay(DateOnly localDate)
    {
        var localStart = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        return (TimeZoneInfo.ConvertTimeToUtc(localStart, Zone), TimeZoneInfo.ConvertTimeToUtc(localEnd, Zone));
    }

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
