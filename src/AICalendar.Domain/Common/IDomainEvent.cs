namespace AICalendar.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// They are published via the Outbox Pattern for reliable processing.
/// </summary>
public interface IDomainEvent
{
}
