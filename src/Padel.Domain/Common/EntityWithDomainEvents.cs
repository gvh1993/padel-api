namespace Padel.Domain.Common;

public abstract class EntityWithDomainEvents : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected EntityWithDomainEvents(Guid id) : base(id)
    {
    }

    protected EntityWithDomainEvents()
    {
    }

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
