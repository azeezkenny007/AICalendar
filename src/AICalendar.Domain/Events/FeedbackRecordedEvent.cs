using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record FeedbackRecordedEvent(
    UserFeedbackId FeedbackId,
    UserId UserId,
    PredictionId PredictionId,
    PredictionItemId PredictionItemId,
    FeedbackType Type,
    bool WasEdited,
    DateTime OccurredOn
) : IDomainEvent;