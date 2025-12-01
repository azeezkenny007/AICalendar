# 📘 Domain Development & Event Integration Guide

**Target Audience:** Calendar Dev, UserFeedback Dev
**Purpose:** Standardize domain modeling and event consumption across the AICalendar project.

---

## 🏗️ Part 1: Domain Modeling Guidelines

We follow **Domain-Driven Design (DDD)** principles. Your domain logic should be pure, rich, and isolated from infrastructure concerns.

### 1. Folder Structure
Follow this structure for your feature:

```
src/AICalendar.Domain/
├── Aggregates/
│   └── CalendarAggregate/
│       ├── Calendar.cs          (Aggregate Root)
│       ├── CalendarItem.cs      (Entity)
│       └── CalendarId.cs        (Value Object)
├── Events/                      (Domain Events)
├── ValueObjects/                (Shared Value Objects)
└── Interfaces/                  (Repository Interfaces)
```

### 2. Value Objects (The Building Blocks)
- **Rule:** Use Value Objects for all IDs and complex properties.
- **Why:** Prevents "Primitive Obsession" (e.g., passing `Guid` everywhere).
- **Base Class:** Inherit from `ValueObject`.

```csharp
public class CalendarId : ValueObject
{
    public Guid Value { get; }

    private CalendarId(Guid value)
    {
        Value = value;
    }

    public static CalendarId Create(Guid value)
    {
        // Validation here
        return new CalendarId(value);
    }

    public static CalendarId CreateUnique() => new(Guid.NewGuid());

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### 3. Entities
- **Rule:** Entities have an identity (`Id`) and a lifecycle.
- **Base Class:** Inherit from `Entity<TId>`.

```csharp
public class CalendarItem : Entity<CalendarItemId>
{
    public string Title { get; private set; }
    public DateTime StartTime { get; private set; }

    // Private constructor for EF Core
    private CalendarItem() { }

    // Factory method
    public static CalendarItem Create(string title, DateTime startTime)
    {
        return new CalendarItem(CalendarItemId.CreateUnique(), title, startTime);
    }
}
```

### 4. Aggregate Roots
- **Rule:** The entry point for consistency. Only Aggregates can be loaded/saved via Repositories.
- **Base Class:** Inherit from `AggregateRoot<TId>`.
- **Logic:** Encapsulate all business rules here.

```csharp
public class Calendar : AggregateRoot<CalendarId>
{
    private readonly List<CalendarItem> _items = new();
    public IReadOnlyCollection<CalendarItem> Items => _items.AsReadOnly();

    public void AddItem(CalendarItem item)
    {
        // Enforce invariants
        if (_items.Any(i => i.StartTime == item.StartTime))
            throw new DomainException("Conflict detected");

        _items.Add(item);

        // Raise event
        AddDomainEvent(new CalendarItemAddedEvent(Id, item.Id));
    }
}
```

---

## 📡 Part 2: Consuming Domain Events (Outbox Pattern)

We use the **Outbox Pattern** to ensure reliable event delivery. When the `Prediction` module raises an event, you can react to it in your module.

### Available Events
| Event | Payload | Use Case |
|-------|---------|----------|
| `PredictionAcceptedEvent` | Merchant, Amount, Date | **Calendar:** Create Item<br>**Feedback:** Record Positive |
| `PredictionRejectedEvent` | Merchant, Reason | **Feedback:** Record Negative |
| `PredictionItemEditedEvent` | Old/New Values | **Feedback:** Record Correction (Critical for AI) |

### How to Create an Event Handler
Handlers run in the background (Hangfire). Implement `INotificationHandler<DomainEventNotification<T>>`.

**Example: Creating a Calendar Item from a Prediction**

```csharp
// src/AICalendar.Application/Features/Calendar/EventHandlers/PredictionAcceptedEventHandler.cs

using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;

public class PredictionAcceptedEventHandler
    : INotificationHandler<DomainEventNotification<PredictionAcceptedEvent>>
{
    private readonly ICalendarRepository _repository;
    private readonly ILogger _logger;

    public async Task Handle(
        DomainEventNotification<PredictionAcceptedEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        _logger.LogInformation("Processing accepted prediction {Id}", evt.ItemId);

        // 1. Idempotency Check (Prevent duplicates)
        if (await _repository.ExistsByPredictionIdAsync(evt.ItemId))
        {
            _logger.LogWarning("Calendar item already exists for prediction {Id}", evt.ItemId);
            return;
        }

        // 2. Create Aggregate
        var calendarItem = CalendarItem.Create(
            evt.Merchant,
            evt.Amount,
            evt.DueDate
        );

        // 3. Save
        await _repository.AddAsync(calendarItem, ct);
    }
}
```

---

## 💾 Part 3: Repositories & Persistence

### 1. Define Interface (Domain Layer)
Define what you need, not how it's stored.

```csharp
// src/AICalendar.Domain/Interfaces/ICalendarRepository.cs
public interface ICalendarRepository
{
    Task<Calendar?> GetByIdAsync(CalendarId id, CancellationToken ct = default);
    Task AddAsync(Calendar calendar, CancellationToken ct = default);
}
```

### 2. Implement (Infrastructure Layer)
Implement using EF Core.

```csharp
// src/AICalendar.Infrastructure/Persistence/Repositories/CalendarRepository.cs
public class CalendarRepository : ICalendarRepository
{
    private readonly ApplicationDbContext _context;

    public async Task AddAsync(Calendar calendar, CancellationToken ct)
    {
        await _context.Calendars.AddAsync(calendar, ct);
    }
}
```

### 3. Register (Program.cs)
```csharp
builder.Services.AddScoped<ICalendarRepository, CalendarRepository>();
```

---

## ✅ Checklist for New Features

1.  [ ] **Domain:** Define Aggregate, Entities, and Value Objects.
2.  [ ] **Domain:** Define Repository Interface.
3.  [ ] **Infrastructure:** Create EF Core Configuration (`IEntityTypeConfiguration`).
4.  [ ] **Infrastructure:** Implement Repository.
5.  [ ] **Application:** Create Command/Query Handlers (MediatR).
6.  [ ] **Application:** Create Event Handlers (if consuming events).
7.  [ ] **API:** Create Controller Endpoints.

---

## 💡 Best Practices

-   **Private Setters:** Properties should be `private set` to enforce encapsulation.
-   **Static Factory Methods:** Use `Create()` instead of public constructors.
-   **Rich Models:** Put logic in entities, not services.
-   **Pure Domain:** No external dependencies (MediatR, EF Core) in `AICalendar.Domain`.
