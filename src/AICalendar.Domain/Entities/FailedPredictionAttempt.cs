using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Entities;

/// <summary>
/// Tracks failed prediction generation attempts for retry processing
/// </summary>
public class FailedPredictionAttempt : Entity<FailedPredictionAttemptId>
{
    public UserId UserId { get; private set; }
    public string TargetMonth { get; private set; }
    public DateTime FailedAt { get; private set; }
    public string ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastRetryAt { get; private set; }
    public DateTime? SucceededAt { get; private set; }
    public bool IsResolved { get; private set; }

    private FailedPredictionAttempt(
        FailedPredictionAttemptId id,
        UserId userId,
        string targetMonth,
        string errorMessage) : base(id)
    {
        UserId = userId;
        TargetMonth = targetMonth;
        ErrorMessage = errorMessage;
        FailedAt = DateTime.UtcNow;
        RetryCount = 0;
        IsResolved = false;
    }

    public static FailedPredictionAttempt Create(
        UserId userId,
        string targetMonth,
        string errorMessage)
    {
        return new FailedPredictionAttempt(
            FailedPredictionAttemptId.Create(),
            userId,
            targetMonth,
            errorMessage);
    }

    public void IncrementRetryCount()
    {
        RetryCount++;
        LastRetryAt = DateTime.UtcNow;
    }

    public void UpdateError(string errorMessage)
    {
        ErrorMessage = errorMessage;
        FailedAt = DateTime.UtcNow;
    }

    public void MarkAsResolved()
    {
        IsResolved = true;
        SucceededAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Value object for FailedPredictionAttempt ID
/// </summary>
public record FailedPredictionAttemptId(Guid Value)
{
    public static FailedPredictionAttemptId Create() => new(Guid.NewGuid());
    public static FailedPredictionAttemptId From(Guid value) => new(value);
}
