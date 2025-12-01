# 📡 Domain Event Integration Guide

**Target Audience:** Calendar Dev, UserFeedback Dev
**Topic:** Consuming Domain Events via Outbox Pattern

---

## 🚀 Overview

We have implemented the **Outbox Pattern** to ensure reliable event delivery. When a domain event occurs (e.g., a prediction is accepted), it is saved to the database and then processed asynchronously by a background job.

As a developer, you don't need to worry about the outbox mechanism. You simply need to **create an Event Handler** to react to these events.

---

## 📋 Available Events

These events are defined in `src/AICalendar.Domain/Events/`.

### 1. `PredictionAcceptedEvent`
Triggered when a user accepts a prediction.
- **Payload:** `PredictionId`, `ItemId`, `UserId`, `Merchant`, `Amount`, `DueDate`, `WasEdited`, `OriginalAmount`, `OriginalDueDate`
- **Use Case:** Create Calendar Item, Record Positive Feedback

### 2. `PredictionRejectedEvent`
Triggered when a user rejects a prediction.
- **Payload:** `PredictionId`, `ItemId`, `UserId`, `Merchant`, `Amount`, `RejectedAt`
- **Use Case:** Record Negative Feedback, Update Analytics

### 3. `PredictionItemEditedEvent`
Triggered when a user edits a prediction before accepting.
- **Payload:** `PredictionId`, `ItemId`, `UserId`, `OriginalAmount`, `NewAmount`, `OriginalDueDate`, `NewDueDate`
- **Use Case:** Record Correction Feedback (Crucial for AI training)

### 4. `PredictionGeneratedEvent`
Triggered when new predictions are generated.
- **Payload:** `PredictionId`, `UserId`, `Cycle`, `ItemCount`
- **Use Case:** Send Notification, Analytics

---

## 🛠️ How to Create an Event Handler

We use **MediatR** for event handling. However, to keep our Domain layer pure, we wrap domain events in a `DomainEventNotification<T>`.

**Your handler must implement:**
`INotificationHandler<DomainEventNotification<YourEventType>>`

### 📝 Code Template

Create your handler in `src/AICalendar.Application/Features/[YourFeature]/EventHandlers/`.

```csharp
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Features.Calendar.EventHandlers;

public class PredictionAcceptedEventHandler
    : INotificationHandler<DomainEventNotification<PredictionAcceptedEvent>>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly ILogger<PredictionAcceptedEventHandler> _logger;

    public PredictionAcceptedEventHandler(
        ICalendarRepository calendarRepository,
        ILogger<PredictionAcceptedEventHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<PredictionAcceptedEvent> notification,
        CancellationToken cancellationToken)
    {
        // 1. Unwrap the domain event
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Handling PredictionAcceptedEvent for Item {ItemId}", domainEvent.ItemId);

        // 2. Implement your business logic
        // Example: Create a calendar item
        var calendarItem = CalendarItem.Create(
            domainEvent.UserId,
            domainEvent.Merchant,
            domainEvent.Amount,
            domainEvent.DueDate
        );

        await _calendarRepository.AddAsync(calendarItem, cancellationToken);

        // Note: Changes are saved automatically by UnitOfWork if this runs in a command context,
        // but since this is a background job, you might need to call SaveChanges explicitly
        // depending on your repository implementation.
    }
}
```

---

## 👷 Instructions for **Calendar Dev**

**Goal:** When a user accepts a prediction, it must become a real item on their calendar.

1.  **Subscribe to:** `PredictionAcceptedEvent`
2.  **Action:**
    *   Create a new `CalendarItem` entity.
    *   Map `Merchant`, `Amount`, `DueDate` from the event.
    *   Link it to the `UserId`.
    *   Save to `CalendarRepository`.
3.  **Note:** Ensure you handle duplicates (Idempotency). If the event is processed twice, don't create two calendar items. Use `PredictionItemId` as a reference or idempotency key.

---

## 👷 Instructions for **UserFeedback Dev**

**Goal:** Collect data to retrain the AI model. We need to know what the AI got right, what it got wrong, and how the user corrected it.

### Task 1: Handle Acceptance
1.  **Subscribe to:** `PredictionAcceptedEvent`
2.  **Action:** Create a `UserFeedback` entry with `Type = Positive`.
3.  **Data:** Store the `Merchant`, `Amount`, and `ConfidenceScore` (if available via lookup).

### Task 2: Handle Rejection
1.  **Subscribe to:** `PredictionRejectedEvent`
2.  **Action:** Create a `UserFeedback` entry with `Type = Negative`.
3.  **Data:** This tells the AI "This pattern is wrong".

### Task 3: Handle Edits (High Value)
1.  **Subscribe to:** `PredictionItemEditedEvent`
2.  **Action:** Create a `UserFeedback` entry with `Type = Correction`.
3.  **Data:** Store both `Original` values (what AI guessed) and `New` values (what user wanted).
4.  **Why:** This is the most valuable data for training. It teaches the AI specifically *how* it was wrong (e.g., "Netflix is $15.99, not $12.99").

---

## ⚠️ Important Notes

1.  **Async & Background:** These handlers run in a background job (Hangfire). They do **not** block the user's HTTP request.
2.  **Retries:** If your handler throws an exception, Hangfire will retry it automatically. Ensure your logic is **Idempotent** (safe to run multiple times).
3.  **Logging:** Always log the start and completion of your handler.
4.  **Dependency Injection:** All your repositories and services are available via constructor injection.
