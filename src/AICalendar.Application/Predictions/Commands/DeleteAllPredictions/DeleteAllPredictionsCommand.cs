using AICalendar.Domain.Common;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.DeleteAllPredictions;

/// <summary>
/// Command to delete all prediction data from the system
/// </summary>
public record DeleteAllPredictionsCommand : IRequest<Result<DeleteAllPredictionsResponse>>;

/// <summary>
/// Response from delete all predictions command
/// </summary>
public record DeleteAllPredictionsResponse(int DeletedRecordCount);
