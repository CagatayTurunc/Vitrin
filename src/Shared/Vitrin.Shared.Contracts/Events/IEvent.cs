using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Shared.Contracts.Events;

public interface IEvent : IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime Timestamp { get; }
    string Version { get; }
    Guid CorrelationId { get; }
    Guid? CausationId { get; }
}

public abstract class BaseEvent : IEvent
{
    public Guid EventId { get; init; }
    public string EventType { get; init; }
    public DateTime Timestamp { get; init; }
    public string Version { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    
    protected BaseEvent(string eventType, string version = "1.0")
    {
        EventId = Guid.NewGuid();
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
        Version = version;
        CorrelationId = Guid.NewGuid();
    }
}
