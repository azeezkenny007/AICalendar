using AICalendar.Domain.ValueObjects;

namespace AICalendar.Application.Common.Interfaces;

/// <summary>
/// Service for sending push notifications to users
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a push notification to a specific user
    /// </summary>
    /// <param name="userId">The user to notify</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message body</param>
    /// <param name="data">Optional additional data</param>
    Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends push notifications to multiple users
    /// </summary>
    Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications,
        CancellationToken cancellationToken = default
    );
}
