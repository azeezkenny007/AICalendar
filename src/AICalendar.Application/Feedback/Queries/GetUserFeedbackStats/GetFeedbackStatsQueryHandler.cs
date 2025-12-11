using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;

public class GetUserFeedbackStatsQueryHandler : IRequestHandler<GetUserFeedbackStatsQuery, Result<FeedbackStatsDto>>
{
    private readonly IUserFeedbackRepository _feedbackRepository;
    private readonly IUserRepository _userRepository;

    public GetUserFeedbackStatsQueryHandler(IUserFeedbackRepository feedbackRepository, IUserRepository userRepository)
    {
        _feedbackRepository = feedbackRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<FeedbackStatsDto>> Handle(GetUserFeedbackStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<FeedbackStatsDto>.Failure($"User with ID {request.UserId} not found.");
        }
        
        var feedbacks = await _feedbackRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (!feedbacks.Any())
        {
            return Result<FeedbackStatsDto>.Success(new FeedbackStatsDto(
                request.UserId.Value,
                0, 0, 0, 0, 0, 0
            ));
        }

        var total = feedbacks.Count;
        var positive = feedbacks.Count(f => f.Action == FeedbackAction.Accepted);
        var negative = feedbacks.Count(f => f.Action == FeedbackAction.Rejected);
        var edited = feedbacks.Count(f => f.WasEdited);

        var acceptanceRate = total > 0 ? (decimal)positive / total * 100 : 0;
        var editRate = positive > 0 ? (decimal)edited / positive * 100 : 0;

        var dto = new FeedbackStatsDto(
            request.UserId.Value,
            total,
            positive,
            negative,
            edited,
            Math.Round(acceptanceRate, 2),
            Math.Round(editRate, 2)
        );

        return Result<FeedbackStatsDto>.Success(dto);
    }
}