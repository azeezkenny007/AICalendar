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

    // EF Core needs a parameterless constructor
    private Transaction()
    {

    }

    private Transaction(TransactionId id, UserId userId, decimal amount, string description, TransactionType type)
    {
        Id = id;
        UserId = userId;
        Amount = amount;
        Description = description;
        Type = type;
        TransactionDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public static Transaction Create(UserId userId, decimal amount, string description, TransactionType type)
    {
        if (userId == null)
            throw new ArgumentNullException(nameof(userId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        return new Transaction(TransactionId.Create(), userId, amount, description, type);
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
}
