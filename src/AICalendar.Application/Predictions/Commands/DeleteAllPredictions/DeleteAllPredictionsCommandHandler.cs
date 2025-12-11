using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.Commands.DeleteAllPredictions;

/// <summary>
/// Handler for deleting all prediction data
/// </summary>
public class DeleteAllPredictionsCommandHandler : IRequestHandler<DeleteAllPredictionsCommand, Result<DeleteAllPredictionsResponse>>
{
    private readonly IPredictionRepository _predictionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAllPredictionsCommandHandler> _logger;

    public DeleteAllPredictionsCommandHandler(
        IPredictionRepository predictionRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAllPredictionsCommandHandler> logger)
    {
        _predictionRepository = predictionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DeleteAllPredictionsResponse>> Handle(
        DeleteAllPredictionsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Starting deletion of all prediction data");

            // Get all predictions
            var allPredictions = await _predictionRepository.GetAllAsync(cancellationToken);

            var deletedCount = allPredictions.Count;

            if (deletedCount > 0)
            {
                // Delete each prediction
                foreach (var prediction in allPredictions)
                {
                    await _predictionRepository.DeleteAsync(prediction, cancellationToken);
                }
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogWarning(
                    "Successfully deleted all prediction data. Removed {DeletedCount} prediction record(s)",
                    deletedCount
                );
            }
            else
            {
                _logger.LogInformation("No prediction data to delete");
            }

            return Result<DeleteAllPredictionsResponse>.Success(
                new DeleteAllPredictionsResponse(deletedCount)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all prediction data");
            return Result<DeleteAllPredictionsResponse>.Failure("Failed to delete prediction data: " + ex.Message);
        }
    }
}
