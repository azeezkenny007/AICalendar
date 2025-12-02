using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionAcceptedCalendarHandler : INotificationHandler<DomainEventNotification<PredictionBatchAcceptedEvent>>
{
    private readonly ILogger<PredictionAcceptedCalendarHandler> _logger;

    public PredictionAcceptedCalendarHandler(ILogger<PredictionAcceptedCalendarHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PredictionBatchAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "CALENDAR: Handling batch acceptance for Prediction {PredictionId}. {Count} items accepted.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        foreach (var item in domainEvent.Items)
        {

            // TODO: Add to Calendar (Create Transaction/CalendarEntry)
            _logger.LogInformation("CALENDAR: Adding item {ItemId} to Calendar: {Merchant} - {Amount}", item.ItemId.Value, item.Merchant, item.Amount);
        }

        return Task.CompletedTask;
    }
}
