using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/users")]
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
    [HttpPost("register-device")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Test endpoint to send a notification to a user
    /// </summary>
    [HttpPost("test-notification/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Clears a user's FCM device token (e.g., on logout)
    /// </summary>
    [HttpPost("unregister-device/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

public record RegisterDeviceRequest(Guid UserId, string FcmToken);
