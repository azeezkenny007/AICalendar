using AICalendar.Domain.Interfaces;
using AICalendar.Infrastructure.Data;

namespace AICalendar.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// Unit of Work implementation for managing database transactions.
/// Domain events are automatically converted to outbox messages by OutboxInterceptor.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // OutboxInterceptor automatically handles domain event conversion
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // OutboxInterceptor automatically handles domain event conversion
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
