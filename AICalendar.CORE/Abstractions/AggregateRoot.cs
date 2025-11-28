using System.ComponentModel.DataAnnotations.Schema;

namespace AICalendar.CORE.Abstractions;

public abstract class AggregateRoot<TId> where TId : notnull
{
    [NotMapped]
    private readonly List<IDomainEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
