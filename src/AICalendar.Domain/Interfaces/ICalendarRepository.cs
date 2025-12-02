using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Interfaces;

public interface ICalendarRepository
{
    Task<Calendar?> GetByIdAsync(CalendarId id, CancellationToken cancellationToken = default);
    Task<Calendar?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<Calendar>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Calendar calendar, CancellationToken cancellationToken = default);
    Task UpdateAsync(Calendar calendar, CancellationToken cancellationToken = default);
    Task DeleteAsync(Calendar calendar, CancellationToken cancellationToken = default);
}
