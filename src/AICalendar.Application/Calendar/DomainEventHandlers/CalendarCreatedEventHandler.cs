using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.DomainEventHandlers;

/// <summary>
/// Handles CalendarCreatedEvent by logging and invalidating any cached calendar
/// for the associated user so that subsequent reads fetch the latest state.
/// </summary>
public class CalendarCreatedEventHandler : INotificationHandler<DomainEventNotification<CalendarCreatedEvent>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CalendarCreatedEventHandler> _logger;

    public CalendarCreatedEventHandler(
        ICacheService cacheService,
        ILogger<CalendarCreatedEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CalendarCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "CALENDAR: Calendar {CalendarId} created for user {UserId} at {OccurredOn}",
            domainEvent.CalendarId.Value,
            domainEvent.UserId.Value,
            domainEvent.OccurredOn);

        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);
    }
}
