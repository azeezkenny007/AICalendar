using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Persistence.Repositories;

public class PredictionRepository : IPredictionRepository
{
    private readonly ApplicationDbContext _context;

    public PredictionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Prediction?> GetByIdAsync(PredictionId id, CancellationToken ct = default)
    {
        return await _context.Set<Prediction>()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Prediction?> GetByItemIdAsync(PredictionItemId itemId, CancellationToken ct = default)
    {
        return await _context.Set<Prediction>()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Items.Any(i => i.Id == itemId), ct);
    }

    public async Task<Prediction?> GetByUserAndCycleAsync(UserId userId, PredictionCycle cycle, CancellationToken ct = default)
    {
        return await _context.Set<Prediction>()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Cycle == cycle, ct);
    }

    public async Task<List<Prediction>> GetByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        return await _context.Set<Prediction>()
            .Include(p => p.Items)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Prediction prediction, CancellationToken ct = default)
    {
        await _context.Set<Prediction>().AddAsync(prediction, ct);
    }

    public Task UpdateAsync(Prediction prediction, CancellationToken ct = default)
    {
        _context.Set<Prediction>().Update(prediction);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Prediction prediction, CancellationToken ct = default)
    {
        _context.Set<Prediction>().Remove(prediction);
        return Task.CompletedTask;
    }
}
