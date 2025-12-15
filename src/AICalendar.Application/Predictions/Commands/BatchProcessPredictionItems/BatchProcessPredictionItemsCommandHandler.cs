using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.Commands.BatchProcessPredictionItems;

public class BatchProcessPredictionItemsCommandHandler
    : IRequestHandler<BatchProcessPredictionItemsCommand, Result>
{
    private readonly IPredictionRepository _repository;
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BatchProcessPredictionItemsCommandHandler> _logger;

    public BatchProcessPredictionItemsCommandHandler(
        IPredictionRepository repository,
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<BatchProcessPredictionItemsCommandHandler> _logger)
    {
        _repository = repository;
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
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
                return Result.Failure($"Prediction item {itemId} not found.");
            }
        }

        // Second pass: Process batches per prediction
        foreach (var predictionId in itemsByPrediction.Keys)
        {
            var prediction = loadedPredictions[predictionId];
            var itemIds = itemsByPrediction[predictionId];

            var acceptedInThisBatch = itemIds.Intersect(request.AcceptedItemIds).ToList();
            _logger.LogInformation(
                "Processing Prediction {PredictionId} with {AcceptedCount} accepted and {RejectedCount} rejected items",
                predictionId,
                acceptedInThisBatch.Count,
                acceptedInThisBatch
            );

            var rejectedInThisBatch = itemIds.Intersect(request.RejectedItemIds).ToList();

            if (acceptedInThisBatch.Any())
            {
                var result = prediction.AcceptItems(acceptedInThisBatch);
                
                if (result.IsFailure)
                {
                    return Result.Failure(result.Error);
                }

                // Populate calendar with accepted items
                var populateCalendarResult = await PopulateCalendarWithAcceptedItems(
                    prediction, 
                    acceptedInThisBatch, 
                    ct);
                
                if (populateCalendarResult.IsFailure)
                {
                    _logger.LogError(
                        "Failed to populate calendar for prediction {PredictionId}: {Error}",
                        predictionId,
                        populateCalendarResult.Error);
                    return populateCalendarResult;
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

    /// <summary>
    /// Populates the user's calendar with accepted prediction items
    /// </summary>
    private async Task<Result> PopulateCalendarWithAcceptedItems(
        Domain.Aggregates.PredictionAggregate.Prediction prediction,
        List<PredictionItemId> acceptedItemIds,
        CancellationToken ct)
    {
        try
        {
            // Get or create user's calendar
            var calendar = await _calendarRepository.GetByUserIdAsync(prediction.UserId, ct);

            if (calendar == null)
            {
                _logger.LogInformation("CALENDAR: Creating new calendar for user {UserId}", prediction.UserId.Value);
                calendar = Domain.Aggregates.CalendarAggregate.Calendar.Create(prediction.UserId);
                await _calendarRepository.AddAsync(calendar, ct);
                
                // Save the calendar to the database immediately so it has an ID for foreign key constraint
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("CALENDAR: New calendar created and saved for user {UserId}", prediction.UserId.Value);
            }

            // Get the accepted items from the prediction
            var acceptedItems = prediction.Items
                .Where(item => acceptedItemIds.Contains(item.Id))
                .ToList();

            _logger.LogInformation(
                "CALENDAR: Processing {Count} accepted items for user {UserId}",
                acceptedItems.Count,
                prediction.UserId.Value);

            // Add each accepted item to the calendar
            foreach (var item in acceptedItems)
            {
                _logger.LogInformation(
                    "CALENDAR: Adding item {ItemId} to Calendar: {Merchant} - ${Amount} due on {DueDate}",
                    item.Id.Value,
                    item.Merchant,
                    item.Amount,
                    item.DueDate.ToString("yyyy-MM-dd")
                );

                var result = calendar.AddItemFromPrediction(
                    item.Id,
                    item.Merchant,
                    item.Amount,
                    item.DueDate
                );

                if (!result.IsSuccess)
                {
                    _logger.LogWarning(
                        "CALENDAR: Failed to add item {ItemId}: {Error}",
                        item.Id.Value,
                        result.Error
                    );
                    return result;
                }
            }

            // Update the calendar with the new items
            await _calendarRepository.UpdateAsync(calendar, ct);

            // Invalidate cache for this user's calendar
            var cacheKey = $"calendar:user:{prediction.UserId.Value}";
            await _cacheService.RemoveAsync(cacheKey);

            _logger.LogInformation(
                "CALENDAR: Successfully processed {Count} items for user {UserId}",
                acceptedItems.Count,
                prediction.UserId.Value
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CALENDAR: Error populating calendar for user {UserId}",
                prediction.UserId.Value);
            return Result.Failure($"Failed to populate calendar: {ex.Message}");
        }
    }
}
