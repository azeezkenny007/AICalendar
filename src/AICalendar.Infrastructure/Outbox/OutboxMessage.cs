namespace AICalendar.Infrastructure.Outbox;

/// <summary>
/// Represents a domain event stored in the outbox table for reliable processing
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>
    /// The type of the domain event (e.g., "PredictionAcceptedEvent")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized content of the domain event
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the domain event occurred
    /// </summary>
    public DateTime OccurredOnUtc { get; set; }

    /// <summary>
    /// When the event was successfully processed (null if not yet processed)
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Number of times processing has been attempted
    /// </summary>
    public int RetryCount { get; set; }
}
