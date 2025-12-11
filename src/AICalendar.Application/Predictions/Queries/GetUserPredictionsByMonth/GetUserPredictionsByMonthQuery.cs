using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictionsByMonth;

public record GetUserPredictionsByMonthQuery(
    UserId UserId,
    int Year,
    int Month
) : IRequest<Result<List<PredictionDto>>>;
