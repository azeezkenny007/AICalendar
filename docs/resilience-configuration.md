# AI Service Resilience Configuration Guide

## Overview

The AI Prediction Service now includes comprehensive resilience patterns with externalized configuration for easy tuning in different environments.

## Configuration Location

All resilience settings are configured in `appsettings.json` under `AIService:Resilience`:

```json
{
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
        "MaxPredictions": 5
      }
    }
  }
}
```

## Resilience Patterns Implemented

### 1. **Retry Policy** (Exponential Backoff + Jitter)
- **Purpose**: Automatically retry failed requests to handle transient failures
- **Configuration**:
  - `MaxRetryAttempts`: Number of retry attempts (default: 3)
  - `BaseDelaySeconds`: Base delay for exponential backoff (default: 2)
  - `MaxJitterMilliseconds`: Maximum random jitter to prevent thundering herd (default: 1000ms)
- **Behavior**:
  - Retry 1: ~2 seconds + random jitter
  - Retry 2: ~4 seconds + random jitter
  - Retry 3: ~8 seconds + random jitter

### 2. **Circuit Breaker** (Shared Singleton)
- **Purpose**: Prevent cascading failures by failing fast when service is down
- **Configuration**:
  - `FailureThreshold`: Number of consecutive failures before opening (default: 5)
  - `BreakDurationSeconds`: How long to keep circuit open (default: 30)
- **States**:
  - **Closed**: Normal operation, all requests go through
  - **Open**: After N failures, all requests fail immediately (no retries!)
  - **Half-Open**: After break duration, allows one test request
- **Key Feature**: **Singleton pattern** ensures circuit state is shared across ALL requests

### 3. **Timeout Policy**
- **Purpose**: Prevent requests from hanging indefinitely
- **Configuration**:
  - `TimeoutSeconds`: Maximum time to wait for response (default: 30)
- **Strategy**: Optimistic (respects CancellationToken)

### 4. **Fallback Policy** ⭐ NEW!
- **Purpose**: Provide graceful degradation when all else fails
- **Configuration**:
  - `Enabled`: Whether fallback is active (default: true)
  - `MaxPredictions`: Max predictions in fallback response (default: 5)
- **Behavior**: Returns empty prediction response with status "fallback"
- **Response Format**:
```json
{
  "status": "fallback",
  "message": "AI service unavailable. Please try again later.",
  "predictions": [],
  "metadata": {
    "total_predictions": 0,
    "confidence_score": 0.0,
    "model_version": "fallback",
    "processing_time_ms": 0
  }
}
```

## Policy Execution Order

Policies are wrapped in this order (executes inside-out):

```
Request
  ↓
[Fallback] ← Outermost: Catches all failures
  ↓
[Retry] ← Retries the entire pipeline below
  ↓
[Circuit Breaker] ← Counts each retry attempt (SHARED!)
  ↓
[Timeout] ← Innermost: Ensures request completes in time
  ↓
HTTP Request to AI Service
```

## Environment-Specific Configuration

### Development
```json
{
  "AIService": {
    "Resilience": {
      "Retry": {
        "MaxRetryAttempts": 2,
        "BaseDelaySeconds": 1
      },
      "CircuitBreaker": {
        "FailureThreshold": 3,
        "BreakDurationSeconds": 15
      },
      "Timeout": {
        "TimeoutSeconds": 10
      },
      "Fallback": {
        "Enabled": true
      }
    }
  }
}
```

### Production
```json
{
  "AIService": {
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
        "Enabled": true
      }
    }
  }
}
```

### High-Traffic Production
```json
{
  "AIService": {
    "Resilience": {
      "Retry": {
        "MaxRetryAttempts": 2,
        "BaseDelaySeconds": 1,
        "MaxJitterMilliseconds": 500
      },
      "CircuitBreaker": {
        "FailureThreshold": 10,
        "BreakDurationSeconds": 60
      },
      "Timeout": {
        "TimeoutSeconds": 20
      },
      "Fallback": {
        "Enabled": true
      }
    }
  }
}
```

## Monitoring & Observability

### Log Messages

**Retry**:
```
⚠️ [AI SERVICE RETRY] Attempt 1/3 after 2.15s. Status: 503, Exception: Service Unavailable
```

**Circuit Breaker**:
```
⚡ [CIRCUIT BREAKER OPEN] AI service circuit breaker opened after 5 failures. Break duration: 30s
✅ [CIRCUIT BREAKER CLOSED] AI service circuit breaker reset. Normal operation resumed.
🔄 [CIRCUIT BREAKER HALF-OPEN] AI service circuit breaker testing. Allowing one request...
```

**Timeout**:
```
⏱️ [TIMEOUT] AI service request timed out after 30s
```

**Fallback**:
```
🔄 [FALLBACK TRIGGERED] AI service call failed. Returning empty predictions. Exception: ...
```

## Performance Impact

### Without Circuit Breaker (4 users, service down)
- User 1: 15.60s (3 retries)
- User 2: 15.36s (3 retries)
- User 3: 16.55s (3 retries)
- User 4: 15.80s (3 retries)
- **Total: 63.36 seconds**

### With Circuit Breaker (4 users, service down)
- User 1: 15.56s (3 retries, circuit opens)
- User 2: 0.01s (circuit open, fails immediately)
- User 3: 0.01s (circuit open, fails immediately)
- User 4: 0.01s (circuit open, fails immediately)
- **Total: 15.63 seconds** ⚡ **75% faster!**

### With 1000 users (service down)
- **Without Circuit Breaker**: ~4.4 hours
- **With Circuit Breaker**: ~26 seconds
- **Improvement**: **99.8% faster!** 🚀

## Best Practices

1. **Tune for Your Environment**: Adjust thresholds based on traffic patterns
2. **Monitor Circuit State**: Track how often circuit opens in production
3. **Alert on Fallback**: Set up alerts when fallback is triggered frequently
4. **Test Resilience**: Regularly test with simulated failures (chaos engineering)
5. **Review Logs**: Analyze retry patterns to optimize configuration

## Disabling Fallback

If you want to let failures propagate instead of using fallback:

```json
{
  "AIService": {
    "Resilience": {
      "Fallback": {
        "Enabled": false
      }
    }
  }
}
```

## Code References

- **Configuration**: `src/AICalendar.Infrastructure/Services/ResilienceOptions.cs`
- **Policies**: `src/AICalendar.Infrastructure/Services/AIPredictionServicePolicies.cs`
- **Registration**: `src/AICalendar.API/Program.cs` (lines 96-107)
- **Settings**: `src/AICalendar.API/appsettings.json`

## Testing

To test resilience patterns:

1. **Stop AI Service**: Simulate service down
2. **Trigger Batch Job**: Run prediction generation
3. **Observe Logs**: Watch retry, circuit breaker, and fallback in action
4. **Check Database**: Verify failed attempts are tracked for retry

## Next Steps

1. ✅ Retry with exponential backoff + jitter
2. ✅ Circuit Breaker (shared singleton)
3. ✅ Timeout protection
4. ✅ Fallback policy
5. ✅ Externalized configuration
6. ⏳ Create retry job for failed users
7. ⏳ Add health checks for AI service
8. ⏳ Implement bulkhead isolation (optional)

---

**Last Updated**: 2025-12-05
**Version**: 1.0
