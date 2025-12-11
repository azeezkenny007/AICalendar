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

    /// <summary>
    /// Gets all unpaid calendar items across all users with their associated user IDs
    /// </summary>
    Task<List<(CalendarItem Item, UserId UserId)>> GetUnpaidItemsWithDueDatesAsync(CancellationToken cancellationToken = default);
}
