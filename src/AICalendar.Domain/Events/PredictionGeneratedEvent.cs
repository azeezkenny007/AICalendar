using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

/// <summary>
/// Domain event raised when predictions are generated for a user.
/// This is informational and can be used for:
/// - Sending notifications to the user
/// - Analytics tracking
/// - Audit logging
/// </summary>
public record PredictionGeneratedEvent(
    PredictionId PredictionId,
    UserId UserId,
    PredictionCycle Cycle,
    int ItemCount,
    DateTime GeneratedAt
) : IDomainEvent;
