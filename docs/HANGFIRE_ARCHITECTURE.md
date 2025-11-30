# Hangfire Architecture

## Overview
This document describes the Hangfire background job processing architecture implemented in the AICalendar application.

## Architecture Pattern

The implementation follows a **clean, modular architecture** inspired by enterprise-grade patterns:

### 1. **Service Layer** (`IHangfireService`)
- **Location**: `AICalendar.Application.Common.Interfaces.IHangfireService`
- **Purpose**: Provides a high-level abstraction for scheduling and managing background jobs
- **Methods**:
  - `ScheduleJobAsync<T>()` - Schedule a one-time job with a delay
  - `ScheduleRecurringJobAsync<T>()` - Schedule a recurring job with cron expression
  - `DeleteJobAsync()` - Delete a scheduled job
  - `TriggerJobAsync<T>()` - Trigger an immediate job execution

### 2. **Implementation** (`HangfireService`)
- **Location**: `AICalendar.Infrastructure.BackgroundJobs.HangfireService`
- **Purpose**: Concrete implementation using Hangfire's `IBackgroundJobClient` and `IRecurringJobManager`
- **Features**:
  - Comprehensive logging for all operations
  - Exception handling with proper error logging
  - Type-safe job scheduling using expressions

### 3. **Configuration** (`HangfireConfiguration`)
- **Location**: `AICalendar.Infrastructure.BackgroundJobs.HangfireConfiguration`
- **Purpose**: Centralized configuration for recurring jobs
- **Features**:
  - Reads cron expressions from `appsettings.json`
  - Supports multiple recurring jobs
  - Uses UTC timezone for consistency

### 4. **Job Monitoring** (`LogJobFilter`)
- **Location**: `AICalendar.Infrastructure.BackgroundJobs.LogJobFilter`
- **Purpose**: Provides comprehensive logging and monitoring for all background jobs
- **Features**:
  - Logs job creation, start, completion, and failures
  - Tracks job execution duration
  - Stores metadata (creation time, start time, job name)
  - Implements both `IClientFilter` and `IServerFilter`

### 5. **Extension Methods**
#### `HangfireServiceExtensions`
- **Location**: `AICalendar.API.Extensions.HangfireServiceExtensions`
- **Purpose**: Encapsulates all Hangfire service registration
- **Configuration**:
  - SQL Server storage with optimized settings
  - Automatic retry with configurable attempts
  - Multiple job queues (default, critical, low)
  - Worker count configuration
  - Job filter registration

#### `HangfireDashboardExtensions`
- **Location**: `AICalendar.API.Extensions.HangfireDashboardExtensions`
- **Purpose**: Configures the Hangfire Dashboard
- **Features**:
  - Custom authorization filter
  - Branded dashboard title
  - 5-second stats polling
  - Antiforgery token handling

## Configuration

### appsettings.json
```json
{
  "Hangfire": {
    "WorkerCount": 20,
    "RetryAttempts": 3
  },
  "BackgroundJobs": {
    "CleanupExpiredPredictions": {
      "CronExpression": "0 2 * * *"
    }
  }
}
```

### Connection String
Hangfire uses the `DefaultConnection` connection string from `appsettings.json` for SQL Server storage.

## Job Queues

The system supports three priority queues:
1. **critical** - High-priority jobs that need immediate processing
2. **default** - Standard priority jobs
3. **low** - Background maintenance tasks

## Dashboard Access

- **URL**: `/hangfire`
- **Authorization**: Currently open for demo purposes (see `HangfireAuthorizationFilter`)
- **Features**:
  - Real-time job monitoring
  - Job history and statistics
  - Manual job triggering
  - Failed job retry

## Current Jobs

### CleanupExpiredPredictionsJob
- **Schedule**: Daily at 2:00 AM UTC (configurable via `appsettings.json`)
- **Purpose**: Remove expired predictions from the database
- **Attributes**:
  - `[AutomaticRetry(Attempts = 3)]` - Retries up to 3 times on failure
  - `[DisableConcurrentExecution]` - Prevents overlapping executions
  - `[DisplayName("Cleanup Expired Predictions")]` - Dashboard display name

## Usage Examples

### Scheduling a One-Time Job
```csharp
public class MyController : ControllerBase
{
    private readonly IHangfireService _hangfireService;

    public MyController(IHangfireService hangfireService)
    {
        _hangfireService = hangfireService;
    }

    public async Task<IActionResult> ScheduleTask()
    {
        var jobId = await _hangfireService.ScheduleJobAsync<MyJob>(
            job => job.ExecuteAsync(CancellationToken.None),
            TimeSpan.FromMinutes(5)
        );

        return Ok(new { jobId });
    }
}
```

### Scheduling a Recurring Job
```csharp
await _hangfireService.ScheduleRecurringJobAsync<MyJob>(
    "my-recurring-job",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily(3) // Every day at 3 AM UTC
);
```

### Triggering an Immediate Job
```csharp
await _hangfireService.TriggerJobAsync<MyJob>(
    job => job.ExecuteAsync(CancellationToken.None)
);
```

## Best Practices

1. **Job Methods**: Always use async methods that return `Task`
2. **Cancellation Tokens**: Pass `CancellationToken` to support graceful shutdown
3. **Attributes**: Use Hangfire attributes for retry, concurrency, and display configuration
4. **Logging**: Jobs automatically log via `LogJobFilter`, but add domain-specific logging as needed
5. **Idempotency**: Design jobs to be idempotent (safe to run multiple times)
6. **Configuration**: Store cron expressions in `appsettings.json` for easy modification

## Improvements Over Previous Implementation

1. ✅ **Removed Hardcoded Values**: Cron expressions now in configuration
2. ✅ **Better Separation of Concerns**: Extensions handle registration, services handle logic
3. ✅ **Enhanced Monitoring**: `LogJobFilter` provides comprehensive job tracking
4. ✅ **Type Safety**: Using `IHangfireService` interface for better testability
5. ✅ **Queue Support**: Multiple priority queues for different job types
6. ✅ **Production Ready**: Proper error handling, logging, and retry mechanisms

## Security Considerations

⚠️ **Current State**: Dashboard is open for demo purposes
🔒 **Production**: Uncomment authentication logic in `HangfireAuthorizationFilter` to require:
- User authentication
- Admin role membership
- Or implement custom authorization logic

## Monitoring & Troubleshooting

1. **Dashboard**: Visit `/hangfire` to view job status and history
2. **Logs**: Check application logs for job execution details
3. **Database**: Hangfire creates tables in the SQL Server database with `HangFire.` prefix
4. **Failed Jobs**: Review failed jobs in the dashboard and retry manually if needed

## Future Enhancements

- [ ] Add more background jobs (email notifications, report generation, etc.)
- [ ] Implement job result persistence
- [ ] Add job progress tracking
- [ ] Configure job timeout settings
- [ ] Add health checks for Hangfire server
- [ ] Implement custom job activator for better DI support
