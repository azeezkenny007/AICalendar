using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Queries.GetPrediction;

public record GetPredictionQuery(PredictionId Id) : IRequest<Result<PredictionDto>>;
