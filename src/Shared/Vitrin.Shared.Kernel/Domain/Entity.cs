namespace Vitrin.Shared.Kernel.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    protected Entity()
    {
        Id = Guid.NewGuid();
    }
    
    protected Entity(Guid id)
    {
        Id = id;
    }
}

public abstract class AggregateRoot : Entity
{
    protected AggregateRoot() : base() { }
    protected AggregateRoot(Guid id) : base(id) { }
}
