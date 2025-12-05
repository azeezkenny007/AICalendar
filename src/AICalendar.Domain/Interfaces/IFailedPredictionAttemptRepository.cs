using AICalendar.Domain.Entities;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Interfaces;

public interface IFailedPredictionAttemptRepository
{
    Task AddAsync(FailedPredictionAttempt attempt, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<FailedPredictionAttempt> attempts, CancellationToken cancellationToken = default);
    Task<IEnumerable<FailedPredictionAttempt>> GetUnresolvedAttemptsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FailedPredictionAttempt>> GetUnresolvedAttemptsAsync(string targetMonth, CancellationToken cancellationToken = default);
    Task<FailedPredictionAttempt?> GetByIdAsync(FailedPredictionAttemptId id, CancellationToken cancellationToken = default);
    Task<FailedPredictionAttempt?> GetByUserAndMonthAsync(UserId userId, string targetMonth, CancellationToken cancellationToken = default);
    void Update(FailedPredictionAttempt attempt);
}
