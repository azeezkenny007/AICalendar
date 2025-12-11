using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Events;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionRejectedFeedbackHandler : INotificationHandler<DomainEventNotification<PredictionBatchRejectedEvent>>
{
    private readonly IUserFeedbackRepository _feedbackRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PredictionRejectedFeedbackHandler> _logger;

    public PredictionRejectedFeedbackHandler(
        IUserFeedbackRepository feedbackRepository,
        IUnitOfWork unitOfWork,
        ILogger<PredictionRejectedFeedbackHandler> logger)
    {
        _feedbackRepository = feedbackRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PredictionBatchRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "FEEDBACK: Handling batch rejection for Prediction {PredictionId}. {Count} items rejected.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        var feedbacks = new List<UserFeedback>();

        foreach (var item in domainEvent.Items)
        {
            _logger.LogInformation(
                "FEEDBACK: Recording negative feedback for item {ItemId}: {Merchant} - ${Amount}",
                item.ItemId.Value,
                item.Merchant,
                item.Amount
            );

            var feedback = UserFeedback.CreateNegative(
                domainEvent.UserId,
                domainEvent.PredictionId,
                item.ItemId,
                item.Merchant,
                item.Amount,
                item.DueDate
            );

            feedbacks.Add(feedback);
        }

        await _feedbackRepository.AddRangeAsync(feedbacks, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "FEEDBACK: Successfully recorded {Count} negative feedback entries for user {UserId}",
            feedbacks.Count,
            domainEvent.UserId.Value
        );
    }
}