using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

/// <summary>
/// Domain event raised when a user accepts a prediction item.
/// This triggers:
/// - Creation of a CalendarItem
/// - Recording of UserFeedback for AI training
/// </summary>
public record PredictionAcceptedEvent(
    PredictionId PredictionId,
    PredictionItemId ItemId,
    UserId UserId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    bool WasEdited,
    decimal? OriginalAmount,
    DateTime? OriginalDueDate,
    DateTime AcceptedAt
) : IDomainEvent;
