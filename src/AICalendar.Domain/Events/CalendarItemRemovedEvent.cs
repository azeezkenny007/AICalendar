using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarItemRemovedEvent(
    CalendarId CalendarId,
    CalendarItemId CalendarItemId,
    UserId UserId,
    DateTime OccurredOn
) : IDomainEvent;
