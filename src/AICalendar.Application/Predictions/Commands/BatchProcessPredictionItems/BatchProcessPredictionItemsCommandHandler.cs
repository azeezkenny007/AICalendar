using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.Commands.BatchProcessPredictionItems;

public class BatchProcessPredictionItemsCommandHandler
    : IRequestHandler<BatchProcessPredictionItemsCommand, Result>
{
    private readonly IPredictionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BatchProcessPredictionItemsCommandHandler> _logger;

    public BatchProcessPredictionItemsCommandHandler(
        IPredictionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<BatchProcessPredictionItemsCommandHandler> _logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<Result> Handle(
        BatchProcessPredictionItemsCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Batch processing {AcceptedCount} accepted and {RejectedCount} rejected items",
            request.AcceptedItemIds.Count,
            request.RejectedItemIds.Count
        );

        var allItemIds = request.AcceptedItemIds.Concat(request.RejectedItemIds).Distinct().ToList();

        if (!allItemIds.Any())
        {
            return Result.Success();
        }

        // In a real app, we might want a repository method to fetch multiple predictions by item IDs
        // For now, we'll process them one by one but save changes at the end
        // Optimization: Group by Prediction if possible, but we don't have PredictionId in command

        // Group items by Prediction to process them in batches per aggregate
        var loadedPredictions = new Dictionary<PredictionId, Domain.Aggregates.PredictionAggregate.Prediction>();
        var itemsByPrediction = new Dictionary<PredictionId, List<PredictionItemId>>();

        // First pass: Identify which prediction each item belongs to
        // Note: This is still N+1 queries if items are scattered, but typically they are from the same prediction
        foreach (var itemId in allItemIds)
        {
            var prediction = loadedPredictions.Values
                .FirstOrDefault(p => p.Items.Any(i => i.Id == itemId));

            if (prediction == null)
            {
                prediction = await _repository.GetByItemIdAsync(itemId, ct);
                if (prediction != null)
                {
                    loadedPredictions[prediction.Id] = prediction;
                }
            }

            if (prediction != null)
            {
                if (!itemsByPrediction.ContainsKey(prediction.Id))
                {
                    itemsByPrediction[prediction.Id] = new List<PredictionItemId>();
                }
                itemsByPrediction[prediction.Id].Add(itemId);
            }
            else
            {
                _logger.LogWarning("Prediction not found for item {ItemId}", itemId);
            }
        }

        // Second pass: Process batches per prediction
        foreach (var predictionId in itemsByPrediction.Keys)
        {
            var prediction = loadedPredictions[predictionId];
            var itemIds = itemsByPrediction[predictionId];

            var acceptedInThisBatch = itemIds.Intersect(request.AcceptedItemIds).ToList();
            var rejectedInThisBatch = itemIds.Intersect(request.RejectedItemIds).ToList();

            if (acceptedInThisBatch.Any())
            {
                var result = prediction.AcceptItems(acceptedInThisBatch);
                if (result.IsFailure)
                {
                    // In a real app, we might want to rollback or return partial success
                    // For now, we log and continue, or fail fast. Let's fail fast for safety.
                    return Result.Failure(result.Error);
                }
            }

            if (rejectedInThisBatch.Any())
            {
                var result = prediction.RejectItems(rejectedInThisBatch);
                if (result.IsFailure)
                {
                    return Result.Failure(result.Error);
                }
            }

            await _repository.UpdateAsync(prediction, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Successfully processed batch items");

        return Result.Success();
    }
}
