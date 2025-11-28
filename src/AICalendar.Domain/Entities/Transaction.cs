using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Entities;

public class Transaction : Entity<TransactionId>
{
    public UserId UserId { get; private set; }
    public User User { get; private set; } = null!;
    public decimal Amount { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime TransactionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core needs a parameterless constructor
    private Transaction()
    {
     
    }

    

    
}
