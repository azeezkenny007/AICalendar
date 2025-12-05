using AICalendar.Application.Services;
using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.Entities;
using AICalendar.Domain.Enums;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.Commands.GeneratePredictions;

/// <summary>
/// Handler for generating AI predictions for a user
/// </summary>
public class GeneratePredictionsCommandHandler : IRequestHandler<GeneratePredictionsCommand, Result<GeneratePredictionsResponse>>
{
    private readonly ILogger<GeneratePredictionsCommandHandler> _logger;
    private readonly IAIPredictionService _aiPredictionService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPredictionRepository _predictionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GeneratePredictionsCommandHandler(
        ILogger<GeneratePredictionsCommandHandler> logger,
        IAIPredictionService aiPredictionService,
        ITransactionRepository transactionRepository,
        IPredictionRepository predictionRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _aiPredictionService = aiPredictionService;
        _transactionRepository = transactionRepository;
        _predictionRepository = predictionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GeneratePredictionsResponse>> Handle(
        GeneratePredictionsCommand request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "🚀 [PREDICTION START] Generating predictions for user {UserId} for month {TargetMonth}",
                request.UserId, request.TargetMonth);

            // 1. Get user's historical transactions
            var transactions = await _transactionRepository.GetByUserIdAsync(
                request.UserId.Value,
                cancellationToken);

            var transactionList = transactions.ToList();

            if (!transactionList.Any())
            {
                _logger.LogWarning(
                    "⚠️  [NO DATA] No transactions found for user {UserId}. Cannot generate predictions.",
                    request.UserId);

                return Result<GeneratePredictionsResponse>.Failure(
                    "No transactions found for user");
            }

            _logger.LogInformation(
                "📊 [DATA LOADED] Found {TransactionCount} transactions for user {UserId} (Total Amount: {TotalAmount:C})",
                transactionList.Count, request.UserId, transactionList.Sum(t => t.Amount));

            // 2. Call AI service to generate predictions
            var aiCallStart = DateTime.UtcNow;
            _logger.LogInformation(
                "🤖 [AI CALL] Calling AI service for user {UserId}...",
                request.UserId);

            var aiResult = await _aiPredictionService.GeneratePredictionsAsync(
                userId: request.UserId.Value.ToString(),
                targetMonth: request.TargetMonth,
                transactions: transactionList,
                maxPredictions: request.MaxPredictions,
                timezone: request.Timezone,
                cancellationToken: cancellationToken);

            var aiCallDuration = (DateTime.UtcNow - aiCallStart).TotalSeconds;

            if (!aiResult.IsSuccess)
            {
                _logger.LogError(
                    "❌ [AI FAILED] AI service failed for user {UserId} after {Duration:F2}s: {Error}",
                    request.UserId, aiCallDuration, aiResult.ErrorMessage);

                return Result<GeneratePredictionsResponse>.Failure(
                    aiResult.ErrorMessage ?? "AI service failed");
            }

            if (!aiResult.Predictions.Any())
            {
                _logger.LogWarning(
                    "⚠️  [NO PREDICTIONS] AI service returned no predictions for user {UserId} after {Duration:F2}s",
                    request.UserId, aiCallDuration);

                return Result<GeneratePredictionsResponse>.Failure(
                    "No predictions were generated");
            }

            _logger.LogInformation(
                "✅ [AI SUCCESS] AI service generated {PredictionCount} predictions for user {UserId} in {Duration:F2}s (Model: {ModelVersion})",
                aiResult.Predictions.Count, request.UserId, aiCallDuration, aiResult.Metadata?.ModelVersion ?? "unknown");

            // Log each prediction item
            foreach (var (predictedItem, index) in aiResult.Predictions.Select((p, i) => (p, i + 1)))
            {
                _logger.LogInformation(
                    "  📝 Prediction {Index}/{Total}: {Title} - {Amount:C} on {Date:yyyy-MM-dd} (Confidence: {Confidence:P0}, Pattern: {Pattern})",
                    index, aiResult.Predictions.Count, predictedItem.Title, predictedItem.PredictedAmount,
                    predictedItem.PredictedDate, predictedItem.ConfidenceScore, predictedItem.PatternType);
            }

            // 3. Create prediction aggregate with items
            var predictionCycle = ParsePredictionCycle(request.TargetMonth);
            var prediction = Prediction.Create(request.UserId, predictionCycle);

            // Add each predicted item to the aggregate
            foreach (var predictedItem in aiResult.Predictions)
            {
                // Parse pattern type
                if (!Enum.TryParse<PatternType>(predictedItem.PatternType, true, out var patternType))
                {
                    patternType = PatternType.OneTime;
                }

                var item = PredictionItem.Create(
                    transactionId: Guid.Empty, // No specific source transaction for now
                    merchant: predictedItem.Title,
                    amount: predictedItem.PredictedAmount,
                    dueDate: predictedItem.PredictedDate,
                    explanation: predictedItem.Reasoning,
                    confidence: ConfidenceScore.Create(predictedItem.ConfidenceScore),
                    pattern: patternType,
                    account: predictedItem.Category,
                    accountName: predictedItem.Description,
                    description: predictedItem.Reasoning
                );

                prediction.AddItem(item);
            }

            // 4. Save prediction to database
            var dbSaveStart = DateTime.UtcNow;
            await _predictionRepository.AddAsync(prediction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var dbSaveDuration = (DateTime.UtcNow - dbSaveStart).TotalSeconds;

            stopwatch.Stop();
            var totalDuration = stopwatch.Elapsed.TotalSeconds;

            _logger.LogInformation(
                "💾 [DB SAVED] Saved prediction {PredictionId} in {DbDuration:F2}s",
                prediction.Id, dbSaveDuration);

            _logger.LogInformation(
                "🎉 [PREDICTION COMPLETE] Successfully generated predictions for user {UserId} | " +
                "Total Time: {TotalDuration:F2}s | AI Time: {AiDuration:F2}s | DB Time: {DbDuration:F2}s | " +
                "Items: {ItemCount} | Total Amount: {TotalAmount:C}",
                request.UserId, totalDuration, aiCallDuration, dbSaveDuration,
                aiResult.Predictions.Count, aiResult.Predictions.Sum(p => p.PredictedAmount));

            var response = new GeneratePredictionsResponse(
                PredictionId: prediction.Id,
                ItemsGenerated: aiResult.Predictions.Count,
                ModelVersion: aiResult.Metadata?.ModelVersion ?? "unknown",
                GeneratedAt: aiResult.Metadata?.GeneratedAt ?? DateTime.UtcNow);

            return Result<GeneratePredictionsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "💥 [ERROR] Unexpected error while generating predictions for user {UserId} after {Duration:F2}s",
                request.UserId, stopwatch.Elapsed.TotalSeconds);

            return Result<GeneratePredictionsResponse>.Failure(
                $"Error generating predictions: {ex.Message}");
        }
    }

    private static PredictionCycle ParsePredictionCycle(string targetMonth)
    {
        // Expected format: YYYY-MM
        var parts = targetMonth.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
        {
            throw new ArgumentException($"Invalid target month format: {targetMonth}. Expected YYYY-MM");
        }

        return PredictionCycle.Monthly(year, month);
    }
}
