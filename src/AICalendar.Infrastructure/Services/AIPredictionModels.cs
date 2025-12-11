using System.Text.Json.Serialization;

namespace AICalendar.Infrastructure.Services;

public class ApiPredictionRequest
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("target_month")]
    public string TargetMonth { get; set; } = string.Empty;

    [JsonPropertyName("historical_transactions")]
    public List<ApiTransaction> HistoricalTransactions { get; set; } = new();

    [JsonPropertyName("max_predictions")]
    public int MaxPredictions { get; set; }
}

public class ApiTransaction
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("ReceiverId")]
    public string? ReceiverId { get; set; }

    [JsonPropertyName("TransactionDate")]
    public string TransactionDate { get; set; } = string.Empty;

    [JsonPropertyName("CreatedAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class ApiPredictionResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("predictions")]
    public List<ApiPredictionItem> Predictions { get; set; } = new();

    [JsonPropertyName("metadata")]
    public ApiPredictionMetadata? Metadata { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public class ApiPredictionItem
{
    [JsonPropertyName("prediction_id")]
    public string PredictionId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("predicted_date")]
    public string PredictedDate { get; set; } = string.Empty;

    [JsonPropertyName("predicted_amount")]
    public decimal PredictedAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("confidence_score")]
    public float ConfidenceScore { get; set; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    [JsonPropertyName("pattern_type")]
    public string PatternType { get; set; } = string.Empty;

    [JsonPropertyName("source_transaction_ids")]
    public List<string> SourceTransactionIds { get; set; } = new();

    [JsonPropertyName("reminder_hours_before")]
    public int ReminderHoursBefore { get; set; }
}

public class ApiPredictionMetadata
{
    [JsonPropertyName("processing_time_ms")]
    public long ProcessingTimeMs { get; set; }

    [JsonPropertyName("transactions_analyzed")]
    public int TransactionsAnalyzed { get; set; }

    [JsonPropertyName("model_version")]
    public string ModelVersion { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public string GeneratedAt { get; set; } = string.Empty;
}
