using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.CreateTestPrediction;

public record CreateTestPredictionCommand(
    UserId UserId
) : IRequest<Result<PredictionId>>;
