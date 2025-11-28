namespace AICalendar.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime OccurredOn { get; }
}
