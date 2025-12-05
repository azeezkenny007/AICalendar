using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.DomainEventHandlers;

/// <summary>
/// Handles CalendarItemUpdatedEvent by logging and invalidating the cached calendar
/// for the associated user so that updates are reflected on subsequent reads.
/// </summary>
public class CalendarItemUpdatedEventHandler : INotificationHandler<DomainEventNotification<CalendarItemUpdatedEvent>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CalendarItemUpdatedEventHandler> _logger;

    public CalendarItemUpdatedEventHandler(
        ICacheService cacheService,
        ILogger<CalendarItemUpdatedEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CalendarItemUpdatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "CALENDAR: Item {CalendarItemId} updated in calendar {CalendarId} for user {UserId}. Merchant={Merchant}, Amount={Amount}, DueDate={DueDate}, Account={Account}, AccountName={AccountName}, Description={Description}",
            domainEvent.CalendarItemId.Value,
            domainEvent.CalendarId.Value,
            domainEvent.UserId.Value,
            domainEvent.Merchant,
            domainEvent.Amount,
            domainEvent.DueDate,
            domainEvent.Account,
            domainEvent.AccountName,
            domainEvent.Description);

        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);
    }
}
