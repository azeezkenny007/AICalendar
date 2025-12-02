using AICalendar.Domain.Enums;

namespace AICalendar.Application.Predictions.Queries.GetPrediction;

public record PredictionDto(
    Guid Id,
    Guid UserId,
    string Status,
    DateTime CreatedAt,
    List<PredictionItemDto> Items
);

public record PredictionItemDto(
    Guid Id,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    string Explanation,
    double Confidence,
    string Pattern,
    bool? IsAccepted,
    bool IsEdited
);
