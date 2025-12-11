using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record PredictionBatchRejectedEvent(
    PredictionId PredictionId,
    UserId UserId,
    List<RejectedItemDetails> Items,
    DateTime OccurredOn
) : IDomainEvent;

public record RejectedItemDetails(
    PredictionItemId ItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate
);
