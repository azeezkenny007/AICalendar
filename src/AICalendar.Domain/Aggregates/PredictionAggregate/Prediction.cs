using AICalendar.Domain.Common;
using AICalendar.Domain.Entities;
using AICalendar.Domain.Enums;
using AICalendar.Domain.Events;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Aggregates.PredictionAggregate;

public class Prediction : AggregateRoot<PredictionId>
{
    private readonly List<PredictionItem> _items = new();

    public PredictionId PredictionId { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public PredictionCycle Cycle { get; private set; } = default!;
    public PredictionStatus Status { get; private set; }
    public IReadOnlyCollection<PredictionItem> Items => _items.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core
    private Prediction() { }

    private Prediction(UserId userId, PredictionCycle cycle)
    {
        PredictionId = PredictionId.Create();
        UserId = userId;
        Cycle = cycle;
        Status = PredictionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static Prediction Create(UserId userId, PredictionCycle cycle)
    {
        return new Prediction(userId, cycle);
    }

    public Result AddItem(PredictionItem item)
    {
        if (_items.Count >= 10)
        {
            return Result.Failure("Cannot add more than 10 prediction items.");
        }

        if (Status != PredictionStatus.Pending && Status != PredictionStatus.Generated)
        {
            return Result.Failure($"Cannot add items when status is {Status}");
        }

        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result MarkAsGenerated()
    {
        if (Status != PredictionStatus.Pending)
        {
            return Result.Failure("Prediction is already generated or processed.");
        }

        Status = PredictionStatus.Generated;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PredictionGeneratedEvent(
            PredictionId,
            UserId,
            Cycle,
            _items.Count,
            DateTime.UtcNow
        ));

        return Result.Success();
    }

    public Result AcceptItem(PredictionItemId itemId)
    {
        var item = _items.FirstOrDefault(x => x.Id.Value == itemId.Value);
        if (item == null)
        {
            return Result.Failure("Item not found.");
        }

        if (Status == PredictionStatus.Expired || Status == PredictionStatus.Failed)
        {
            return Result.Failure($"Cannot accept items when status is {Status}");
        }

        item.Accept();
        Status = PredictionStatus.Reviewing;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PredictionAcceptedEvent(
            PredictionId,
            itemId,
            UserId,
            item.Merchant,
            item.Amount,
            item.DueDate,
            item.IsEdited,
            item.OriginalAmount,
            item.OriginalDueDate,
            DateTime.UtcNow
        ));

        CheckIfCompleted();

        return Result.Success();
    }

    public Result RejectItem(PredictionItemId itemId)
    {
        var item = _items.FirstOrDefault(x => x.Id.Value == itemId.Value);
        if (item == null)
        {
            return Result.Failure("Item not found.");
        }

        if (Status == PredictionStatus.Expired || Status == PredictionStatus.Failed)
        {
            return Result.Failure($"Cannot reject items when status is {Status}");
        }

        item.Reject();
        Status = PredictionStatus.Reviewing;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PredictionRejectedEvent(
            PredictionId,
            itemId,
            UserId,
            item.Merchant,
            item.Amount,
            item.DueDate,
            DateTime.UtcNow
        ));

        CheckIfCompleted();

        return Result.Success();
    }

    public Result EditItem(PredictionItemId itemId, string merchant, decimal amount, DateTime dueDate)
    {
        var item = _items.FirstOrDefault(x => x.Id.Value == itemId.Value);
        if (item == null)
        {
            return Result.Failure("Item not found.");
        }

        if (Status == PredictionStatus.Expired || Status == PredictionStatus.Failed)
        {
            return Result.Failure($"Cannot edit items when status is {Status}");
        }

        var originalAmount = item.Amount;
        var originalDueDate = item.DueDate;

        var result = item.Edit(merchant, amount, dueDate);
        if (!result.IsSuccess)
        {
            return result;
        }

        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PredictionItemEditedEvent(
            PredictionId,
            itemId,
            UserId,
            merchant,
            originalAmount,
            amount,
            originalDueDate,
            dueDate,
            DateTime.UtcNow
        ));

        return Result.Success();
    }

    private void CheckIfCompleted()
    {
        if (_items.All(i => i.IsAccepted.HasValue))
        {
            Status = PredictionStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
