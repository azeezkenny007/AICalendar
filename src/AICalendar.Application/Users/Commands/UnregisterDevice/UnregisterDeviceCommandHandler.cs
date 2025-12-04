using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Users.Commands.UnregisterDevice;

public class UnregisterDeviceCommandHandler : IRequestHandler<UnregisterDeviceCommand, OperationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnregisterDeviceCommandHandler> _logger;

    public UnregisterDeviceCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UnregisterDeviceCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OperationResult> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
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

            // Business rule: a user that is not registered cannot be unregistered again
            if (string.IsNullOrEmpty(user.FcmDeviceToken))
            {
                return OperationResult.BadRequest(
                    "No device registered",
                    "User has no registered device token to unregister",
                    "The user does not currently have an FCM device token registered."
                );
            }

            user.ClearDeviceToken();
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleared FCM token for user {UserId}", request.UserId);

            return OperationResult.Success(
                "Device unregistered successfully",
                $"Device token successfully removed for user {request.UserId}",
                new { userId = request.UserId }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device for user {UserId}", request.UserId);
            return OperationResult.ServerError(
                "Unregistration failed",
                "An error occurred while unregistering device",
                "An unexpected error occurred. Please try again later."
            );
        }
    }
}
