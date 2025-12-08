using AICalendar.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Feedback.AI.Jobs;
public class SendFeedbackToAIJob
{
    private readonly IUserFeedbackRepository _feedbackRepository;
    private readonly IAIFeedbackClient _aiClient;
    private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SendFeedbackToAIJob> _logger;

    public SendFeedbackToAIJob(
        IUserFeedbackRepository feedbackRepository,
        IAIFeedbackClient aiClient,
        IUnitOfWork unitOfWork,
        ILogger<SendFeedbackToAIJob> logger
        )
    {
        _feedbackRepository = feedbackRepository;
        _aiClient = aiClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI FEEDBACK JOB STARTED at {Time}", DateTime.UtcNow);

        var feedbacks = await _feedbackRepository.GetUnsentToAiAsync(cancellationToken);

        if (!feedbacks.Any())
            {
                _logger.LogInformation("No unsent feedback found. Job exiting.");
                return;
            }

        _logger.LogInformation(
            "Found {Count} unsent feedback records to send to AI",
            feedbacks.Count
        );

        var groupedByUser = feedbacks.GroupBy(f => f.UserId.Value);

        foreach (var group in groupedByUser)
        {
            _logger.LogInformation(
                "Preparing to send {Count} feedback items for User {UserId}",
                group.Count(),
                group.Key
            );
            var request = new AiFeedbackRequestDto(
                group.Key,
                group.Select(f => new AiFeedbackItemDto(
                    prediction_id: f.PredictionId.Value.ToString(),
                    action: f.Action.ToString().ToUpper(),
                    prediction_details: new AiPredictionDetailsDto(
                        f.Merchant,
                        f.Amount
                    ),
                    discard_reason: f.Action == FeedbackAction.Rejected ? "User rejected prediction" : null,
                    edited_data: f.WasEdited
                        ? new AiEditedDataDto(
                            f.Merchant,
                            f.Amount,
                            f.DueDate,
                            "OTHER"
                        )
                        : null
                )).ToList()
            );

            _logger.LogInformation(
                "Sending feedback batch to AI for User {UserId}",
                group.Key
            );

            await _aiClient.SendFeedbackAsync(request, cancellationToken);

            foreach (var feedback in group)
                feedback.MarkAsSentToAI();

            _logger.LogInformation(
                "Marked {Count} feedback items as sent to AI for User {UserId}",
                group.Count(),
                group.Key
            );
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AI FEEDBACK JOB COMPLETED at {Time}", DateTime.UtcNow);
    }
}
