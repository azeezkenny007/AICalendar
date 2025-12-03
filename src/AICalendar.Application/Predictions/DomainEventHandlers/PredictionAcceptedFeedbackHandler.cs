using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Events;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionAcceptedFeedbackHandler : INotificationHandler<DomainEventNotification<PredictionBatchAcceptedEvent>>
{
    private readonly IUserFeedbackRepository _feedbackRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PredictionAcceptedFeedbackHandler> _logger;

    public PredictionAcceptedFeedbackHandler(
        IUserFeedbackRepository feedbackRepository,
        IUnitOfWork unitOfWork,
        ILogger<PredictionAcceptedFeedbackHandler> logger)
    {
        _feedbackRepository = feedbackRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PredictionBatchAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "FEEDBACK: Handling batch acceptance for Prediction {PredictionId}. {Count} items accepted.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        var feedbacks = new List<UserFeedback>();

        foreach (var item in domainEvent.Items)
        {
            var feedbackType = item.IsEdited ? "edited then accepted" : "accepted as-is";

            _logger.LogInformation(
                "FEEDBACK: Recording positive feedback for item {ItemId}: {Merchant} - ${Amount} ({Type})",
                item.ItemId.Value,
                item.Merchant,
                item.Amount,
                feedbackType
            );

            var feedback = UserFeedback.CreatePositive(
                domainEvent.UserId,
                domainEvent.PredictionId,
                item.ItemId,
                item.Merchant,
                item.Amount,
                item.DueDate,
                item.IsEdited,
                item.OriginalAmount,
                item.OriginalDueDate
            );

            feedbacks.Add(feedback);
        }

        await _feedbackRepository.AddRangeAsync(feedbacks, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "FEEDBACK: Successfully recorded {Count} positive feedback entries for user {UserId}",
            feedbacks.Count,
            domainEvent.UserId.Value
        );
    }
}