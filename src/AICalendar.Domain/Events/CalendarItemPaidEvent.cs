using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarItemPaidEvent(
    CalendarId CalendarId,
    CalendarItemId CalendarItemId,
    UserId UserId,
    DateTime PaidDate,
    DateTime OccurredOn
) : IDomainEvent;
