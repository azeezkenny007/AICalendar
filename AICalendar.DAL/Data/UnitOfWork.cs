using AICalendar.CORE.Interfaces;

namespace AICalendar.DAL.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AICalendarDbContext _context;

    public UnitOfWork(AICalendarDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
