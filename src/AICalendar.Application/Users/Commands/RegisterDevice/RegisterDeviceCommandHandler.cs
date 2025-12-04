using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Users.Commands.RegisterDevice;

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterDeviceCommandHandler> _logger;

    public RegisterDeviceCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegisterDeviceCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OperationResult> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(
                UserId.Create(request.UserId)
            );

            if (user == null)
            {
                return OperationResult.NotFound(
                    "User not found",
                    $"User {request.UserId} not found",
                    $"The user with ID '{request.UserId}' does not exist."
                );
            }

            // Business rule: a user that is already registered cannot register again
            if (!string.IsNullOrEmpty(user.FcmDeviceToken))
            {
                return OperationResult.Conflict(
                    "Device already registered",
                    "User already has a registered device token",
                    "The user already has an active FCM device token. Unregister the existing device before registering a new one."
                );
            }

            user.UpdateDeviceToken(request.FcmToken);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Registered FCM token for user {UserId}",
                request.UserId
            );

            return OperationResult.Success(
                "Device registered successfully",
                $"Device token successfully registered for user {request.UserId}",
                new { userId = request.UserId }
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for device registration");
            return OperationResult.BadRequest(
                "Invalid request",
                ex.Message,
                "The device token or user ID provided is invalid."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token for user {UserId}", request.UserId);
            return OperationResult.ServerError(
                "Registration failed",
                "An error occurred while registering device",
                "An unexpected error occurred. Please try again later."
            );
        }
    }
}
