using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Persistence.Repositories;

public class UserFeedbackRepository : IUserFeedbackRepository
{
    private readonly ApplicationDbContext _context;

    public UserFeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserFeedback?> GetByIdAsync(UserFeedbackId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<List<UserFeedback>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserFeedback>> GetByPredictionIdAsync(PredictionId predictionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .Where(f => f.PredictionId == predictionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserFeedback>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserFeedback feedback, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserFeedback>().AddAsync(feedback, cancellationToken);
    }

    public async Task AddRangeAsync(List<UserFeedback> feedbacks, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserFeedback>().AddRangeAsync(feedbacks, cancellationToken);
    }

    public async Task<List<UserFeedback>> GetUnsentToAiAsync(CancellationToken cancellationToken = default)
{
    return await _context.Set<UserFeedback>()
        .Where(f => !f.SentToAI)
        .OrderBy(f => f.CreatedAt)
        .ToListAsync(cancellationToken);
}
}