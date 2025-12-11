using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.PushNotifications.Queries.CheckDeviceRegistration;

public record CheckDeviceRegistrationQuery(
    Guid UserId
) : IRequest<OperationResult<bool>>;
