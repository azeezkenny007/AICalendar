using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.BackgroundJobs;

/// <summary>
/// Hangfire job that sends reminders for upcoming calendar items
/// Runs every hour
/// </summary>
public class SendRemindersJob
{
    private readonly ILogger<SendRemindersJob> _logger;
    private readonly ICalendarRepository _calendarRepository;
    private readonly INotificationService _notificationService;

    public SendRemindersJob(
        ILogger<SendRemindersJob> logger,
        ICalendarRepository calendarRepository,
        INotificationService notificationService)
    {
        _logger = logger;
        _calendarRepository = calendarRepository;
        _notificationService = notificationService;
    }

    public async Task SendDueReminders()
    {
        _logger.LogInformation("Starting reminder processing at {Time}", DateTime.UtcNow);

        try
        {
            // Get all unpaid calendar items with payment due dates
            var unpaidItems = await _calendarRepository.GetUnpaidItemsWithDueDatesAsync();

            if (!unpaidItems.Any())
            {
                _logger.LogInformation("No unpaid items found. Skipping reminder processing.");
                return;
            }

            _logger.LogInformation("Found {Count} unpaid items to check", unpaidItems.Count);

            var notificationsSent = 0;
            var now = DateTime.UtcNow;

            foreach (var (item, userId) in unpaidItems)
            {
                var paymentDue = item.DueDate;

                // Check each time window and send notification if matched
                string? notificationMessage = null;

                // BEFORE DUE NOTIFICATIONS
                if (now >= paymentDue.AddHours(-24) && now < paymentDue.AddHours(-23))
                {
                    notificationMessage = $"Payment Due Soon: {item.Merchant} - ${item.Amount:F2} due in 24 hours ({paymentDue:MMM dd, h:mm tt} UTC)";
                }
                else if (now >= paymentDue.AddHours(-6) && now < paymentDue.AddHours(-5))
                {
                    notificationMessage = $"Payment Due Soon: {item.Merchant} - ${item.Amount:F2} due in 6 hours ({paymentDue:h:mm tt} UTC)";
                }
                else if (now >= paymentDue.AddHours(-1) && now < paymentDue)
                {
                    notificationMessage = $"Payment Due Soon: {item.Merchant} - ${item.Amount:F2} due in 1 hour ({paymentDue:h:mm tt} UTC)";
                }
                // AFTER DUE NOTIFICATIONS (OVERDUE)
                else if (now >= paymentDue.AddHours(12) && now < paymentDue.AddHours(13))
                {
                    notificationMessage = $"Payment Overdue: {item.Merchant} - ${item.Amount:F2} was due 12 hours ago";
                }
                else if (now >= paymentDue.AddHours(24) && now < paymentDue.AddHours(25))
                {
                    notificationMessage = $"Payment Overdue: {item.Merchant} - ${item.Amount:F2} was due 24 hours ago";
                }
                else if (now >= paymentDue.AddHours(48) && now < paymentDue.AddHours(49))
                {
                    notificationMessage = $"Payment Overdue: {item.Merchant} - ${item.Amount:F2} was due 48 hours ago";
                }

                // Send notification if a time window matched
                if (!string.IsNullOrEmpty(notificationMessage))
                {
                    await _notificationService.SendPushNotificationAsync(
                        userId: userId,
                        title: "AICalendar Payment Reminder",
                        message: notificationMessage,
                        data: new Dictionary<string, string>
                        {
                            { "calendarItemId", item.Id.Value.ToString() },
                            { "type", "payment_reminder" },
                            { "merchant", item.Merchant },
                            { "amount", item.Amount.ToString("F2") },
                            { "dueDate", paymentDue.ToString("O") }
                        }
                    );

                    notificationsSent++;

                    _logger.LogInformation(
                        "Sent reminder for item {ItemId} to user {UserId}: {Message}",
                        item.Id.Value,
                        userId.Value,
                        notificationMessage
                    );
                }
            }

            _logger.LogInformation(
                "Reminder processing completed. Sent {SentCount} notifications out of {TotalCount} unpaid items",
                notificationsSent,
                unpaidItems.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reminder processing");
            throw;
        }
    }
}
