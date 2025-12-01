# Outbox Pattern Implementation

## Overview

The **Outbox Pattern** ensures reliable processing of domain events in a distributed system. It guarantees that domain events are never lost, even if the event processing fails.

---

## How It Works

### 1. **Domain Event Raised**
When business logic executes (e.g., user accepts a prediction), the aggregate raises a domain event:

```csharp
// In Prediction.cs
public Result AcceptItem(PredictionItemId itemId)
{
    // ... business logic ...

    AddDomainEvent(new PredictionAcceptedEvent(
        PredictionId,
        itemId,
        item.Merchant,
        item.Amount,
        item.DueDate
    ));

    return Result.Success();
}
```

### 2. **UnitOfWork Converts Events to Outbox Messages**
When `SaveChangesAsync()` is called, the UnitOfWork:
1. Collects all domain events from aggregates
2. Serializes them to JSON
3. Stores them in the `OutboxMessages` table
4. Clears events from aggregates
5. Saves everything in **ONE database transaction**

```csharp
// In UnitOfWork.cs
public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Convert domain events to outbox messages
    await ConvertDomainEventsToOutboxMessages(cancellationToken);

    // Save aggregate + outbox messages in ONE transaction
    return await _context.SaveChangesAsync(cancellationToken);
}
```

### 3. **Hangfire Processes Outbox Messages**
Every 10 seconds, the `OutboxProcessorJob` runs:
1. Reads unprocessed messages from `OutboxMessages` table
2. Deserializes the JSON back to domain events
3. Publishes events to MediatR
4. Marks messages as processed

```csharp
// In OutboxProcessorJob.cs
public async Task ProcessOutboxMessages()
{
    var messages = await _context.Set<OutboxMessage>()
        .Where(m => m.ProcessedOnUtc == null)
        .OrderBy(m => m.OccurredOnUtc)
        .Take(20)
        .ToListAsync();

    foreach (var message in messages)
    {
        var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
        await _publisher.Publish(domainEvent);
        message.ProcessedOnUtc = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();
}
```

### 4. **Event Handlers Execute**
MediatR publishes the event to **ALL** registered handlers:

```csharp
// CalendarEventHandler.cs
public class PredictionAcceptedEventHandler : INotificationHandler<PredictionAcceptedEvent>
{
    public async Task Handle(PredictionAcceptedEvent notification, CancellationToken ct)
    {
        // Create calendar item
        var calendarItem = CalendarItem.Create(...);
        await _calendarRepository.AddAsync(calendarItem, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

// UserFeedbackEventHandler.cs
public class UserFeedbackEventHandler : INotificationHandler<PredictionAcceptedEvent>
{
    public async Task Handle(PredictionAcceptedEvent notification, CancellationToken ct)
    {
        // Record feedback for AI
        var feedback = UserFeedback.Create(...);
        await _feedbackRepository.AddAsync(feedback, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

---

## Benefits

### ✅ **Reliability**
- Events are stored in the database in the **same transaction** as the aggregate
- If the transaction fails, neither the aggregate nor the events are saved
- If the transaction succeeds, events are **guaranteed** to be processed eventually

### ✅ **Decoupling**
- Aggregates don't know about event handlers
- Event handlers don't know about each other
- Easy to add new handlers without changing existing code

### ✅ **Retry Logic**
- Failed events are automatically retried (up to 5 times)
- Errors are logged for debugging
- Failed events don't block other events

### ✅ **Scalability**
- Events are processed in batches (20 at a time)
- Can scale horizontally by adding more Hangfire workers
- Outbox table is indexed for fast queries

---

## Database Schema

### OutboxMessages Table

| Column | Type | Description |
|--------|------|-------------|
| `Id` | GUID | Primary key |
| `Type` | NVARCHAR(500) | Fully qualified type name (e.g., `AICalendar.Domain.Events.PredictionAcceptedEvent`) |
| `Content` | NVARCHAR(MAX) | JSON serialized event data |
| `OccurredOnUtc` | DATETIME | When the event was raised |
| `ProcessedOnUtc` | DATETIME (nullable) | When the event was successfully processed |
| `Error` | NVARCHAR(2000) | Error message if processing failed |
| `RetryCount` | INT | Number of processing attempts |

**Index:** `IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc` for efficient querying of unprocessed messages.

---

## Example Flow

### User Accepts Prediction

```
1. User clicks "Keep" on prediction
         ↓
2. API: POST /api/predictions/{id}/items/{itemId}/accept
         ↓
3. AcceptPredictionItemCommandHandler
   - Loads Prediction aggregate
   - Calls prediction.AcceptItem(itemId)
   - Calls _unitOfWork.SaveChangesAsync()
         ↓
4. UnitOfWork.SaveChangesAsync()
   - Finds PredictionAcceptedEvent in aggregate
   - Creates OutboxMessage:
     {
       "Id": "abc123...",
       "Type": "AICalendar.Domain.Events.PredictionAcceptedEvent",
       "Content": "{\"PredictionId\":\"...\",\"ItemId\":\"...\"}",
       "OccurredOnUtc": "2025-12-01T06:00:00Z",
       "ProcessedOnUtc": null
     }
   - Saves Prediction + OutboxMessage in ONE transaction
         ↓
5. Hangfire (10 seconds later)
   - OutboxProcessorJob runs
   - Reads OutboxMessage
   - Deserializes PredictionAcceptedEvent
   - Publishes to MediatR
         ↓
6. Event Handlers Execute (in parallel)
   - PredictionAcceptedEventHandler → Creates CalendarItem
   - UserFeedbackEventHandler → Creates UserFeedback
         ↓
7. OutboxMessage marked as processed
   - ProcessedOnUtc = "2025-12-01T06:00:10Z"
```

---

## Configuration

### appsettings.json

```json
{
  "BackgroundJobs": {
    "ProcessOutbox": {
      "CronExpression": "*/10 * * * * *",
      "Description": "Process domain events every 10 seconds"
    },
    "CleanupOutbox": {
      "CronExpression": "0 4 * * 0",
      "Description": "Cleanup old messages weekly on Sunday at 4 AM"
    }
  }
}
```

### Hangfire Configuration

```csharp
// In HangfireConfiguration.cs
RecurringJob.AddOrUpdate<OutboxProcessorJob>(
    "process-outbox-messages",
    job => job.ProcessOutboxMessages(),
    "*/10 * * * * *",
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc,
        Queue = "critical"
    });
```

---

## Monitoring

### Hangfire Dashboard

Access: `https://your-domain/hangfire`

**Metrics to watch:**
- **Succeeded Jobs:** Should be high
- **Failed Jobs:** Should be low (investigate failures)
- **Processing Time:** Should be < 1 second per batch
- **Queue Length:** Should be near 0 (if growing, increase workers)

### Database Queries

**Check unprocessed messages:**
```sql
SELECT COUNT(*)
FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL;
```

**Check failed messages:**
```sql
SELECT *
FROM OutboxMessages
WHERE Error IS NOT NULL
ORDER BY OccurredOnUtc DESC;
```

**Check processing lag:**
```sql
SELECT
    DATEDIFF(SECOND, OccurredOnUtc, GETUTCDATE()) AS LagSeconds
FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL
ORDER BY OccurredOnUtc ASC;
```

---

## Troubleshooting

### Events Not Processing

**Symptoms:** `ProcessedOnUtc` stays `NULL`

**Causes:**
1. Hangfire job not running
2. Deserialization error
3. Event handler throwing exception

**Solution:**
1. Check Hangfire Dashboard
2. Check application logs for errors
3. Check `Error` column in OutboxMessages table

### Duplicate Event Processing

**Symptoms:** Event handlers execute multiple times

**Causes:**
1. Outbox job running on multiple servers
2. Transaction not committed properly

**Solution:**
1. Ensure Hangfire is configured for distributed locks
2. Check database transaction isolation level

### Outbox Table Growing Too Large

**Symptoms:** Millions of rows in OutboxMessages

**Causes:**
1. Cleanup job not running
2. Retention period too long

**Solution:**
1. Check Hangfire Dashboard for CleanupOutboxJob
2. Reduce retention period in cleanup job
3. Manually delete old messages:
   ```sql
   DELETE FROM OutboxMessages
   WHERE ProcessedOnUtc < DATEADD(DAY, -7, GETUTCDATE());
   ```

---

## Best Practices

### ✅ DO

- Keep events small (< 10 KB)
- Use specific event names (e.g., `PredictionAcceptedEvent`, not `PredictionEvent`)
- Log event processing for debugging
- Monitor outbox table size
- Set up alerts for failed messages

### ❌ DON'T

- Don't store large payloads in events (use IDs instead)
- Don't process events synchronously in the aggregate
- Don't delete outbox messages immediately after processing
- Don't skip the outbox for "important" events (use it for ALL events)

---

## Migration Guide

### Create Migration

```bash
dotnet ef migrations add AddOutboxTable --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

### Apply Migration

```bash
dotnet ef database update --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

---

## Summary

The Outbox Pattern ensures **reliable, decoupled, and scalable** event processing. It's a critical piece of infrastructure that makes your system resilient to failures and easy to extend.

**Key Components:**
1. **OutboxMessage** - Entity for storing events
2. **UnitOfWork** - Converts domain events to outbox messages
3. **OutboxProcessorJob** - Hangfire job that processes events
4. **CleanupOutboxJob** - Removes old processed messages

**Flow:**
Domain Event → Outbox Table → Hangfire Job → MediatR → Event Handlers
