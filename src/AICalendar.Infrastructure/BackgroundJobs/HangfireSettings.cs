namespace AICalendar.Infrastructure.BackgroundJobs;

public class HangfireSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int WorkerCount { get; set; } = 20;
    public int RetryAttempts { get; set; } = 3;
}
