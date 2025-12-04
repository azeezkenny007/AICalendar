using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.Users.Commands.RegisterDevice;

public record RegisterDeviceCommand(
    Guid UserId,
    string FcmToken
) : IRequest<OperationResult>;
