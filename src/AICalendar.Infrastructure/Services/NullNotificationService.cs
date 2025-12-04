using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Services;

/// <summary>
/// Null implementation of notification service for development/testing
/// Logs notification attempts without actually sending them
/// </summary>
public class NullNotificationService : INotificationService
{
    private readonly ILogger<NullNotificationService> _logger;

    public NullNotificationService(ILogger<NullNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would send to user {UserId}: {Title} - {Message}",
            userId.Value,
            title,
            message
        );

        if (data != null && data.Any())
        {
            _logger.LogInformation(
                "[NULL NOTIFICATION] Data: {Data}",
                string.Join(", ", data.Select(kvp => $"{kvp.Key}={kvp.Value}"))
            );
        }

        return Task.CompletedTask;
    }

    public Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would send {Count} notifications",
            notifications.Count
        );

        foreach (var notification in notifications)
        {
            _logger.LogInformation(
                "[NULL NOTIFICATION] - User {UserId}: {Title} - {Message}",
                notification.userId.Value,
                notification.title,
                notification.message
            );
        }

        return Task.CompletedTask;
    }
}
