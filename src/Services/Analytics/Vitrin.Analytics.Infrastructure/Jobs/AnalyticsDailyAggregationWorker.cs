using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Analytics.Infrastructure.Data;

namespace Vitrin.Analytics.Infrastructure.Jobs;

/// <summary>
/// Her gece UTC 03:30'da bir önceki günün analitik event'lerini aggregate eder.
///
/// Batch Processing görevleri:
/// 1. Önceki güne ait ham event'lerden ürün bazlı view/upvote/comment sayılarını hesaplar.
/// 2. 90 günden eski işlenmiş event'leri temizler (SQLite disk tasarrufu).
///
/// Neden Batch?
/// - Ham event'ler gerçek zamanlı Kafka stream'ından gelir (Stream Processing ✅).
/// - Günlük aggregate hesabı; tüm gün bitmeden yapılmaz — batch iş.
/// - SQLite'ta unbounded büyüme önlenir.
///
/// Not: Hangfire veya cron tabanlı scheduler yoksa PeriodicTimer + UTC
/// zamanlama ile aynı işlev sağlanır.
/// </summary>
public sealed class AnalyticsDailyAggregationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalyticsDailyAggregationWorker> _logger;

    // Her 24 saatte bir kontrol eder; UTC 03:30'a denk gelecek şekilde bekler.
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    // Ham event'ler bu günden eskiyse temizlenir.
    private const int EventRetentionDays = 90;

    public AnalyticsDailyAggregationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AnalyticsDailyAggregationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsDailyAggregationWorker başlatıldı.");

        // İlk çalışma için UTC 03:30'a kadar bekle
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAggregationAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunAggregationAsync(CancellationToken ct)
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        _logger.LogInformation(
            "Günlük analitik aggregation başlıyor: {Date:yyyy-MM-dd}", yesterday);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();

            // ── 1. Önceki günün ürün bazlı metriklerini hesapla ──────────────
            var dayStart = yesterday;
            var dayEnd   = yesterday.AddDays(1);

            var summary = await db.AnalyticsEvents
                .AsNoTracking()
                .Where(e => e.CreatedAt >= dayStart && e.CreatedAt < dayEnd && e.ProductId.HasValue)
                .GroupBy(e => new { e.ProductId, e.EventType })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.EventType,
                    Count = g.Count()
                })
                .ToListAsync(ct);

            var productCount = summary.Select(s => s.ProductId).Distinct().Count();
            var totalEvents  = summary.Sum(s => s.Count);

            _logger.LogInformation(
                "Aggregation tamamlandı: {Date:yyyy-MM-dd} — {ProductCount} ürün, {TotalEvents} event.",
                yesterday, productCount, totalEvents);

            // ── 2. Eski event'leri temizle (retention politikası) ────────────
            var cutoff = DateTime.UtcNow.AddDays(-EventRetentionDays);
            var deleted = await db.AnalyticsEvents
                .Where(e => e.CreatedAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Analytics event temizliği: {Deleted} satır silindi ({RetentionDays} günden eski).",
                    deleted, EventRetentionDays);
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor, normal akış
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Günlük analitik aggregation sırasında hata oluştu.");
        }
    }

    /// <summary>Bir sonraki UTC 03:30'a kadar bekler (maksimum 24 saat).</summary>
    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now  = DateTime.UtcNow;
        var next = now.Date.AddHours(3).AddMinutes(30); // bugün 03:30
        if (next <= now) next = next.AddDays(1);        // geçtiyse yarın 03:30

        var delay = next - now;
        await Task.Delay(delay, ct);
    }
}
