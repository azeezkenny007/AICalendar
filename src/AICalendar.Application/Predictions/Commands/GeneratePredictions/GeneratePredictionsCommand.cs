using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.GeneratePredictions;

/// <summary>
/// Command to generate AI predictions for a user
/// </summary>
public record GeneratePredictionsCommand(
    UserId UserId,
    string TargetMonth,
    int MaxPredictions = 10,
    string Timezone = "UTC") : IRequest<Result<GeneratePredictionsResponse>>;

/// <summary>
/// Response containing generated prediction information
/// </summary>
public record GeneratePredictionsResponse(
    PredictionId PredictionId,
    int ItemsGenerated,
    string ModelVersion,
    DateTime GeneratedAt);
