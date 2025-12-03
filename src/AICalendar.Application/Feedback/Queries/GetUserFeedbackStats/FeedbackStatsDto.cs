namespace AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;

public record FeedbackStatsDto(
    Guid UserId,
    int TotalFeedbacks,
    int PositiveFeedbacks,
    int NegativeFeedbacks,
    int EditedBeforeAccepted,
    decimal AcceptanceRate,
    decimal EditRate
);