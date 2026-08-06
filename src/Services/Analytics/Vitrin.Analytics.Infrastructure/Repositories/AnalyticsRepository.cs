using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vitrin.Analytics.Domain.Entities;
using Vitrin.Analytics.Domain.Repositories;
using Vitrin.Analytics.Infrastructure.Data;

namespace Vitrin.Analytics.Infrastructure.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AnalyticsDbContext _context;

    public AnalyticsRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        await _context.AnalyticsEvents.AddAsync(analyticsEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountByEventTypeAsync(
        string eventType,
        Guid? productId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AnalyticsEvents
            .AsNoTracking()
            .Where(a => a.EventType == eventType);

        if (productId.HasValue)
            query = query.Where(a => a.ProductId == productId.Value);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnalyticsEvent>> GetRecentAsync(
        string? eventType = null,
        Guid? productId = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AnalyticsEvents.AsNoTracking();

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(a => a.EventType == eventType);

        if (productId.HasValue)
            query = query.Where(a => a.ProductId == productId.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopSearchTerm>> GetTopSearchTermsAsync(
        int limit = 10,
        DateTime? from = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AnalyticsEvents
            .AsNoTracking()
            .Where(a => a.EventType == "Search");

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        var rawEvents = await query
            .Select(a => a.EventData)
            .ToListAsync(cancellationToken);

        var topTerms = rawEvents
            .Select(data =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    return doc.RootElement.TryGetProperty("Query", out var q)
                        ? q.GetString()
                        : null;
                }
                catch { return null; }
            })
            .Where(q => !string.IsNullOrEmpty(q))
            .GroupBy(q => q!)
            .OrderByDescending(g => g.Count())
            .Take(Math.Clamp(limit, 1, 100))
            .Select(g => new TopSearchTerm(g.Key, g.Count()))
            .ToList();

        return topTerms;
    }

    public async Task<IReadOnlyList<DailyMetricPoint>> GetDailyTimeSeriesAsync(
        Guid productId,
        string eventType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.AnalyticsEvents
            .AsNoTracking()
            .Where(a => a.ProductId == productId
                     && a.EventType == eventType
                     && a.CreatedAt >= from
                     && a.CreatedAt <= to)
            .Select(a => a.CreatedAt.Date)
            .ToListAsync(cancellationToken);

        // Fill missing days with 0
        var grouped = events
            .GroupBy(d => d)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<DailyMetricPoint>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            result.Add(new DailyMetricPoint(
                DateOnly.FromDateTime(d),
                grouped.TryGetValue(d, out var count) ? count : 0));
        }
        return result;
    }

    public async Task<IReadOnlyList<ReferrerStat>> GetReferrerStatsAsync(
        Guid productId,
        DateTime from,
        CancellationToken cancellationToken = default)
    {
        var rawEvents = await _context.AnalyticsEvents
            .AsNoTracking()
            .Where(a => a.ProductId == productId
                     && a.EventType == "ProductView"
                     && a.CreatedAt >= from)
            .Select(a => a.EventData)
            .ToListAsync(cancellationToken);

        var referrers = rawEvents
            .Select(data =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("Referrer", out var r))
                    {
                        var raw = r.GetString();
                        if (string.IsNullOrWhiteSpace(raw)) return "Direkt";
                        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
                            return uri.Host.ToLowerInvariant().TrimStart("www.".ToCharArray());
                        return "Diğer";
                    }
                    return "Direkt";
                }
                catch { return "Direkt"; }
            })
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .ToList();

        var total = referrers.Sum(g => g.Count());
        return referrers
            .Take(20)
            .Select(g => new ReferrerStat(
                g.Key,
                g.Count(),
                total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductAggregateStat>> GetMakerProductStatsAsync(
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0) return [];

        var grouped = await _context.AnalyticsEvents
            .AsNoTracking()
            .Where(a => a.ProductId.HasValue && productIds.Contains(a.ProductId!.Value)
                     && (a.EventType == "ProductView"
                      || a.EventType == "ProductUpvote"
                      || a.EventType == "Comment"))
            .GroupBy(a => new { a.ProductId, a.EventType })
            .Select(g => new { g.Key.ProductId, g.Key.EventType, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return productIds.Select(pid =>
        {
            var views    = grouped.FirstOrDefault(g => g.ProductId == pid && g.EventType == "ProductView")?.Count    ?? 0;
            var upvotes  = grouped.FirstOrDefault(g => g.ProductId == pid && g.EventType == "ProductUpvote")?.Count  ?? 0;
            var comments = grouped.FirstOrDefault(g => g.ProductId == pid && g.EventType == "Comment")?.Count        ?? 0;
            var conversion = views > 0 ? Math.Round((double)upvotes / views * 100, 2) : 0;
            return new ProductAggregateStat(pid, views, upvotes, comments, conversion);
        }).ToList();
    }

    public async Task<RetentionStats> GetRetentionStatsAsync(
        Guid productId,
        DateTime from,
        CancellationToken cancellationToken = default)
    {
        var viewEvents = await _context.AnalyticsEvents
            .AsNoTracking()
            .Where(a => a.ProductId == productId
                     && a.EventType == "ProductView"
                     && a.CreatedAt >= from
                     && a.UserId.HasValue)
            .Select(a => a.UserId!.Value)
            .ToListAsync(cancellationToken);

        var totalViews   = await _context.AnalyticsEvents.AsNoTracking()
            .CountAsync(a => a.ProductId == productId && a.EventType == "ProductView" && a.CreatedAt >= from, cancellationToken);

        var uniqueViewers  = viewEvents.Distinct().Count();
        var returnViewers  = viewEvents.GroupBy(id => id).Count(g => g.Count() > 1);
        var retentionRate  = uniqueViewers > 0 ? Math.Round((double)returnViewers / uniqueViewers * 100, 1) : 0;

        return new RetentionStats(totalViews, uniqueViewers, returnViewers, retentionRate);
    }
}
