using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

/// <summary>
/// Domain event raised when a user rejects a prediction item.
/// This triggers:
/// - Recording of UserFeedback for AI training (negative feedback)
/// - Analytics tracking
/// </summary>
public record PredictionRejectedEvent(
    PredictionId PredictionId,
    PredictionItemId ItemId,
    UserId UserId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    DateTime RejectedAt
) : IDomainEvent;
