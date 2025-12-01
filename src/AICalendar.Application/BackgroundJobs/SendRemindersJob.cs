using Microsoft.Extensions.Logging;

namespace AICalendar.Application.BackgroundJobs;

/// <summary>
/// Hangfire job that sends reminders for upcoming calendar items
/// Runs every hour
/// </summary>
public class SendRemindersJob
{
    private readonly ILogger<SendRemindersJob> _logger;

    public SendRemindersJob(ILogger<SendRemindersJob> logger)
    {
        _logger = logger;
    }

    public async Task SendDueReminders()
    {
        _logger.LogInformation("Starting reminder processing at {Time}", DateTime.UtcNow);

        try
        {
            // TODO: Implement when CalendarRepository and notification service are ready
            // 1. Get calendar items due in next 24 hours
            // var upcomingItems = await _calendarRepository.GetUpcomingItemsAsync(
            //     startDate: DateTime.UtcNow,
            //     endDate: DateTime.UtcNow.AddHours(24)
            // );

            // 2. For each item, send reminder
            // foreach (var item in upcomingItems)
            // {
            //     await _notificationService.SendReminderAsync(
            //         userId: item.UserId,
            //         message: $"Reminder: {item.Description} due on {item.DueDate:MMM dd}"
            //     );
            // }

            _logger.LogInformation("Reminder processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reminder processing");
            throw;
        }
    }
}
