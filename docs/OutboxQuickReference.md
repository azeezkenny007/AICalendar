# Outbox Pattern - Quick Reference

## 🎯 What is it?

The **Outbox Pattern** ensures domain events are **never lost** by storing them in the database in the **same transaction** as the aggregate changes.

---

## 🔄 Flow

```
User Action
    ↓
Command Handler
    ↓
Aggregate.DoSomething()
    ├─ Changes state
    └─ Raises domain event (AddDomainEvent)
    ↓
_unitOfWork.SaveChangesAsync()
    ↓
OutboxInterceptor (runs automatically)
    ├─ Finds domain events
    ├─ Serializes to JSON
    └─ Creates OutboxMessage records
    ↓
Database Transaction (ATOMIC)
    ├─ UPDATE Aggregates
    └─ INSERT OutboxMessages
    ↓
Hangfire (every 10 seconds)
    ├─ Reads OutboxMessages
    ├─ Deserializes events
    └─ Publishes to MediatR
    ↓
Event Handlers Execute
```

---

## 📝 How to Use

### 1. Raise Domain Event in Aggregate

```csharp
public class Prediction : AggregateRoot<PredictionId>
{
    public Result AcceptItem(PredictionItemId itemId)
    {
        // Business logic
        item.Accept();
        Status = PredictionStatus.Reviewing;

        // Raise domain event
        AddDomainEvent(new PredictionAcceptedEvent(
            PredictionId,
            itemId,
            item.Merchant,
            item.Amount
        ));

        return Result.Success();
    }
}
```

### 2. Save Changes in Command Handler

```csharp
public class AcceptPredictionItemCommandHandler
{
    public async Task<Result> Handle(...)
    {
        var prediction = await _repository.GetByIdAsync(...);

        var result = prediction.AcceptItem(itemId);
        if (!result.IsSuccess)
            return result;

        // OutboxInterceptor runs automatically here
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}
```

### 3. Create Event Handler

```csharp
public class PredictionAcceptedEventHandler
    : INotificationHandler<PredictionAcceptedEvent>
{
    public async Task Handle(
        PredictionAcceptedEvent notification,
        CancellationToken ct)
    {
        // This runs when Hangfire processes the outbox
        var calendarItem = CalendarItem.Create(...);
        await _repository.AddAsync(calendarItem, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "BackgroundJobs": {
    "ProcessOutbox": {
      "CronExpression": "*/10 * * * * *"
    }
  }
}
```

### Program.cs

```csharp
// Register interceptor
builder.Services.AddSingleton<OutboxInterceptor>();

// Add to DbContext
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
});

// Register Hangfire job
builder.Services.AddScoped<OutboxProcessorJob>();
```

---

## 🔍 Monitoring

### Check Unprocessed Messages

```sql
SELECT COUNT(*)
FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL;
```

### Check Failed Messages

```sql
SELECT *
FROM OutboxMessages
WHERE Error IS NOT NULL
ORDER BY OccurredOnUtc DESC;
```

### Check Processing Lag

```sql
SELECT
    DATEDIFF(SECOND, OccurredOnUtc, GETUTCDATE()) AS LagSeconds
FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL
ORDER BY OccurredOnUtc ASC;
```

---

## 🐛 Troubleshooting

### Events Not Processing

**Problem:** `ProcessedOnUtc` stays `NULL`

**Solutions:**
1. Check Hangfire Dashboard (`/hangfire`)
2. Check `OutboxProcessorJob` is scheduled
3. Check application logs for errors
4. Check `Error` column in `OutboxMessages`

### Duplicate Events

**Problem:** Event handlers execute multiple times

**Solutions:**
1. Make handlers idempotent
2. Check Hangfire distributed locks
3. Verify transaction isolation level

### Outbox Table Growing

**Problem:** Millions of rows in `OutboxMessages`

**Solutions:**
1. Check `CleanupOutboxJob` is running
2. Reduce retention period (default 7 days)
3. Manually delete old messages

---

## ✅ Best Practices

### DO

- ✅ Keep events small (< 10 KB)
- ✅ Make event handlers idempotent
- ✅ Use specific event names
- ✅ Log event processing
- ✅ Monitor outbox table size

### DON'T

- ❌ Store large payloads in events
- ❌ Process events synchronously
- ❌ Delete outbox messages immediately
- ❌ Skip outbox for "important" events
- ❌ Modify events after creation

---

## 📊 Key Metrics

| Metric | Target | Action if Exceeded |
|--------|--------|-------------------|
| Unprocessed messages | < 100 | Increase Hangfire workers |
| Processing lag | < 30 sec | Check for errors |
| Failed messages | < 1% | Investigate errors |
| Table size | < 1M rows | Reduce retention period |
| Processing time | < 1 sec | Optimize event handlers |

---

## 🚀 Quick Commands

### Create Migration

```bash
dotnet ef migrations add AddOutboxTable --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

### Apply Migration

```bash
dotnet ef database update --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

### View Hangfire Dashboard

```
https://localhost:5001/hangfire
```

---

## 📚 Related Docs

- [OutboxPattern.md](OutboxPattern.md) - Detailed explanation
- [Interceptors.md](Interceptors.md) - EF Core interceptors
- [OutboxRefactoring.md](OutboxRefactoring.md) - Before/after comparison
- [BackgroundJobs.md](BackgroundJobs.md) - Hangfire configuration

---

## 💡 Remember

**The Outbox Pattern runs automatically!**

You don't need to do anything special - just:
1. Raise domain events in aggregates
2. Call `SaveChangesAsync()`
3. Create event handlers

The rest is handled by the interceptor and Hangfire! 🎉
