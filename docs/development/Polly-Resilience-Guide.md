# Polly Resilience Guide for AICalendar

This document explains **how Polly is used in this codebase** to make calls to the AI Prediction Service resilient and configurable.

## 1. High-Level Overview

Polly is used to protect outbound HTTP calls from the API to the **AI Prediction Service**:

- Applies **Timeout**, **Circuit Breaker**, **Retry (with exponential backoff + jitter)**, and **Fallback**
- All behavior is driven by configuration under `AIService:Resilience` in `appsettings.json`
- Policies are composed into a single pipeline and attached to the `HttpClient` used by `IAIPredictionService`

Key goals:

- Avoid long hangs when the AI service is slow or offline
- Prevent cascading failures with a shared circuit breaker
- Provide predictable fallback responses when everything else fails

---

## 2. Where Polly Is Configured

### 2.1. NuGet Package

`src/AICalendar.Infrastructure/AICalendar.Infrastructure.csproj`:

```xml
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

This brings Polly and the HTTP-specific helpers (`HttpPolicyExtensions`).

### 2.2. Options Binding

`src/AICalendar.Infrastructure/Resilience/ResilienceOptions.cs` defines strongly typed options:

```csharp
public class ResilienceOptions
{
    public const string SectionName = "AIService:Resilience";

    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
    public FallbackOptions Fallback { get; set; } = new();
}
```

These are bound in `Program.cs`:

```csharp
// Program.cs
builder.Services.Configure<AICalendar.Infrastructure.Resilience.ResilienceOptions>(
    builder.Configuration.GetSection(AICalendar.Infrastructure.Resilience.ResilienceOptions.SectionName));
```

### 2.3. Policy Registration for the AI HttpClient

`src/AICalendar.API/Program.cs` wires Polly into the `HttpClient` used by the AI service:\r

```csharp
// Register Application Services with Polly Resilience Policies
builder.Services.AddHttpClient<AICalendar.Application.Services.IAIPredictionService, AIPredictionService>()
    .AddPolicyHandler((services, request) =>
    {
        var logger = services.GetRequiredService<ILogger<AIPredictionService>>();
        var options = services.GetRequiredService<IOptions<ResilienceOptions>>().Value;
        return AIPredictionServicePolicies.GetResiliencePipeline(logger, options);
    });
```

Every outgoing HTTP call from `AIPredictionService` is executed through this resilience pipeline.

---

## 3. appsettings.json Configuration

`src/AICalendar.API/appsettings.json`:

```json
"AIService": {
  "Url": "http://localhost:7071/api",
  "Key": "YOUR_FUNCTION_KEY_HERE",
  "Resilience": {
    "Retry": {
      "MaxRetryAttempts": 3,
      "BaseDelaySeconds": 2,
      "MaxJitterMilliseconds": 1000
    },
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "BreakDurationSeconds": 30
    },
    "Timeout": {
      "TimeoutSeconds": 30
    },
    "Fallback": {
      "Enabled": true,
      "MaxPredictions": 5,
      "RetryJobDelayMinutes": 1
    }
  }
}
```

You can override these values per environment (e.g., `appsettings.Development.json`, production settings, or environment variables).

For a deeper configuration-focused explanation, see `docs/resilience-configuration.md`.

---

## 4. Policy Implementation (`AIPredictionServicePolicies`)

Main implementation:  
`src/AICalendar.Infrastructure/Resilience/AIPredictionServicePolicies.cs`

This class builds all policies and composes them into a single pipeline.

### 4.1. Timeout Policy

```csharp
public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(
    ILogger logger,
    ResilienceOptions options)
{
    return Policy.TimeoutAsync<HttpResponseMessage>(
        timeout: TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds),
        timeoutStrategy: TimeoutStrategy.Optimistic,
        onTimeoutAsync: (context, timespan, task) =>
        {
            logger.LogWarning(
                "⏱️ [TIMEOUT] AI service request timed out after {Timeout}s",
                timespan.TotalSeconds);
            return Task.CompletedTask;
        });
}
```

- Cancels AI HTTP calls that exceed `TimeoutSeconds`
- Uses **optimistic** timeouts (respects `CancellationToken` from `HttpClient`)

### 4.2. Shared Circuit Breaker (Singleton)

```csharp
private static IAsyncPolicy<HttpResponseMessage>? _sharedCircuitBreaker;
private static readonly object _lock = new object();

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
                        onBreak: (outcome, breakDelay) => { ... },
                        onReset: () => { ... },
                        onHalfOpen: () => { ... });
            }
        }
    }

    return _sharedCircuitBreaker;
}
```

- Uses `HttpPolicyExtensions.HandleTransientHttpError()` to treat 5xx, 408, and network issues as failures
- **Singleton** instance shared by all requests, so once the circuit is open, all callers fail fast
- Logs when circuit opens, closes, and moves to half-open

### 4.3. Retry with Exponential Backoff + Jitter

```csharp
public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
    ILogger logger,
    ResilienceOptions options)
{
    var jitterer = new Random();

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutRejectedException>()
        .WaitAndRetryAsync(
            retryCount: options.Retry.MaxRetryAttempts,
            sleepDurationProvider: retryAttempt =>
            {
                var exponentialDelay = TimeSpan.FromSeconds(
                    Math.Pow(options.Retry.BaseDelaySeconds, retryAttempt));
                var jitter = TimeSpan.FromMilliseconds(
                    jitterer.Next(0, options.Retry.MaxJitterMilliseconds));
                return exponentialDelay + jitter;
            },
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                logger.LogWarning(
                    "⚠️ [AI SERVICE RETRY] Attempt {RetryAttempt}/{MaxRetries} after {Delay:F2}s. Status: {StatusCode}, Exception: {Exception}",
                    retryAttempt, options.Retry.MaxRetryAttempts, timespan.TotalSeconds,
                    outcome.Result?.StatusCode.ToString() ?? "N/A",
                    outcome.Exception?.Message ?? "N/A");
            });
}
```

- Retries transient failures and timeouts
- Backoff grows exponentially (`BaseDelaySeconds^attempt + jitter`)
- Logs each attempt with delay, status code, and exception message

### 4.4. Fallback Policy

```csharp
public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(
    ILogger logger,
    ResilienceOptions options)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<BrokenCircuitException>()
        .Or<TimeoutRejectedException>()
        .FallbackAsync(
            fallbackAction: (outcome, context, cancellationToken) =>
            {
                var realReason = outcome.Exception?.Message
                    ?? (outcome.Result != null
                        ? $"HTTP {outcome.Result.StatusCode}"
                        : "Unknown Error");

                var fallbackResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            status = "fallback",
                            error_message = $"AI service unavailable: {realReason}",
                            predictions = Array.Empty<object>(),
                            metadata = new
                            {
                                total_predictions = 0,
                                confidence_score = 0.0,
                                model_version = "fallback",
                                processing_time_ms = 0
                            }
                        }),
                        Encoding.UTF8,
                        "application/json")
                };

                return Task.FromResult(fallbackResponse);
            },
            onFallbackAsync: (outcome, context) =>
            {
                logger.LogWarning(
                    "🔄 [FALLBACK TRIGGERED] AI service call failed. Returning empty predictions. Exception: {Exception}, Status: {Status}",
                    outcome.Exception?.Message ?? "N/A",
                    outcome.Result?.StatusCode.ToString() ?? "N/A");
                return Task.CompletedTask;
            });
}
```

- Catches failures from retries, timeouts, and broken circuits
- Returns a **synthetic success (HTTP 200)** with a payload that clearly indicates `status = "fallback"`
- This allows the rest of the pipeline to keep working while still recording the underlying error in `error_message`

### 4.5. Policy Composition Order

The complete pipeline is composed in `GetResiliencePipeline`:

```csharp
public static IAsyncPolicy<HttpResponseMessage> GetResiliencePipeline(
    ILogger logger,
    ResilienceOptions options)
{
    var timeout = GetTimeoutPolicy(logger, options);
    var circuitBreaker = GetCircuitBreakerPolicy(logger, options);
    var retry = GetRetryPolicy(logger, options);

    var pipeline = Policy.WrapAsync(
        retry,          // outer
        circuitBreaker, // middle
        timeout         // inner
    );

    if (options.Fallback.Enabled)
    {
        var fallback = GetFallbackPolicy(logger, options);
        return Policy.WrapAsync(
            fallback,   // outermost
            pipeline
        );
    }

    return pipeline;
}
```

Execution order (inside → outside):

1. **Timeout** – ensures each HTTP call completes within `TimeoutSeconds`
2. **Circuit Breaker** – counts failures and opens the circuit when the threshold is reached
3. **Retry** – retries the whole inner pipeline on transient errors
4. **Fallback** (optional) – catches final failures and returns a safe default response

This order is also summarized visually in `docs/resilience-configuration.md`.

---

## 5. How to Tune Polly for Different Environments

You can override any `AIService:Resilience` setting per environment.

Examples (from `docs/resilience-configuration.md`):

- **Development** – shorter timeouts, fewer retries to fail fast
- **Production** – higher retry counts and longer timeouts when appropriate
- **High-Traffic Production** – fewer retries and adjusted circuit breaker thresholds to protect throughput

Use environment-specific configuration files or environment variables, for example:

```bash
ASPNETCORE_ENVIRONMENT=Production
AIService__Resilience__Retry__MaxRetryAttempts=2
AIService__Resilience__Timeout__TimeoutSeconds=20
```

---

## 6. How to Detect Polly Behavior in Logs

Polly emits clear, structured log messages:

- **Retry**: `⚠️ [AI SERVICE RETRY] Attempt X/Y after Zs...`
- **Circuit Breaker Open**: `⚡ [CIRCUIT BREAKER OPEN] AI service circuit breaker opened...`
- **Timeout**: `⏱️ [TIMEOUT] AI service request timed out after ...`
- **Fallback**: `🔄 [FALLBACK TRIGGERED] AI service call failed. Returning empty predictions...`

You can view these logs in:

- Console output during development
- Seq (see `docs/deployment/Seq-Setup-Guide.md`) in the `AICalendar.API` stream

---

## 7. Extending Polly Usage

If you need similar resilience for other outbound HTTP calls:

1. Add or reuse an options section under `appsettings.json`
2. Create a policy builder similar to `AIPredictionServicePolicies`
3. Register the new `HttpClient` using `.AddHttpClient<...>()` and `.AddPolicyHandler(...)`

You can also:

- Add **bulkhead isolation** for concurrency limits
- Add **fallbacks** that enqueue background jobs when real-time calls fail

---

## 8. Related Files

- `src/AICalendar.Infrastructure/Resilience/ResilienceOptions.cs` – configuration model
- `src/AICalendar.Infrastructure/Resilience/AIPredictionServicePolicies.cs` – Polly policies and pipeline
- `src/AICalendar.API/Program.cs` – registration of the resilient `HttpClient`
- `src/AICalendar.API/appsettings.json` – `AIService:Resilience` configuration
- `docs/resilience-configuration.md` – configuration-focused explanation and examples
