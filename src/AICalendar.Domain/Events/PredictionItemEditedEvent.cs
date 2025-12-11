using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

/// <summary>
/// Domain event raised when a user edits a prediction item before accepting it.
/// This is valuable for AI training to understand:
/// - Which predictions need correction
/// - How users adjust predictions
/// - Patterns in user edits
/// </summary>
public record PredictionItemEditedEvent(
    PredictionId PredictionId,
    PredictionItemId ItemId,
    UserId UserId,
    string Merchant,
    decimal OriginalAmount,
    decimal NewAmount,
    DateTime OriginalDueDate,
    DateTime NewDueDate,
    DateTime EditedAt
) : IDomainEvent;
