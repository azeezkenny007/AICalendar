using AICalendar.Domain.Common;
using AICalendar.Domain.Events;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Aggregates.UserFeedbackAggregate;

/// <summary>
/// UserFeedback aggregate root - captures user acceptance/rejection patterns for AI training
/// </summary>
public class UserFeedback : AggregateRoot<UserFeedbackId>
{
    public UserId UserId { get; private set; } = default!;
    public PredictionId PredictionId { get; private set; } = default!;
    public PredictionItemId PredictionItemId { get; private set; } = default!;

    public FeedbackType Type { get; private set; }
    public FeedbackAction Action { get; private set; }

    // Prediction details at time of feedback
    public string Merchant { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }

    // For accepted items - track if user edited before accepting
    public bool WasEdited { get; private set; }
    public decimal? OriginalAmount { get; private set; }
    public DateTime? OriginalDueDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool SentToAI { get; private set; } 
    public DateTime? SentToAIAt { get; private set; }

    // EF Core
    private UserFeedback() { }

    private UserFeedback(
        UserId userId,
        PredictionId predictionId,
        PredictionItemId predictionItemId,
        FeedbackType type,
        FeedbackAction action,
        string merchant,
        decimal amount,
        DateTime dueDate,
        bool wasEdited = false,
        decimal? originalAmount = null,
        DateTime? originalDueDate = null)
    {
        Id = UserFeedbackId.Create();
        UserId = userId;
        PredictionId = predictionId;
        PredictionItemId = predictionItemId;
        Type = type;
        Action = action;
        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        WasEdited = wasEdited;
        OriginalAmount = originalAmount;
        OriginalDueDate = originalDueDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsSentToAI()
    {
        SentToAI = true;
        SentToAIAt = DateTime.UtcNow;
    }   
    /// <summary>
    /// Creates positive feedback when user accepts a prediction
    /// </summary>
    public static UserFeedback CreatePositive(
        UserId userId,
        PredictionId predictionId,
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        bool wasEdited,
        decimal? originalAmount,
        DateTime? originalDueDate)
    {
        var feedback = new UserFeedback(
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Positive,
            FeedbackAction.Accepted,
            merchant,
            amount,
            dueDate,
            wasEdited,
            originalAmount,
            originalDueDate
        );

        feedback.AddDomainEvent(new FeedbackRecordedEvent(
            feedback.Id,
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Positive,
            wasEdited,
            DateTime.UtcNow
        ));

        return feedback;
    }

    /// <summary>
    /// Creates negative feedback when user rejects a prediction
    /// </summary>
    public static UserFeedback CreateNegative(
        UserId userId,
        PredictionId predictionId,
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate)
    {
        var feedback = new UserFeedback(
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Negative,
            FeedbackAction.Rejected,
            merchant,
            amount,
            dueDate
        );

        feedback.AddDomainEvent(new FeedbackRecordedEvent(
            feedback.Id,
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Negative,
            false,
            DateTime.UtcNow
        ));

        return feedback;
    }
}