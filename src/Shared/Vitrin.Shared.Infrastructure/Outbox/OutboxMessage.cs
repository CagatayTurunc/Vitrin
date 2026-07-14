using System.Text.Json;
using Vitrin.Shared.Contracts.Events;

namespace Vitrin.Shared.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EventVersion { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? DeadLetteredAtUtc { get; private set; }

    public static OutboxMessage Create(IEvent @event, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var utcNow = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        return new OutboxMessage
        {
            Id = @event.EventId,
            EventType = @event.EventType,
            EventVersion = @event.Version,
            Topic = EventCatalog.GetTopic(@event),
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            CorrelationId = @event.CorrelationId,
            CausationId = @event.CausationId,
            OccurredAtUtc = DateTime.SpecifyKind(@event.Timestamp, DateTimeKind.Utc),
            CreatedAtUtc = utcNow,
            NextAttemptAtUtc = utcNow
        };
    }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        ProcessedAtUtc = DateTime.SpecifyKind(processedAtUtc, DateTimeKind.Utc);
        LastError = null;
    }

    public void MarkFailed(
        string error,
        DateTime failedAtUtc,
        int maxRetryAttempts,
        TimeSpan maxBackoff)
    {
        if (maxRetryAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts));
        }

        RetryCount++;
        LastError = error.Length <= 2_000 ? error : error[..2_000];

        var utcNow = DateTime.SpecifyKind(failedAtUtc, DateTimeKind.Utc);
        if (RetryCount >= maxRetryAttempts)
        {
            DeadLetteredAtUtc = utcNow;
            return;
        }

        var delaySeconds = Math.Min(Math.Pow(2, RetryCount), maxBackoff.TotalSeconds);
        NextAttemptAtUtc = utcNow.AddSeconds(delaySeconds);
    }
}
