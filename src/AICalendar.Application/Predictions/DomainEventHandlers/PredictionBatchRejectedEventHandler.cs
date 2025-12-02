using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionBatchRejectedEventHandler : INotificationHandler<DomainEventNotification<PredictionBatchRejectedEvent>>
{
    private readonly ILogger<PredictionBatchRejectedEventHandler> _logger;

    public PredictionBatchRejectedEventHandler(ILogger<PredictionBatchRejectedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PredictionBatchRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "Handling batch rejection for Prediction {PredictionId}. {Count} items rejected.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        foreach (var item in domainEvent.Items)
        {
            // TODO: Create UserFeedback (Negative)
            _logger.LogInformation("Recording negative feedback for item {ItemId}", item.ItemId.Value);
        }

        return Task.CompletedTask;
    }
}
