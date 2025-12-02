using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionBatchAcceptedEventHandler : INotificationHandler<DomainEventNotification<PredictionBatchAcceptedEvent>>
{
    private readonly ILogger<PredictionBatchAcceptedEventHandler> _logger;

    public PredictionBatchAcceptedEventHandler(ILogger<PredictionBatchAcceptedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PredictionBatchAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "Handling batch acceptance for Prediction {PredictionId}. {Count} items accepted.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        foreach (var item in domainEvent.Items)
        {
            // TODO: Add to Calendar (Create Transaction/CalendarEntry)
            _logger.LogInformation("Adding item {ItemId} to Calendar: {Merchant} - {Amount}", item.ItemId.Value, item.Merchant, item.Amount);

            // TODO: Create UserFeedback (Positive)
            _logger.LogInformation("Recording positive feedback for item {ItemId}", item.ItemId.Value);
        }

        return Task.CompletedTask;
    }
}
