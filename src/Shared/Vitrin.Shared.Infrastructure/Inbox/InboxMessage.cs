namespace Vitrin.Shared.Infrastructure.Inbox;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }

    public static InboxMessage CreateProcessed(
        Guid eventId,
        string eventType,
        DateTime processedAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("An event id is required.", nameof(eventId));
        }

        var utcNow = DateTime.SpecifyKind(processedAtUtc, DateTimeKind.Utc);
        return new InboxMessage
        {
            Id = eventId,
            EventType = eventType,
            ReceivedAtUtc = utcNow,
            ProcessedAtUtc = utcNow
        };
    }
}
