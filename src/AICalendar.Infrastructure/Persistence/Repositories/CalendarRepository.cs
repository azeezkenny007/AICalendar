using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Persistence.Repositories;

public class CalendarRepository : ICalendarRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Calendar?> GetByIdAsync(CalendarId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Calendar>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Calendar?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Calendar>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<List<Calendar>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Calendar>()
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Calendar calendar, CancellationToken cancellationToken = default)
    {
        await _context.Set<Calendar>().AddAsync(calendar, cancellationToken);
    }

    public Task UpdateAsync(Calendar calendar, CancellationToken cancellationToken = default)
    {
        _context.Set<Calendar>().Update(calendar);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Calendar calendar, CancellationToken cancellationToken = default)
    {
        _context.Set<Calendar>().Remove(calendar);
        return Task.CompletedTask;
    }
}
