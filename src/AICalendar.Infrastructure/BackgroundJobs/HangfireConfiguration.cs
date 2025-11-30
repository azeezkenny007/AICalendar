using Hangfire;
using AICalendar.Application.BackgroundJobs;
using AICalendar.Infrastructure.Outbox;
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
        // ═══════════════════════════════════════════════════════════
        // 🔥 JOB 1: Process Outbox Messages (CRITICAL - Every 10 sec)
        // ═══════════════════════════════════════════════════════════
        // This job reads domain events from the Outbox table and
        // publishes them to event handlers (Calendar, UserFeedback)
        var outboxCron = configuration["BackgroundJobs:ProcessOutbox:CronExpression"]
            ?? "*/10 * * * * *"; // Default: Every 10 seconds

        RecurringJob.AddOrUpdate<OutboxProcessorJob>(
            "process-outbox-messages",
            job => job.ProcessOutboxMessages(),
            outboxCron,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "critical" // High priority queue
            });

        // ═══════════════════════════════════════════════════════════
        // 🔥 JOB 2: Generate Predictions (Nightly at 2 AM)
        // ═══════════════════════════════════════════════════════════
        // This job generates predictions for ALL users by calling
        // the AI service with their transaction history
        var batchPredictionCron = configuration["BackgroundJobs:BatchPrediction:CronExpression"]
            ?? Cron.Daily(2); // Default: 2 AM UTC

        RecurringJob.AddOrUpdate<BatchPredictionJob>(
            "batch-prediction-generation",
            job => job.GeneratePredictionsForAllUsers(),
            batchPredictionCron,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "default"
            });

        // ═══════════════════════════════════════════════════════════
        // 🔥 JOB 3: Cleanup Expired Predictions (Daily at 3 AM)
        // ═══════════════════════════════════════════════════════════
        // This job deletes old predictions that users never reviewed
        var cleanupPredictionsCron = configuration["BackgroundJobs:CleanupExpiredPredictions:CronExpression"]
            ?? Cron.Daily(3); // Default: 3 AM UTC

        RecurringJob.AddOrUpdate<CleanupExpiredPredictionsJob>(
            "cleanup-expired-predictions",
            job => job.ExecuteAsync(CancellationToken.None),
            cleanupPredictionsCron,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "low"
            });

        // ═══════════════════════════════════════════════════════════
        // 🔥 JOB 4: Send Reminders (Every Hour)
        // ═══════════════════════════════════════════════════════════
        // This job checks for upcoming calendar items and sends
        // push notifications to users
        var remindersCron = configuration["BackgroundJobs:SendReminders:CronExpression"]
            ?? Cron.Hourly(); // Default: Every hour

        RecurringJob.AddOrUpdate<SendRemindersJob>(
            "send-reminders",
            job => job.SendDueReminders(),
            remindersCron,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "default"
            });

        // ═══════════════════════════════════════════════════════════
        // 🔥 JOB 5: Cleanup Processed Outbox (Weekly)
        // ═══════════════════════════════════════════════════════════
        // This job deletes old processed outbox messages to keep
        // the database clean
        var cleanupOutboxCron = configuration["BackgroundJobs:CleanupOutbox:CronExpression"]
            ?? Cron.Weekly(DayOfWeek.Sunday, 4); // Default: Sunday at 4 AM UTC

        RecurringJob.AddOrUpdate<CleanupOutboxJob>(
            "cleanup-outbox",
            job => job.CleanupProcessedMessages(),
            cleanupOutboxCron,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                Queue = "low"
            });
    }
}
