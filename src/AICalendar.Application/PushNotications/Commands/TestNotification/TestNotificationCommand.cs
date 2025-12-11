using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.PushNotifications.Commands.TestNotification;

public record TestNotificationCommand(
    Guid UserId
) : IRequest<OperationResult>;
