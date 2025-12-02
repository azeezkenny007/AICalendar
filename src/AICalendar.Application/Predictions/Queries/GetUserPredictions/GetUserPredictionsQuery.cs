using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictions;

public record GetUserPredictionsQuery(UserId UserId) : IRequest<Result<List<PredictionDto>>>;
