using Hangfire;
using System.Threading;
using AICalendar.Application.BackgroundJobs;

using Microsoft.Extensions.Configuration;

namespace AICalendar.Infrastructure.BackgroundJobs;

/// <summary>
/// Configuration class for Hangfire background jobs
/// Registers and schedules all recurring jobs
/// </summary>
public static class HangfireConfiguration
{
    /// <summary>
    /// Configures and schedules all recurring Hangfire jobs
    /// Call this method during application startup
    /// </summary>
    public static void ConfigureRecurringJobs(IConfiguration configuration)
    {
        // Get cron expression from configuration or default to daily at 2 AM UTC
        var cleanupCron = configuration["BackgroundJobs:CleanupExpiredPredictions:CronExpression"] ?? Cron.Daily(2);

        // Schedule cleanup of expired predictions
        RecurringJob.AddOrUpdate<CleanupExpiredPredictionsJob>(
            "cleanup-expired-predictions",
            job => job.ExecuteAsync(CancellationToken.None),
            cleanupCron,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Add more recurring jobs here as needed
        // Example:
        // RecurringJob.AddOrUpdate<AnotherJob>(
        //     "another-job-id",
        //     job => job.ExecuteAsync(CancellationToken.None),
        //     Cron.Hourly(0)); // Every hour
    }
}

