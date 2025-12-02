using AICalendar.Domain.Common;
using AICalendar.Domain.Events;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Aggregates.CalendarAggregate;

/// <summary>
/// Calendar aggregate root - represents a user's calendar containing all their scheduled items
/// </summary>
public class Calendar : AggregateRoot<CalendarId>
{
    private readonly List<CalendarItem> _items = new();

    public UserId UserId { get; private set; } = default!;
    public IReadOnlyCollection<CalendarItem> Items => _items.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core
    private Calendar() { }

    private Calendar(UserId userId)
    {
        Id = CalendarId.Create();
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public static Calendar Create(UserId userId)
    {
        var calendar = new Calendar(userId);

        calendar.AddDomainEvent(new CalendarCreatedEvent(
            calendar.Id,
            userId,
            DateTime.UtcNow
        ));

        return calendar;
    }

    /// <summary>
    /// Adds a new calendar item from an accepted prediction
    /// </summary>
    public Result AddItemFromPrediction(
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account = null,
        string? accountName = null,
        string? description = null)
    {
        // Check for duplicates
        if (_items.Any(i => i.PredictionItemId == predictionItemId))
        {
            return Result.Failure($"Calendar item for prediction {predictionItemId.Value} already exists.");
        }

        var item = CalendarItem.CreateFromPrediction(
            predictionItemId,
            merchant,
            amount,
            dueDate,
            account,
            accountName,
            description
        );

        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CalendarItemAddedEvent(
            Id,
            item.Id,
            UserId,
            predictionItemId,
            merchant,
            amount,
            dueDate,
            DateTime.UtcNow
        ));

        return Result.Success();
    }

    /// <summary>
    /// Marks a calendar item as paid/completed
    /// </summary>
    public Result MarkItemAsPaid(CalendarItemId itemId, DateTime paidDate)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            return Result.Failure($"Calendar item {itemId.Value} not found.");
        }

        var result = item.MarkAsPaid(paidDate);
        if (!result.IsSuccess)
        {
            return result;
        }

        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CalendarItemPaidEvent(
            Id,
            itemId,
            UserId,
            paidDate,
            DateTime.UtcNow
        ));

        return Result.Success();
    }

    /// <summary>
    /// Updates an existing calendar item
    /// </summary>
    public Result UpdateItem(
        CalendarItemId itemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account = null,
        string? accountName = null,
        string? description = null)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            return Result.Failure($"Calendar item {itemId.Value} not found.");
        }

        var result = item.Update(merchant, amount, dueDate, account, accountName, description);
        if (!result.IsSuccess)
        {
            return result;
        }

        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CalendarItemUpdatedEvent(
            Id,
            itemId,
            UserId,
            merchant,
            amount,
            dueDate,
            account,
            accountName,
            description,
            DateTime.UtcNow
        ));

        return Result.Success();
    }

    /// <summary>
    /// Removes a calendar item (e.g., user cancelled subscription)
    /// </summary>
    public Result RemoveItem(CalendarItemId itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            return Result.Failure($"Calendar item {itemId.Value} not found.");
        }

        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CalendarItemRemovedEvent(
            Id,
            itemId,
            UserId,
            DateTime.UtcNow
        ));

        return Result.Success();
    }
}
