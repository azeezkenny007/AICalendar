using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarItemUpdatedEvent(
    CalendarId CalendarId,
    CalendarItemId CalendarItemId,
    UserId UserId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    string? Account,
    string? AccountName,
    string? Description,
    DateTime OccurredOn
) : IDomainEvent;
