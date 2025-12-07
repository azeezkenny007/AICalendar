using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.PushNotifications.Commands.RegisterDevice;

public record RegisterDeviceCommand(
    Guid UserId,
    string FcmToken
) : IRequest<OperationResult>;
