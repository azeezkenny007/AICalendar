using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Services;

/// <summary>
/// Domain service for Calendar aggregate operations that require additional business logic
/// </summary>
public interface ICalendarDomainService
{
    /// <summary>
    /// Validates if a calendar item can be updated
    /// </summary>
    Result<bool> CanUpdateItem(Calendar calendar, CalendarItemId itemId);

    /// <summary>
    /// Validates if a calendar item can be removed
    /// </summary>
    Result<bool> CanRemoveItem(Calendar calendar, CalendarItemId itemId);

    /// <summary>
    /// Checks for duplicate calendar items based on merchant and due date
    /// </summary>
    Result<bool> IsDuplicateItem(Calendar calendar, string merchant, DateTime dueDate, CalendarItemId? excludeItemId = null);
}
