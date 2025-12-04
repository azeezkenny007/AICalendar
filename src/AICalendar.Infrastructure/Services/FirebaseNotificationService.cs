using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Services;

/// <summary>
/// Firebase Cloud Messaging implementation of notification service
/// </summary>
public class FirebaseNotificationService : INotificationService
{
    private readonly ILogger<FirebaseNotificationService> _logger;
    private readonly IUserRepository _userRepository;

    public FirebaseNotificationService(
        ILogger<FirebaseNotificationService> logger,
        IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user's FCM device token
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning(
                    "User {UserId} not found. Cannot send notification.",
                    userId.Value
                );
                return;
            }

            if (string.IsNullOrEmpty(user.FcmDeviceToken))
            {
                _logger.LogWarning(
                    "User {UserId} has no FCM device token. Cannot send notification.",
                    userId.Value
                );
                return;
            }

            // Build notification message
            var fcmMessage = new Message
            {
                Token = user.FcmDeviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = message
                },
                Data = data ?? new Dictionary<string, string>
                {
                    { "userId", userId.Value.ToString() },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                },
                // Android specific settings
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Icon = "notification_icon",
                        Color = "#FF5722", // Orange color
                        Sound = "default",
                        ChannelId = "payment_reminders"
                    }
                },
                // iOS specific settings
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = title,
                            Body = message
                        },
                        Sound = "default",
                        Badge = 1
                    }
                },
                // Web push settings
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = title,
                        Body = message,
                        Icon = "/icon-192x192.png"
                    }
                }
            };

            // Send notification
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage, cancellationToken);

            _logger.LogInformation(
                "Successfully sent notification to user {UserId}. FCM Response: {Response}",
                userId.Value,
                response
            );
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex,
                "Firebase error sending notification to user {UserId}. Error code: {ErrorCode}",
                userId.Value,
                ex.MessagingErrorCode
            );

            // Handle invalid token (user uninstalled app or token expired)
            if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                _logger.LogWarning(
                    "Invalid FCM token for user {UserId}. Clearing token from database.",
                    userId.Value
                );

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user != null)
                {
                    user.ClearDeviceToken();
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending notification to user {UserId}",
                userId.Value
            );
        }
    }

    public async Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications,
        CancellationToken cancellationToken = default)
    {
        var tasks = notifications.Select(n =>
            SendPushNotificationAsync(n.userId, n.title, n.message, null, cancellationToken)
        );

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Sent {Count} bulk notifications",
            notifications.Count
        );
    }
}
