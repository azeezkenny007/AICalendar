using AICalendar.Domain.Entities;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Persistence.Repositories;

public class FailedPredictionAttemptRepository : IFailedPredictionAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public FailedPredictionAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FailedPredictionAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _context.FailedPredictionAttempts.AddAsync(attempt, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FailedPredictionAttempt> attempts, CancellationToken cancellationToken = default)
    {
        await _context.FailedPredictionAttempts.AddRangeAsync(attempts, cancellationToken);
    }

    public async Task<IEnumerable<FailedPredictionAttempt>> GetUnresolvedAttemptsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FailedPredictionAttempts
            .Where(a => !a.IsResolved)
            .OrderBy(a => a.FailedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FailedPredictionAttempt>> GetUnresolvedAttemptsAsync(string targetMonth, CancellationToken cancellationToken = default)
    {
        return await _context.FailedPredictionAttempts
            .Where(a => !a.IsResolved && a.TargetMonth == targetMonth)
            .OrderBy(a => a.FailedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FailedPredictionAttempt?> GetByIdAsync(FailedPredictionAttemptId id, CancellationToken cancellationToken = default)
    {
        return await _context.FailedPredictionAttempts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<FailedPredictionAttempt?> GetByUserAndMonthAsync(UserId userId, string targetMonth, CancellationToken cancellationToken = default)
    {
        return await _context.FailedPredictionAttempts
            .Where(a => a.UserId == userId && a.TargetMonth == targetMonth && !a.IsResolved)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Update(FailedPredictionAttempt attempt)
    {
        _context.FailedPredictionAttempts.Update(attempt);
    }
}
