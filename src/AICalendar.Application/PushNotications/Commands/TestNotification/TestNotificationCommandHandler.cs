using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.PushNotifications.Commands.TestNotification;

public class TestNotificationCommandHandler : IRequestHandler<TestNotificationCommand, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TestNotificationCommandHandler> _logger;

    public TestNotificationCommandHandler(
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<TestNotificationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<OperationResult> Handle(TestNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(UserId.Create(request.UserId));

            if (user == null)
            {
                return OperationResult.NotFound(
                    "User not found",
                    $"User {request.UserId} not found",
                    $"The user with ID '{request.UserId}' does not exist."
                );
            }

            if (string.IsNullOrEmpty(user.FcmDeviceToken))
            {
                return OperationResult.BadRequest(
                    "No device token",
                    "User has no registered device token",
                    "The user must register a device token before receiving notifications."
                );
            }

            await _notificationService.SendPushNotificationAsync(
                UserId.Create(request.UserId),
                "Test Notification",
                "This is a test notification from AICalendar",
                new Dictionary<string, string>
                {
                    { "type", "test" },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                }
            );

            _logger.LogInformation("Sent test notification to user {UserId}", request.UserId);

            return OperationResult.Success(
                "Test notification sent successfully",
                $"A test notification has been sent to user {request.UserId}",
                new { userId = request.UserId }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification to user {UserId}", request.UserId);
            return OperationResult.ServerError(
                "Notification failed",
                "An error occurred while sending notification",
                "An unexpected error occurred. Please try again later."
            );
        }
    }
}
