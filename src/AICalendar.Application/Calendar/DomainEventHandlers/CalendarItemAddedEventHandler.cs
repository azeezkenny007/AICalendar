using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.DomainEventHandlers;

/// <summary>
/// Handles CalendarItemAddedEvent by logging and invalidating the cached calendar
/// for the associated user so that newly added items appear on subsequent reads.
/// </summary>
public class CalendarItemAddedEventHandler : INotificationHandler<DomainEventNotification<CalendarItemAddedEvent>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CalendarItemAddedEventHandler> _logger;

    public CalendarItemAddedEventHandler(
        ICacheService cacheService,
        ILogger<CalendarItemAddedEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CalendarItemAddedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "CALENDAR: Item {CalendarItemId} added to calendar {CalendarId} for user {UserId}. Merchant={Merchant}, Amount={Amount}, DueDate={DueDate}, PredictionItemId={PredictionItemId}",
            domainEvent.CalendarItemId.Value,
            domainEvent.CalendarId.Value,
            domainEvent.UserId.Value,
            domainEvent.Merchant,
            domainEvent.Amount,
            domainEvent.DueDate,
            domainEvent.PredictionItemId.Value);

        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);
    }
}
