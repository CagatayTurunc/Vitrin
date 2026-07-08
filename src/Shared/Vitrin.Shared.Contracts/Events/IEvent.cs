using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Shared.Contracts.Events;

public interface IEvent : IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime Timestamp { get; }
    string Version { get; }
}

public abstract class BaseEvent : IEvent
{
    public Guid EventId { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }
    public string Version { get; }
    
    protected BaseEvent(string eventType, string version = "1.0")
    {
        EventId = Guid.NewGuid();
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
        Version = version;
    }
}
