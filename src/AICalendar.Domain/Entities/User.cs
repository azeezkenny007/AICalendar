using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Entities;

public class User : Entity<UserId>
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private User()
    {
    }

    private User(UserId id, string username, string email)
    {
        Id = id;
        Username = username;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    
}
