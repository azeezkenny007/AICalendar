using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictionItemsLimit;

public record GetUserPredictionItemsLimitQuery(UserId UserId, int Limit = 10)
    : IRequest<Result<List<PredictionDto>>>;
