using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;

public record GetUserFeedbackStatsQuery(UserId UserId) : IRequest<Result<FeedbackStatsDto>>;