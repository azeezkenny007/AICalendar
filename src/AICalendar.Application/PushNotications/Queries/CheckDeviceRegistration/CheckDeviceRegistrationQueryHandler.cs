using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.PushNotifications.Queries.CheckDeviceRegistration;

public class CheckDeviceRegistrationQueryHandler : IRequestHandler<CheckDeviceRegistrationQuery, OperationResult<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CheckDeviceRegistrationQueryHandler> _logger;

    public CheckDeviceRegistrationQueryHandler(
        IUserRepository userRepository,
        ILogger<CheckDeviceRegistrationQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<OperationResult<bool>> Handle(CheckDeviceRegistrationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(
                UserId.Create(request.UserId)
            );

            if (user == null)
            {
                return OperationResult<bool>.NotFound(
                    "User not found",
                    $"User {request.UserId} not found",
                    $"The user with ID '{request.UserId}' does not exist."
                );
            }

            var hasRegisteredDevice = !string.IsNullOrEmpty(user.FcmDeviceToken);

            _logger.LogInformation(
                "Checked device registration for user {UserId}: {IsRegistered}",
                request.UserId,
                hasRegisteredDevice
            );

            return OperationResult<bool>.Success(
                "Device registration status retrieved",
                $"User {request.UserId} device registration status: {hasRegisteredDevice}",
                hasRegisteredDevice
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking device registration for user {UserId}", request.UserId);
            return OperationResult<bool>.ServerError(
                "Check failed",
                "An error occurred while checking device registration",
                "An unexpected error occurred. Please try again later."
            );
        }
    }
}
