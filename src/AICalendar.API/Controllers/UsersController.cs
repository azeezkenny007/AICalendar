using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

/// <summary>
/// Manages user device registration and push notification settings
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Registers or updates a user's FCM device token for push notifications
    /// </summary>
    /// <param name="request">The device registration request containing user ID and FCM token</param>
    /// <returns>Success status of the device registration</returns>
    /// <response code="200">Device token successfully registered for the user</response>
    /// <response code="400">Invalid request data or malformed FCM token</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">An unexpected error occurred during registration</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users/register-device
    ///     {
    ///       "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "fcmToken": "fGcI7X8kRZuQ9..."
    ///     }
    ///
    /// The FCM token should be obtained from Firebase SDK on the client device.
    /// If a user already has a token registered, this will update it with the new token.
    /// Use this endpoint when the app starts or when the FCM token is refreshed.
    /// </remarks>
    [HttpPost("register-device")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterDeviceRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(
                UserId.Create(request.UserId)
            );

            if (user == null)
            {
                return NotFound(new { message = $"User {request.UserId} not found" });
            }

            user.UpdateDeviceToken(request.FcmToken);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Registered FCM token for user {UserId}",
                request.UserId
            );

            return Ok(new { message = "Device registered successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for device registration");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token");
            return StatusCode(500, new { message = "An error occurred while registering device" });
        }
    }

    /// <summary>
    /// Sends a test push notification to a user's registered device
    /// </summary>
    /// <param name="userId">The unique identifier of the user to send the test notification to</param>
    /// <returns>Success status of the test notification</returns>
    /// <response code="200">Test notification sent successfully to the user's device</response>
    /// <response code="400">User has no registered device token</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">An error occurred while sending the notification</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users/test-notification/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// This endpoint is useful for testing that Firebase Cloud Messaging is properly configured
    /// and that the user's device is correctly receiving notifications.
    /// The user must have a registered FCM device token before calling this endpoint.
    /// </remarks>
    [HttpPost("test-notification/{userId:guid}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestNotification(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(UserId.Create(userId));
            if (user == null)
            {
                return NotFound(new { message = $"User {userId} not found" });
            }

            if (string.IsNullOrEmpty(user.FcmDeviceToken))
            {
                return BadRequest(new { message = "User has no registered device token" });
            }

            await _notificationService.SendPushNotificationAsync(
                UserId.Create(userId),
                "Test Notification",
                "This is a test notification from AICalendar",
                new Dictionary<string, string>
                {
                    { "type", "test" },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                }
            );

            _logger.LogInformation("Sent test notification to user {UserId}", userId);

            return Ok(new { message = "Test notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(500, new { message = "An error occurred while sending notification" });
        }
    }

    /// <summary>
    /// Removes a user's FCM device token to stop receiving push notifications
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unregister</param>
    /// <returns>Success status of the device unregistration</returns>
    /// <response code="200">Device token successfully removed for the user</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">An error occurred while unregistering the device</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/users/unregister-device/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// Call this endpoint when a user logs out or when they want to stop receiving push notifications.
    /// After unregistering, the user will not receive any push notifications until they register a new device token.
    /// This helps maintain user privacy and reduces unnecessary notification attempts.
    /// </remarks>
    [HttpPost("unregister-device/{userId:guid}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnregisterDevice(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(UserId.Create(userId));
            if (user == null)
            {
                return NotFound(new { message = $"User {userId} not found" });
            }

            user.ClearDeviceToken();
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cleared FCM token for user {UserId}", userId);

            return Ok(new { message = "Device unregistered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device");
            return StatusCode(500, new { message = "An error occurred while unregistering device" });
        }
    }
}

/// <summary>
/// Request model for registering a device for push notifications
/// </summary>
/// <param name="UserId">The unique identifier of the user</param>
/// <param name="FcmToken">The Firebase Cloud Messaging device token obtained from the Firebase SDK</param>
public record RegisterDeviceRequest(Guid UserId, string FcmToken);

/// <summary>
/// Standard message response format
/// </summary>
/// <param name="Message">The message describing the result of the operation</param>
public record MessageResponse(string Message);
