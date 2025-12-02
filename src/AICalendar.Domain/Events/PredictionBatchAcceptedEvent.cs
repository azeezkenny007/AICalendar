using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record PredictionBatchAcceptedEvent(
    PredictionId PredictionId,
    UserId UserId,
    List<AcceptedItemDetails> Items,
    DateTime OccurredOn
) : IDomainEvent;

public record AcceptedItemDetails(
    PredictionItemId ItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    bool IsEdited,
    decimal OriginalAmount,
    DateTime OriginalDueDate
);
