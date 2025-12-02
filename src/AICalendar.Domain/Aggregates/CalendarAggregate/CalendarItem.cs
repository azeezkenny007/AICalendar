using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Aggregates.CalendarAggregate;

/// <summary>
/// Represents a single scheduled item in the calendar (bill, subscription, transfer)
/// </summary>
public class CalendarItem : Entity<CalendarItemId>
{
    public PredictionItemId PredictionItemId { get; private set; } = default!;
    public string Merchant { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }

    // Optional fields for transfers/bills
    public string? Account { get; private set; }
    public string? AccountName { get; private set; }
    public string? Description { get; private set; }

    public bool IsPaid { get; private set; }
    public DateTime? PaidDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private CalendarItem() { }

    private CalendarItem(
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account,
        string? accountName,
        string? description)
        : base(CalendarItemId.Create())
    {
        PredictionItemId = predictionItemId;
        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        Account = account;
        AccountName = accountName;
        Description = description;
        IsPaid = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static CalendarItem CreateFromPrediction(
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account = null,
        string? accountName = null,
        string? description = null)
    {
        return new CalendarItem(
            predictionItemId,
            merchant,
            amount,
            dueDate,
            account,
            accountName,
            description
        );
    }

    public Result MarkAsPaid(DateTime paidDate)
    {
        if (IsPaid)
        {
            return Result.Failure("Item is already marked as paid.");
        }

        IsPaid = true;
        PaidDate = paidDate;

        return Result.Success();
    }

    public Result Update(
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account = null,
        string? accountName = null,
        string? description = null)
    {
        if (IsPaid)
        {
            return Result.Failure("Cannot update a paid calendar item.");
        }

        if (string.IsNullOrWhiteSpace(merchant))
        {
            return Result.Failure("Merchant cannot be empty.");
        }

        if (amount <= 0)
        {
            return Result.Failure("Amount must be greater than zero.");
        }

        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        Account = account;
        AccountName = accountName;
        Description = description;

        return Result.Success();
    }
}
