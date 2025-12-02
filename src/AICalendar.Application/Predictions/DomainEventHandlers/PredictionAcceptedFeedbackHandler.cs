using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionAcceptedFeedbackHandler : INotificationHandler<DomainEventNotification<PredictionBatchAcceptedEvent>>
{
    private readonly ILogger<PredictionAcceptedFeedbackHandler> _logger;

    public PredictionAcceptedFeedbackHandler(ILogger<PredictionAcceptedFeedbackHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PredictionBatchAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "FEEDBACK: Handling batch acceptance for Prediction {PredictionId}. {Count} items accepted.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        foreach (var item in domainEvent.Items)
        {
            Console.WriteLine(item);
            // TODO: Create UserFeedback (Positive)
            _logger.LogInformation("FEEDBACK: Recording positive feedback for item {ItemId}", item.ItemId.Value);
        }

        return Task.CompletedTask;
    }
}
