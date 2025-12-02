using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarItemAddedEvent(
    CalendarId CalendarId,
    CalendarItemId CalendarItemId,
    UserId UserId,
    PredictionItemId PredictionItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    DateTime OccurredOn
) : IDomainEvent;
