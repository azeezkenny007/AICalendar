namespace AICalendar.Domain.Aggregates.UserFeedbackAggregate;

/// <summary>
/// Type of feedback from user
/// </summary>
public enum FeedbackType
{
    /// <summary>
    /// User accepted the prediction
    /// </summary>
    Positive = 1,

    /// <summary>
    /// User rejected the prediction
    /// </summary>
    Negative = 2
}