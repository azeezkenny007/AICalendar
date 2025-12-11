using AICalendar.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AICalendar.Infrastructure.Services;

/// <summary>
/// REST client implementation for AI prediction service
/// </summary>
public class AIPredictionService : IAIPredictionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIPredictionService> _logger;
    private readonly string _functionKey;

    public AIPredictionService(
        HttpClient httpClient,
        ILogger<AIPredictionService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["AIService:Url"] ?? "http://localhost:7071/api";
        _functionKey = configuration["AIService:Key"] ?? string.Empty;

        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PredictionResult> GeneratePredictionsAsync(
        string userId,
        string targetMonth,
        IEnumerable<AICalendar.Domain.Entities.Transaction> transactions,
        int maxPredictions = 10,
        string timezone = "UTC",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Calling AI service (REST) to generate predictions for user {UserId} for month {TargetMonth}",
                userId, targetMonth);

            // Map transactions to API DTO
            var transactionDtos = transactions.Select(t => new ApiTransaction
            {
                Id = t.Id.Value.ToString(),
                UserId = t.UserId.Value.ToString(),
                Amount = t.Amount, // Decimal
                Description = t.Description,
                Type = t.Type.ToString(),
                ReceiverId = t.ReceiverId ?? t.MerchantId ?? "UNKNOWN", // Fallback logic
                TransactionDate = t.TransactionDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                CreatedAt = t.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
            }).ToList();

            var requestDto = new ApiPredictionRequest
            {
                UserId = userId,
                TargetMonth = targetMonth,
                HistoricalTransactions = transactionDtos,
                MaxPredictions = maxPredictions
            };

            var url = $"predictions?code={_functionKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestDto, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "AI service returned non-success status code: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return new PredictionResult(
                    IsSuccess: false,
                    Predictions: new List<Application.Services.PredictedItemDto>(),
                    Metadata: null,
                    ErrorMessage: $"HTTP Error {response.StatusCode}: {errorContent}");
            }

            var responseDto = await response.Content.ReadFromJsonAsync<ApiPredictionResponse>(cancellationToken: cancellationToken);

            if (responseDto == null)
            {
                return new PredictionResult(
                    IsSuccess: false,
                    Predictions: new List<Application.Services.PredictedItemDto>(),
                    Metadata: null,
                    ErrorMessage: "Empty response from AI service");
            }

            if (responseDto.Status != "SUCCESS" && responseDto.Status != "PARTIAL_SUCCESS")
            {
                return new PredictionResult(
                    IsSuccess: false,
                    Predictions: new List<Application.Services.PredictedItemDto>(),
                    Metadata: null,
                    ErrorMessage: responseDto.ErrorMessage ?? $"AI Service Status: {responseDto.Status}");
            }

            // Map API predictions to Application DTOs
            var predictions = responseDto.Predictions.Select(p => new Application.Services.PredictedItemDto(
                PredictionId: p.PredictionId,
                Title: p.Title,
                Description: p.Description,
                Category: p.Category,
                PredictedDate: DateTime.Parse(p.PredictedDate),
                PredictedAmount: p.PredictedAmount,
                Currency: p.Currency,
                ConfidenceScore: p.ConfidenceScore,
                Reasoning: p.Reasoning,
                PatternType: p.PatternType,
                SourceTransactionIds: p.SourceTransactionIds,
                ReminderHoursBefore: p.ReminderHoursBefore
            )).ToList();

            // Map API metadata to Application DTO
            Application.Services.PredictionMetadataDto? metadata = null;
            if (responseDto.Metadata != null)
            {
                metadata = new Application.Services.PredictionMetadataDto(
                    ProcessingTimeMs: responseDto.Metadata.ProcessingTimeMs,
                    TransactionsAnalyzed: responseDto.Metadata.TransactionsAnalyzed,
                    AnalysisStartDate: DateTime.MinValue, // Not in API response currently
                    AnalysisEndDate: DateTime.MinValue,   // Not in API response currently
                    ModelVersion: responseDto.Metadata.ModelVersion,
                    GeneratedAt: DateTime.Parse(responseDto.Metadata.GeneratedAt)
                );
            }

            _logger.LogInformation(
                "Successfully received {Count} predictions from AI service for user {UserId}",
                predictions.Count, userId);

            return new PredictionResult(
                IsSuccess: true,
                Predictions: predictions,
                Metadata: metadata,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while calling AI service for user {UserId}",
                userId);

            return new PredictionResult(
                IsSuccess: false,
                Predictions: new List<PredictedItemDto>(),
                Metadata: null,
                ErrorMessage: $"Unexpected error: {ex.Message}");
        }
    }
}
