namespace AICalendar.Domain.Interfaces;

/// <summary>
/// Interface for OutboxMessage repository operations
/// </summary>
public interface IOutboxMessageRepository
{
    /// <summary>
    /// Gets the count of all outbox messages
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all outbox messages
    /// </summary>
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
