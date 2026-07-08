using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Analytics.Domain.Entities;

public class AnalyticsEvent : AggregateRoot
{
    public Guid? ProductId { get; private set; }
    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = string.Empty; // e.g., "ProductView", "ProductClick", "Search"
    public string EventData { get; private set; } = string.Empty; // JSON data
    public DateTime CreatedAt { get; private set; }

    private AnalyticsEvent() { } // EF Core

    public static Result<AnalyticsEvent> Create(string eventType, string eventData, Guid? productId = null, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return Result<AnalyticsEvent>.Failure("EventType cannot be empty.");

        var analyticsEvent = new AnalyticsEvent
        {
            EventType = eventType,
            EventData = eventData,
            ProductId = productId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        return Result<AnalyticsEvent>.Success(analyticsEvent);
    }
}
