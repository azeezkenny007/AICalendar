# EF Core Interceptors

## Overview

**EF Core Interceptors** allow you to intercept database operations and add cross-cutting concerns like logging, auditing, and domain event processing **without modifying your business logic**.

---

## Implemented Interceptors

### 1. **OutboxInterceptor** (Critical)

**Purpose:** Converts domain events to outbox messages for reliable event processing.

**When it runs:** Before `SaveChangesAsync()`

**What it does:**
1. Finds all aggregates with domain events
2. Serializes events to JSON
3. Creates `OutboxMessage` records
4. Adds them to the database transaction
5. Clears events from aggregates

**Code:**
```csharp
public class OutboxInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        await ConvertDomainEventsToOutboxMessages(context, cancellationToken);
        return await base.SavingChangesAsync(...);
    }
}
```

**Benefits:**
- ✅ Loose coupling (UnitOfWork doesn't know about Outbox)
- ✅ Atomic (events saved in same transaction as aggregates)
- ✅ Reliable (events never lost)

---

### 2. **QueryLoggingInterceptor** (Optional - Development)

**Purpose:** Logs SQL queries and detects slow queries.

**When it runs:** After query execution

**What it does:**
1. Measures query execution time
2. Logs slow queries (> 1 second) as warnings
3. Logs normal queries as debug

**Code:**
```csharp
public class QueryLoggingInterceptor : DbCommandInterceptor
{
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(...)
    {
        if (eventData.Duration.TotalMilliseconds > 1000)
        {
            _logger.LogWarning("Slow query detected ({Duration}ms)", ...);
        }
        return await base.ReaderExecutedAsync(...);
    }
}
```

**Benefits:**
- ✅ Identify performance bottlenecks
- ✅ Debug N+1 query problems
- ✅ Monitor database performance

---

## Registration

### Program.cs

```csharp
// Register interceptors as singletons
builder.Services.AddSingleton<OutboxInterceptor>();
builder.Services.AddSingleton<QueryLoggingInterceptor>();

// Add to DbContext
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);

    // Add interceptors
    options.AddInterceptors(
        sp.GetRequiredService<OutboxInterceptor>(),
        sp.GetRequiredService<QueryLoggingInterceptor>()
    );
});
```

---

## Execution Order

When you call `SaveChangesAsync()`:

```
1. OutboxInterceptor.SavingChangesAsync()
   ├─ Converts domain events to outbox messages
   └─ Adds OutboxMessages to context

2. EF Core executes SQL transaction
   ├─ UPDATE Predictions...
   ├─ INSERT INTO OutboxMessages...
   └─ COMMIT

3. QueryLoggingInterceptor.ReaderExecutedAsync()
   └─ Logs query execution time
```

---

## Additional Interceptor Ideas

### Audit Interceptor

Track who created/modified entities:

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        var entries = context.ChangeTracker.Entries<IAuditable>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = _currentUser.UserId;
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedBy = _currentUser.UserId;
                entry.Entity.ModifiedAt = DateTime.UtcNow;
            }
        }

        return base.SavingChangesAsync(...);
    }
}
```

### Soft Delete Interceptor

Prevent hard deletes:

```csharp
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        var entries = context.ChangeTracker.Entries<ISoftDeletable>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SavingChangesAsync(...);
    }
}
```

### Validation Interceptor

Validate entities before saving:

```csharp
public class ValidationInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var validationContext = new ValidationContext(entry.Entity);
            Validator.ValidateObject(entry.Entity, validationContext, validateAllProperties: true);
        }

        return base.SavingChangesAsync(...);
    }
}
```

---

## Benefits of Interceptor Pattern

### ✅ **Separation of Concerns**
- Business logic stays clean
- Cross-cutting concerns in one place
- Easy to understand and maintain

### ✅ **Reusability**
- Write once, use everywhere
- No need to remember to call audit/logging code
- Consistent across all entities

### ✅ **Testability**
- Can test interceptors in isolation
- Can disable interceptors in tests
- Mock interceptors for unit tests

### ✅ **Flexibility**
- Easy to add/remove interceptors
- Can enable/disable based on environment
- Can chain multiple interceptors

---

## Best Practices

### DO ✅

1. **Keep interceptors focused** - One responsibility per interceptor
2. **Log appropriately** - Use correct log levels
3. **Handle errors gracefully** - Don't break SaveChanges
4. **Use dependency injection** - Inject services via constructor
5. **Register as singleton** - Interceptors should be stateless

### DON'T ❌

1. **Don't modify business logic** - Keep it in aggregates
2. **Don't make external calls** - Keep interceptors fast
3. **Don't store state** - Interceptors are singletons
4. **Don't throw exceptions** - Unless it's critical
5. **Don't do heavy computation** - It runs on every save

---

## Debugging

### Enable Detailed Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "AICalendar.Infrastructure.Persistence.Interceptors": "Debug"
    }
  }
}
```

### Disable Interceptor Temporarily

```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
}
```

---

## Summary

Interceptors provide a **clean, maintainable way** to add cross-cutting concerns to your application. The **OutboxInterceptor** is critical for reliable event processing, while other interceptors can add auditing, logging, and validation without polluting your business logic.

**Key Takeaway:** Interceptors run **automatically** on every database operation, ensuring consistency and reducing boilerplate code.
