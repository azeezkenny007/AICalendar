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

    // New fields for Transfers/Bills
    public string? Account { get; private set; }
    public string? AccountName { get; private set; }
    public string? Description { get; private set; }

    public bool IsEdited { get; private set; }
    public decimal? OriginalAmount { get; private set; }
    public DateTime? OriginalDueDate { get; private set; }

    private PredictionItem(
        PredictionItemId id,
        Guid transactionId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string explanation,
        ConfidenceScore confidence,
        PatternType pattern,
        string? account = null,
        string? accountName = null,
        string? description = null)
        : base(id)
    {
        TransactionId = transactionId;
        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        Explanation = explanation;
        Confidence = confidence;
        Pattern = pattern;
        Account = account;
        AccountName = accountName;
        Description = description;
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
        PatternType pattern,
        string? account = null,
        string? accountName = null,
        string? description = null)
    {
        return new PredictionItem(PredictionItemId.Create(), transactionId, merchant, amount, dueDate, explanation, confidence, pattern, account, accountName, description);
    }

    public Result Edit(
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account,
        string? accountName,
        string? description)
    {
        if (IsAccepted.HasValue)
        {
            return Result.Failure("Cannot edit an item that has already been accepted or rejected.");
        }

        if (!IsEdited)
        {
            OriginalAmount = Amount;
            OriginalDueDate = DueDate;
            IsEdited = true;
        }

        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        Account = account;
        AccountName = accountName;
        Description = description;

        return Result.Success();
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
