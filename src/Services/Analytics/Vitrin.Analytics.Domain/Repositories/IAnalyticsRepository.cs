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
}

/// <summary>
/// En çok aranan terimler için projeksiyon.
/// </summary>
public record TopSearchTerm(string Query, int Count);
