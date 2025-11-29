using Hangfire;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AICalendar.Infrastructure.BackgroundJobs;

/// <summary>
/// Service for scheduling Hangfire jobs programmatically
/// Can be used to schedule one-time or recurring jobs from application code
/// </summary>
public class HangfireJobScheduler
{
    private readonly ILogger<HangfireJobScheduler> _logger;

    public HangfireJobScheduler(ILogger<HangfireJobScheduler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Schedule a one-time job to run after a delay
    /// </summary>
    public string ScheduleJob<T>(Expression<Action<T>> methodCall, TimeSpan delay) where T : class
    {
        try
        {
            var jobId = BackgroundJob.Schedule(methodCall, delay);
            _logger.LogInformation("Scheduled job {JobId} of type {JobType} to run in {Delay}",
                jobId, typeof(T).Name, delay);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule job of type {JobType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Schedule a one-time job to run at a specific time
    /// </summary>
    public string ScheduleJob<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt) where T : class
    {
        try
        {
            var jobId = BackgroundJob.Schedule(methodCall, enqueueAt);
            _logger.LogInformation("Scheduled job {JobId} of type {JobType} to run at {EnqueueAt}",
                jobId, typeof(T).Name, enqueueAt);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule job of type {JobType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Enqueue a job to run immediately
    /// </summary>
    public string EnqueueJob<T>(Expression<Action<T>> methodCall) where T : class
    {
        try
        {
            var jobId = BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Enqueued job {JobId} of type {JobType}", jobId, typeof(T).Name);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job of type {JobType}", typeof(T).Name);
            throw;
        }
    }
}

