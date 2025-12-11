using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Services;

/// <summary>
/// Implementation of Calendar domain service
/// </summary>
public class CalendarDomainService : ICalendarDomainService
{
    public Result<bool> CanUpdateItem(Calendar calendar, CalendarItemId itemId)
    {
        var item = calendar.Items.FirstOrDefault(i => i.Id == itemId);

        if (item == null)
        {
            return Result<bool>.Failure($"Calendar item {itemId.Value} not found.");
        }

        if (item.IsPaid)
        {
            return Result<bool>.Failure("Cannot update a paid calendar item.");
        }

        return Result<bool>.Success(true);
    }

    public Result<bool> CanRemoveItem(Calendar calendar, CalendarItemId itemId)
    {
        var item = calendar.Items.FirstOrDefault(i => i.Id == itemId);

        if (item == null)
        {
            return Result<bool>.Failure($"Calendar item {itemId.Value} not found.");
        }

        return Result<bool>.Success(true);
    }

    public Result<bool> IsDuplicateItem(Calendar calendar, string merchant, DateTime dueDate, CalendarItemId? excludeItemId = null)
    {
        var duplicateExists = calendar.Items
            .Where(i => excludeItemId == null || i.Id != excludeItemId)
            .Any(i => i.Merchant.Equals(merchant, StringComparison.OrdinalIgnoreCase)
                   && i.DueDate.Date == dueDate.Date);

        if (duplicateExists)
        {
            return Result<bool>.Failure($"A calendar item for {merchant} on {dueDate:yyyy-MM-dd} already exists.");
        }

        return Result<bool>.Success(false);
    }
}
