using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace AICalendar.Infrastructure.Services;

/// <summary>
/// Polly resilience policies for AI Prediction Service
/// Implements Retry, Circuit Breaker, Timeout, and Fallback patterns
/// </summary>
public static class AIPredictionServicePolicies
{
    // Static circuit breaker shared across all requests
    // This ensures the circuit state is global, not per-request
    private static IAsyncPolicy<HttpResponseMessage>? _sharedCircuitBreaker;
    private static readonly object _lock = new object();

    /// <summary>
    /// Creates a retry policy with exponential backoff and jitter
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        ILogger logger,
        ResilienceOptions options)
    {
        var jitterer = new Random();

        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx, 408, network failures
            .Or<TimeoutRejectedException>() // Timeout from Polly timeout policy
            .WaitAndRetryAsync(
                retryCount: options.Retry.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff: baseDelay^retryAttempt seconds + jitter
                    var exponentialDelay = TimeSpan.FromSeconds(
                        Math.Pow(options.Retry.BaseDelaySeconds, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(
                        jitterer.Next(0, options.Retry.MaxJitterMilliseconds));
                    return exponentialDelay + jitter;
                },
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "N/A";
                    var exception = outcome.Exception?.Message ?? "N/A";

                    logger.LogWarning(
                        "⚠️ [AI SERVICE RETRY] Attempt {RetryAttempt}/{MaxRetries} after {Delay:F2}s. " +
                        "Status: {StatusCode}, Exception: {Exception}",
                        retryAttempt, options.Retry.MaxRetryAttempts, timespan.TotalSeconds,
                        statusCode, exception);
                });
    }

    /// <summary>
    /// Gets or creates the shared circuit breaker policy (SINGLETON)
    /// IMPORTANT: This is a singleton to share state across all requests
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        ILogger logger,
        ResilienceOptions options)
    {
        if (_sharedCircuitBreaker == null)
        {
            lock (_lock)
            {
                if (_sharedCircuitBreaker == null)
                {
                    _sharedCircuitBreaker = HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .Or<TimeoutRejectedException>()
                        .CircuitBreakerAsync(
                            handledEventsAllowedBeforeBreaking: options.CircuitBreaker.FailureThreshold,
                            durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
                            onBreak: (outcome, breakDelay) =>
                            {
                                logger.LogError(
                                    "⚡ [CIRCUIT BREAKER OPEN] AI service circuit breaker opened after {Threshold} failures. " +
                                    "Break duration: {BreakDelay}s. All requests will fail fast until circuit closes.",
                                    options.CircuitBreaker.FailureThreshold, breakDelay.TotalSeconds);
                            },
                            onReset: () =>
                            {
                                logger.LogInformation(
                                    "✅ [CIRCUIT BREAKER CLOSED] AI service circuit breaker reset. " +
                                    "Normal operation resumed.");
                            },
                            onHalfOpen: () =>
                            {
                                logger.LogInformation(
                                    "🔄 [CIRCUIT BREAKER HALF-OPEN] AI service circuit breaker testing. " +
                                    "Allowing one request to check if service is back.");
                            });
                }
            }
        }

        return _sharedCircuitBreaker;
    }

    /// <summary>
    /// Creates a timeout policy
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(
        ILogger logger,
        ResilienceOptions options)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds),
            timeoutStrategy: TimeoutStrategy.Optimistic, // HttpClient respects CancellationToken
            onTimeoutAsync: (context, timespan, task) =>
            {
                logger.LogWarning(
                    "⏱️ [TIMEOUT] AI service request timed out after {Timeout}s",
                    timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Creates a fallback policy that returns an empty prediction response
    /// Note: Fallback wraps the entire pipeline and catches all failures
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(
        ILogger logger,
        ResilienceOptions options)
    {
        // Create a static fallback response to avoid creating new instances
        var fallbackResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = "fallback",
                    message = "AI service unavailable. Please try again later.",
                    predictions = Array.Empty<object>(),
                    metadata = new
                    {
                        total_predictions = 0,
                        confidence_score = 0.0,
                        model_version = "fallback",
                        processing_time_ms = 0
                    }
                }),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<BrokenCircuitException>()
            .Or<TimeoutRejectedException>()
            .FallbackAsync(
                fallbackValue: fallbackResponse,
                onFallbackAsync: (outcome, context) =>
                {
                    logger.LogWarning(
                        "🔄 [FALLBACK TRIGGERED] AI service call failed. Returning empty predictions. " +
                        "Exception: {Exception}, Status: {Status}",
                        outcome.Exception?.Message ?? "N/A",
                        outcome.Result?.StatusCode.ToString() ?? "N/A");
                    return Task.CompletedTask;
                });
    }

    /// <summary>
    /// Creates the complete resilience pipeline
    /// Order: Fallback → Retry → Circuit Breaker → Timeout (inside-out execution)
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetResiliencePipeline(
        ILogger logger,
        ResilienceOptions options)
    {
        var timeout = GetTimeoutPolicy(logger, options);
        var circuitBreaker = GetCircuitBreakerPolicy(logger, options);
        var retry = GetRetryPolicy(logger, options);

        // Wrap policies in correct order (executes inside-out)
        var pipeline = Policy.WrapAsync(
            retry,           // Outermost (after fallback): Retries the whole pipeline
            circuitBreaker,  // Middle: Counts each retry attempt, opens after N failures (SHARED!)
            timeout          // Innermost: Ensures operations complete in time
        );

        // Optionally wrap with fallback if enabled
        if (options.Fallback.Enabled)
        {
            var fallback = GetFallbackPolicy(logger, options);
            return Policy.WrapAsync(
                fallback,    // Outermost: Catches all failures and returns default response
                pipeline     // Inner: Retry → Circuit Breaker → Timeout
            );
        }

        return pipeline;
    }
}
