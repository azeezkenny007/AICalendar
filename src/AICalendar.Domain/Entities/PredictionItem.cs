using AICalendar.Domain.Common;
using AICalendar.Domain.Enums;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Entities;

public class PredictionItem : Entity<PredictionItemId>
{
    public Guid TransactionId { get; private set; } // Link to source transaction
    public string Merchant { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }
    public string Explanation { get; private set; } = string.Empty; // AI reasoning
    public ConfidenceScore Confidence { get; private set; } = default!;
    public PatternType Pattern { get; private set; }
    public bool? IsAccepted { get; private set; } // null = Pending, true = Accepted, false = Rejected

    private PredictionItem(
        PredictionItemId id,
        Guid transactionId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string explanation,
        ConfidenceScore confidence,
        PatternType pattern)
        : base(id)
    {
        TransactionId = transactionId;
        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        Explanation = explanation;
        Confidence = confidence;
        Pattern = pattern;
    }

    // EF Core
    private PredictionItem() { }

    public static PredictionItem Create(
        Guid transactionId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string explanation,
        ConfidenceScore confidence,
        PatternType pattern)
    {
        return new PredictionItem(PredictionItemId.Create(), transactionId, merchant, amount, dueDate, explanation, confidence, pattern);
    }

    public void Accept()
    {
        IsAccepted = true;
    }

    public void Reject()
    {
        IsAccepted = false;
    }
}
