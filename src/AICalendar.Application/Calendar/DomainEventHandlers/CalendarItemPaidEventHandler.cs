using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.DomainEventHandlers;

/// <summary>
/// Handles CalendarItemPaidEvent by logging and invalidating the cached calendar
/// for the associated user so that payment status is up to date.
/// </summary>
public class CalendarItemPaidEventHandler : INotificationHandler<DomainEventNotification<CalendarItemPaidEvent>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CalendarItemPaidEventHandler> _logger;

    public CalendarItemPaidEventHandler(
        ICacheService cacheService,
        ILogger<CalendarItemPaidEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CalendarItemPaidEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "CALENDAR: Item {CalendarItemId} for user {UserId} in calendar {CalendarId} marked as paid on {PaidDate} (OccurredOn={OccurredOn})",
            domainEvent.CalendarItemId.Value,
            domainEvent.UserId.Value,
            domainEvent.CalendarId.Value,
            domainEvent.PaidDate,
            domainEvent.OccurredOn);

        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);
    }
}
