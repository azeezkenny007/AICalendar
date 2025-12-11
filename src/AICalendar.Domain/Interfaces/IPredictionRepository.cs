using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Interfaces;

public interface IPredictionRepository
{
    Task<Prediction?> GetByIdAsync(PredictionId id, CancellationToken ct = default);
    Task<Prediction?> GetByItemIdAsync(PredictionItemId itemId, CancellationToken ct = default);
    Task<Prediction?> GetByUserAndCycleAsync(UserId userId, PredictionCycle cycle, CancellationToken ct = default);
    Task<List<Prediction>> GetByUserIdAsync(UserId userId, CancellationToken ct = default);
    Task<List<Prediction>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Prediction prediction, CancellationToken ct = default);
    Task UpdateAsync(Prediction prediction, CancellationToken ct = default);
    Task DeleteAsync(Prediction prediction, CancellationToken ct = default);
}
