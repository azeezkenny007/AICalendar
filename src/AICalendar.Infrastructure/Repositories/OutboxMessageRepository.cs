using AICalendar.Domain.Interfaces;
using AICalendar.Infrastructure.Data;

namespace AICalendar.Infrastructure.Repositories;

/// <summary>
/// Repository for OutboxMessage operations
/// </summary>
public class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxMessageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the count of all outbox messages
    /// </summary>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.OutboxMessages.Count();
    }

    /// <summary>
    /// Deletes all outbox messages
    /// </summary>
    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _dbContext.OutboxMessages.RemoveRange(_dbContext.OutboxMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
