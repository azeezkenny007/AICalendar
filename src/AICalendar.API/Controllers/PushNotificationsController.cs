using AICalendar.Application.PushNotifications.Commands.RegisterDevice;
using AICalendar.Application.PushNotifications.Commands.TestNotification;
using AICalendar.Application.PushNotifications.Commands.UnregisterDevice;
using AICalendar.Application.PushNotifications.Queries.CheckDeviceRegistration;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AICalendar.API.Controllers;

/// <summary>
/// Manages user device registration and push notification settings
/// </summary>
[ApiController]
[Route("api/push-notifications")]
[Produces("application/json")]
public class PushNotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PushNotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registers a user's FCM device token for push notifications
    /// </summary>
    /// <param name="request">The device registration request containing user ID and FCM token</param>
    /// <returns>Success status of the device registration</returns>
    /// <response code="200">Device token successfully registered for the user</response>
    /// <response code="400">Invalid request data or malformed FCM token</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="409">User already has a registered device token</response>
    /// <response code="500">An unexpected error occurred during registration</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/push-notifications/register-device
    ///     {
    ///       "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "fcmToken": "fGcI7X8kRZuQ9Y2vN3pL4mK5jH6gF7dS8aA9zX0cV1bN2mM3"
    ///     }
    ///
    /// Sample 200 response:
    ///
    ///     {
    ///       "message": "Device registered successfully"
    ///     }
    ///
    /// Sample 400 response:
    ///
    ///     {
    ///       "message": "Invalid FCM token format"
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     {
    ///       "message": "User not found"
    ///     }
    ///
    /// Sample 409 response:
    ///
    ///     {
    ///       "message": "User already has a registered device"
    ///     }
    ///
    /// The FCM token should be obtained from Firebase SDK on the client device.
    /// A user that already has a registered device must unregister it before registering a new one.
    /// Use this endpoint when the app starts or when the FCM token is first registered.
    /// </remarks>
    [HttpPost("register-device")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterDeviceRequest request)
    {
        var command = new RegisterDeviceCommand(request.UserId, request.FcmToken);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { message = result.Title })
            : result.StatusCode switch
            {
                404 => NotFound(new { message = result.Error }),
                400 => BadRequest(new { message = result.Error }),
                409 => Conflict(new { message = result.Error }),
                _ => StatusCode(500, new { message = result.Error })
            };
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
    ///     POST /api/push-notifications/test-notification/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// Sample 200 response:
    ///
    ///     {
    ///       "message": "Test notification sent successfully"
    ///     }
    ///
    /// Sample 400 response:
    ///
    ///     {
    ///       "message": "User has no registered device"
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     {
    ///       "message": "User not found"
    ///     }
    ///
    /// Sample 500 response:
    ///
    ///     {
    ///       "message": "Failed to send notification"
    ///     }
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
        var command = new TestNotificationCommand(userId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { message = result.Title })
            : result.StatusCode switch
            {
                404 => NotFound(new { message = result.Error }),
                400 => BadRequest(new { message = result.Error }),
                _ => StatusCode(500, new { message = result.Error })
            };
    }

    /// <summary>
    /// Removes a user's FCM device token to stop receiving push notifications
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unregister</param>
    /// <returns>Success status of the device unregistration</returns>
    /// <response code="200">Device token successfully removed for the user</response>
    /// <response code="400">User does not have a registered device token</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">An error occurred while unregistering the device</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/push-notifications/unregister-device/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// Sample 200 response:
    ///
    ///     {
    ///       "message": "Device unregistered successfully"
    ///     }
    ///
    /// Sample 400 response:
    ///
    ///     {
    ///       "message": "User has no registered device"
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     {
    ///       "message": "User not found"
    ///     }
    ///
    /// Sample 500 response:
    ///
    ///     {
    ///       "message": "Failed to unregister device"
    ///     }
    ///
    /// Call this endpoint when a user logs out or when they want to stop receiving push notifications.
    /// After unregistering, the user will not receive any push notifications until they register a new device token.
    /// A user that is not registered cannot be unregistered again.
    /// This helps maintain user privacy and reduces unnecessary notification attempts.
    /// </remarks>
    [HttpPost("unregister-device/{userId:guid}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnregisterDevice(Guid userId)
    {
        var command = new UnregisterDeviceCommand(userId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { message = result.Title })
            : result.StatusCode switch
            {
                404 => NotFound(new { message = result.Error }),
                400 => BadRequest(new { message = result.Error }),
                _ => StatusCode(500, new { message = result.Error })
            };
    }

    /// <summary>
    /// Checks if a user has a registered FCM device token
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check</param>
    /// <returns>A boolean indicating whether the user has a registered device token</returns>
    /// <response code="200">Returns true if the user has a registered device token, false otherwise</response>
    /// <response code="404">User with the specified ID was not found</response>
    /// <response code="500">An error occurred while checking device registration</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/push-notifications/check-device/{userId}
    ///
    /// Sample 200 response when device is registered:
    ///
    ///     {
    ///       "isRegistered": true
    ///     }
    ///
    /// Sample 200 response when device is not registered:
    ///
    ///     {
    ///       "isRegistered": false
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     {
    ///       "message": "User not found"
    ///     }
    ///
    /// Sample 500 response:
    ///
    ///     {
    ///       "message": "Failed to check device registration"
    ///     }
    ///
    /// Use this endpoint to check if a user already has a registered FCM device token
    /// before attempting to register a new one. This helps prevent conflicts and
    /// provides better user experience by allowing conditional registration flows.
    /// </remarks>
    [HttpGet("check-device/{userId:guid}")]
    [OutputCache(PolicyName = "notifications-short")]
    [ProducesResponseType(typeof(DeviceRegistrationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckDeviceRegistration(Guid userId)
    {
        var query = new CheckDeviceRegistrationQuery(userId);
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(new { isRegistered = result.Data })
            : result.StatusCode switch
            {
                404 => NotFound(new { message = result.Error }),
                _ => StatusCode(500, new { message = result.Error })
            };
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

/// <summary>
/// Response model for device registration status
/// </summary>
/// <param name="IsRegistered">Boolean indicating whether the user has a registered FCM device token</param>
public record DeviceRegistrationStatusResponse(bool IsRegistered);
