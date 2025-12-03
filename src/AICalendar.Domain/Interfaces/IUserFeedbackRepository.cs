using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Interfaces;

public interface IUserFeedbackRepository
{
    Task<UserFeedback?> GetByIdAsync(UserFeedbackId id, CancellationToken cancellationToken = default);
    Task<List<UserFeedback>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<UserFeedback>> GetByPredictionIdAsync(PredictionId predictionId, CancellationToken cancellationToken = default);
    Task<List<UserFeedback>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserFeedback feedback, CancellationToken cancellationToken = default);
    Task AddRangeAsync(List<UserFeedback> feedbacks, CancellationToken cancellationToken = default);
}