using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.DomainEventHandlers;

/// <summary>
/// Handles CalendarItemRemovedEvent by logging and invalidating the cached calendar
/// for the associated user so that removed items no longer appear on subsequent reads.
/// </summary>
public class CalendarItemRemovedEventHandler : INotificationHandler<DomainEventNotification<CalendarItemRemovedEvent>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CalendarItemRemovedEventHandler> _logger;

    public CalendarItemRemovedEventHandler(
        ICacheService cacheService,
        ILogger<CalendarItemRemovedEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CalendarItemRemovedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "CALENDAR: Item {CalendarItemId} removed from calendar {CalendarId} for user {UserId} at {OccurredOn}",
            domainEvent.CalendarItemId.Value,
            domainEvent.CalendarId.Value,
            domainEvent.UserId.Value,
            domainEvent.OccurredOn);

        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);
    }
}
