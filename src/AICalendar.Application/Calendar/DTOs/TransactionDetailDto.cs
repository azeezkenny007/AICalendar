namespace AICalendar.Application.Calendar.DTOs;

public class TransactionDetailDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsKept { get; set; }
    public bool IsDiscarded { get; set; }
    public string? ReceiverId { get; set; }
    public string? MerchantId { get; set; }

    // Calendar-specific fields
    public string PredictionStatus { get; set; } = "Pending"; // Pending, Kept, Discarded, Edited
    public DateTime? PredictedNextDate { get; set; }
    public double? Confidence { get; set; }
    public string? AIExplanation { get; set; }
}
