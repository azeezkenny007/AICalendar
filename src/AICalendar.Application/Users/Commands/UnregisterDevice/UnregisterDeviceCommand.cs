using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.Users.Commands.UnregisterDevice;

public record UnregisterDeviceCommand(
    Guid UserId
) : IRequest<OperationResult>;
