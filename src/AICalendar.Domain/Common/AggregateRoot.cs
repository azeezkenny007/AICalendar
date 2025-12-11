using AICalendar.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace AICalendar.Domain.Common;

public abstract class AggregateRoot<TId> : IAggregateRoot where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    [NotMapped]
    private readonly List<IDomainEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
