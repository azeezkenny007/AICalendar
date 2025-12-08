namespace AICalendar.Application.Feedback.AI;

public interface IAIFeedbackClient
{
    Task SendFeedbackAsync(AiFeedbackRequestDto request, CancellationToken cancellationToken);
}
