using Hangfire;
using System.Threading;
using AICalendar.Application.BackgroundJobs;

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
    public static void ConfigureRecurringJobs()
    {
        // Schedule cleanup of expired predictions - runs daily at 2 AM UTC
        RecurringJob.AddOrUpdate<CleanupExpiredPredictionsJob>(
            "cleanup-expired-predictions",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(2), // Daily at 2:00 AM UTC
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

