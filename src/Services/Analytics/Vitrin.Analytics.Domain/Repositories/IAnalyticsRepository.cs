using Vitrin.Analytics.Domain.Entities;

namespace Vitrin.Analytics.Domain.Repositories;

/// <summary>
/// Analytics event'lerini kalıcı depolama katmanına yazan/okuyan interface.
/// </summary>
public interface IAnalyticsRepository
{
    Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);

    Task<int> CountByEventTypeAsync(
        string eventType,
        Guid? productId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalyticsEvent>> GetRecentAsync(
        string? eventType = null,
        Guid? productId = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopSearchTerm>> GetTopSearchTermsAsync(
        int limit = 10,
        DateTime? from = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir ürünün günlük view/upvote/comment trend verisi.
    /// </summary>
    Task<IReadOnlyList<DailyMetricPoint>> GetDailyTimeSeriesAsync(
        Guid productId,
        string eventType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir ürünün referrer kaynaklarını gruplandırılmış olarak döner.
    /// </summary>
    Task<IReadOnlyList<ReferrerStat>> GetReferrerStatsAsync(
        Guid productId,
        DateTime from,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir maker'ın tüm ürünleri için aggregate analytics özeti.
    /// </summary>
    Task<IReadOnlyList<ProductAggregateStat>> GetMakerProductStatsAsync(
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tekrar ziyaretçi oranı: aynı userId'nin birden fazla view bıraktığı oran.
    /// </summary>
    Task<RetentionStats> GetRetentionStatsAsync(
        Guid productId,
        DateTime from,
        CancellationToken cancellationToken = default);
}

/// <summary>En çok aranan terimler için projeksiyon.</summary>
public record TopSearchTerm(string Query, int Count);

/// <summary>Günlük metrik noktası (time-series chart için).</summary>
public record DailyMetricPoint(DateOnly Date, int Count);

/// <summary>Referrer kaynağı istatistiği.</summary>
public record ReferrerStat(string Source, int Count, double Percentage);

/// <summary>Tek ürün için aggregate istatistik (maker dashboard özet kartı).</summary>
public record ProductAggregateStat(
    Guid ProductId,
    int TotalViews,
    int TotalUpvotes,
    int TotalComments,
    double ConversionRate);

/// <summary>Retention istatistikleri.</summary>
public record RetentionStats(
    int TotalViews,
    int UniqueViewers,
    int ReturnViewers,
    double RetentionRate);
