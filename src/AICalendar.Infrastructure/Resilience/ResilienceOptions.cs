namespace AICalendar.Infrastructure.Resilience;

/// <summary>
/// Configuration options for AI service resilience policies
/// </summary>
public class ResilienceOptions
{
    public const string SectionName = "AIService:Resilience";

    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
    public FallbackOptions Fallback { get; set; } = new();
}

/// <summary>
/// Retry policy configuration
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in seconds for exponential backoff (default: 2)
    /// </summary>
    public int BaseDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Maximum random jitter in milliseconds (default: 1000)
    /// </summary>
    public int MaxJitterMilliseconds { get; set; } = 1000;
}

/// <summary>
/// Circuit breaker policy configuration
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of consecutive failures before opening circuit (default: 5)
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration in seconds to keep circuit open (default: 30)
    /// </summary>
    public int BreakDurationSeconds { get; set; } = 30;
}

/// <summary>
/// Timeout policy configuration
/// </summary>
public class TimeoutOptions
{
    /// <summary>
    /// Timeout in seconds for AI service requests (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Fallback policy configuration
/// </summary>
public class FallbackOptions
{
    /// <summary>
    /// Whether fallback is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of predictions to return in fallback response (default: 5)
    /// </summary>
    public int MaxPredictions { get; set; } = 5;

    /// <summary>
    /// Delay in minutes before scheduling the retry job (default: 60)
    /// </summary>
    public int RetryJobDelayMinutes { get; set; } = 60;
}
