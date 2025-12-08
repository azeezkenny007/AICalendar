using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record FeedbackRecordedEvent(
    UserFeedbackId FeedbackId,
    UserId UserId,
    PredictionId PredictionId,
    PredictionItemId PredictionItemId,
    FeedbackAction Action,
    bool WasEdited,
    DateTime OccurredOn
) : IDomainEvent;