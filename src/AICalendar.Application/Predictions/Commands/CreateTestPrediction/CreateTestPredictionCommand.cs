using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.CreateTestPrediction;

public record CreateTestPredictionCommand() : IRequest<Result<TestPredictionResult>>;

public record TestPredictionResult(Guid PredictionId, Guid UserId);
