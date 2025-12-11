using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarCreatedEvent(
    CalendarId CalendarId,
    UserId UserId,
    DateTime OccurredOn
) : IDomainEvent;
