namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public record CalendarDto(
    Guid CalendarId,
    Guid UserId,
    List<CalendarItemDto> Items,
    DateTime CreatedAt
);

public record CalendarItemDto(
    Guid ItemId,
    Guid PredictionItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    string? Account,
    string? AccountName,
    string? Description,
    bool IsPaid,
    DateTime? PaidDate,
    DateTime CreatedAt
);
