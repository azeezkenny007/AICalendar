using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Entities;

public class User : Entity<UserId>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Firebase Cloud Messaging device token
    public string? FcmDeviceToken { get; private set; }
    public DateTime? FcmTokenUpdatedAt { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}";

    private User()
    {
    }

    private User(UserId id, string firstName, string lastName, string username, string email)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(string firstName, string lastName, string username, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        return new User(UserId.Create(), firstName, lastName, username, email);
    }

    /// <summary>
    /// Updates the user's FCM device token for push notifications
    /// </summary>
    public void UpdateDeviceToken(string fcmToken)
    {
        if (string.IsNullOrWhiteSpace(fcmToken))
            throw new ArgumentException("FCM token cannot be empty", nameof(fcmToken));

        FcmDeviceToken = fcmToken;
        FcmTokenUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears the user's FCM device token (e.g., when token is invalid or user logs out)
    /// </summary>
    public void ClearDeviceToken()
    {
        FcmDeviceToken = null;
        FcmTokenUpdatedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
