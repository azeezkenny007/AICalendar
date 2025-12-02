using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionRejectedFeedbackHandler : INotificationHandler<DomainEventNotification<PredictionBatchRejectedEvent>>
{
    private readonly ILogger<PredictionRejectedFeedbackHandler> _logger;

    public PredictionRejectedFeedbackHandler(ILogger<PredictionRejectedFeedbackHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<PredictionBatchRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "FEEDBACK: Handling batch rejection for Prediction {PredictionId}. {Count} items rejected.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        foreach (var item in domainEvent.Items)
        {
            // TODO: Create UserFeedback (Negative)
             Console.WriteLine(item);
            _logger.LogInformation("FEEDBACK: Recording negative feedback for item {ItemId}", item.ItemId.Value);
        }

        return Task.CompletedTask;
    }
}
