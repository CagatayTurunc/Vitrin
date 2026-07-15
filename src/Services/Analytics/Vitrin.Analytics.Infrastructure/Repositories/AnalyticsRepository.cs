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

        // EventData JSON'dan Query alanını çek, grupla ve say
        // SQLite'da JSON fonksiyonları sınırlı olduğu için in-memory gruplama yapıyoruz
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
}
