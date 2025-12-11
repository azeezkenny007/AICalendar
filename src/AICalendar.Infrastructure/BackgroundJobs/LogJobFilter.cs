using Hangfire.Client;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.BackgroundJobs;

// Custom job filter for enhanced logging
public class LogJobFilter : IClientFilter, IServerFilter
{
    private readonly ILogger<LogJobFilter> _logger;

    public LogJobFilter(ILogger<LogJobFilter> logger)
    {
        _logger = logger;
    }

    public void OnCreating(CreatingContext context)
    {
        context.SetJobParameter("CreatedAt", DateTime.UtcNow);

        var jobName = context.Job.Method.Name;
        var jobType = context.Job.Type.Name;

        _logger.LogDebug("Creating job - Type: {JobType}, Method: {JobMethod}",
            jobType, jobName);
    }

    public void OnCreated(CreatedContext context)
    {
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        _logger.LogInformation("Job {JobId} created and queued successfully", jobId);
    }

    public void OnPerforming(PerformingContext context)
    {
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        var jobName = context.BackgroundJob?.Job?.Method?.Name ?? "unknown";

        _logger.LogInformation("Starting execution of job {JobId} ({JobName})",
            jobId, jobName);

        context.SetJobParameter("StartTime", DateTime.UtcNow);
        context.SetJobParameter("JobName", jobName);
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobId = context.BackgroundJob?.Id ?? "unknown";
        var jobName = context.GetJobParameter<string>("JobName") ?? "unknown";
        var startTime = context.GetJobParameter<DateTime?>("StartTime");
        var duration = startTime.HasValue ? DateTime.UtcNow - startTime.Value : TimeSpan.Zero;

        if (context.Exception != null)
        {
            _logger.LogError(context.Exception,
                "Job {JobId} ({JobName}) failed after {Duration}", jobId, jobName, duration);
        }
        else
        {
            _logger.LogInformation(
                "Job {JobId} ({JobName}) completed successfully in {Duration}",
                jobId, jobName, duration);
        }
    }
}
