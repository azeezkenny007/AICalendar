using AICalendar.Domain.Entities;

namespace AICalendar.Application.Services;

/// <summary>
/// Service interface for communicating with the AI prediction gRPC service
/// </summary>
public interface IAIPredictionService
{
    /// <summary>
    /// Generates predictions for a user based on their historical transactions
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="targetMonth">Target month in YYYY-MM format</param>
    /// <param name="transactions">Historical transactions</param>
    /// <param name="maxPredictions">Maximum number of predictions to generate</param>
    /// <param name="timezone">User's timezone</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of predicted items with metadata</returns>
    Task<PredictionResult> GeneratePredictionsAsync(
        string userId,
        string targetMonth,
        IEnumerable<Transaction> transactions,
        int maxPredictions = 10,
        string timezone = "UTC",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from AI prediction service
/// </summary>
public record PredictionResult(
    bool IsSuccess,
    List<PredictedItemDto> Predictions,
    PredictionMetadataDto? Metadata,
    string? ErrorMessage);

/// <summary>
/// Predicted item from AI service
/// </summary>
public record PredictedItemDto(
    string PredictionId,
    string Title,
    string Description,
    string Category,
    DateTime PredictedDate,
    decimal PredictedAmount,
    string Currency,
    float ConfidenceScore,
    string Reasoning,
    string PatternType,
    List<string> SourceTransactionIds,
    int ReminderHoursBefore);

/// <summary>
/// Metadata about the prediction generation
/// </summary>
public record PredictionMetadataDto(
    long ProcessingTimeMs,
    int TransactionsAnalyzed,
    DateTime AnalysisStartDate,
    DateTime AnalysisEndDate,
    string ModelVersion,
    DateTime GeneratedAt);
