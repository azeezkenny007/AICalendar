using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Entities;

public class Transaction : Entity<TransactionId>
{
    public UserId UserId { get; private set; }
    public User User { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsKept { get; private set; } = false;
    public bool IsDiscarded { get; private set; } = false;
    public string? ReceiverId { get; private set; }
    public string? MerchantId { get; private set; }

    // EF Core needs a parameterless constructor
    private Transaction()
    {

    }

    private Transaction(TransactionId id, UserId userId, decimal amount, string description, TransactionType type, string? receiverId = null, string? merchantId = null)
    {
        Id = id;
        UserId = userId;
        Amount = amount;
        Description = description;
        Type = type;
        TransactionDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        ReceiverId = receiverId;
        MerchantId = merchantId;
    }

    public static Transaction Create(UserId userId, decimal amount, string description, TransactionType type, string? receiverId = null, string? merchantId = null)
    {
        if (userId == null)
            throw new ArgumentNullException(nameof(userId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        return new Transaction(TransactionId.Create(), userId, amount, description, type, receiverId, merchantId);
    }

    public void UpdateAmount(decimal amount)
    {
        Amount = amount;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        Description = description;
    }

    public void UpdateType(TransactionType type)
    {
        Type = type;
    }

    public void MarkAsKept()
    {
        IsKept = true;
        IsDiscarded = false;
    }

    public void MarkAsDiscarded()
    {
        IsDiscarded = true;
        IsKept = false;
    }

    public void UpdateReceiverId(string? receiverId)
    {
        ReceiverId = receiverId;
    }

    public void UpdateMerchantId(string? merchantId)
    {
        MerchantId = merchantId;
    }
}
