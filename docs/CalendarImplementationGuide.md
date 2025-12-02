# Calendar Aggregate Implementation Guide

## Overview
This guide will walk you through implementing the **Calendar Aggregate** to handle accepted prediction items. The Calendar system consumes domain events raised by the Prediction aggregate and creates calendar entries for tracking upcoming bills, subscriptions, and transfers.

---

## 🎯 Objective
Build a complete Calendar aggregate that:
- Listens to `PredictionBatchAcceptedEvent` from the Prediction aggregate
- Creates `CalendarItem` entries for each accepted prediction
- Manages reminders for upcoming due dates
- Provides query capabilities for user calendars

---

## 📋 Table of Contents
1. [Domain Layer: Aggregates & Entities](#1-domain-layer-aggregates--entities)
2. [Domain Layer: Value Objects](#2-domain-layer-value-objects)
3. [Domain Layer: Events](#3-domain-layer-events)
4. [Domain Layer: Repository Interface](#4-domain-layer-repository-interface)
5. [Application Layer: Event Handler](#5-application-layer-event-handler)
6. [Application Layer: Commands](#6-application-layer-commands)
7. [Application Layer: Queries](#7-application-layer-queries)
8. [Infrastructure Layer: Repository](#8-infrastructure-layer-repository)
9. [Infrastructure Layer: EF Configuration](#9-infrastructure-layer-ef-configuration)
10. [API Layer: Controller](#10-api-layer-controller)
11. [Testing the Integration](#11-testing-the-integration)

---

## 1. Domain Layer: Aggregates & Entities

### 📁 File: `src/AICalendar.Domain/Aggregates/CalendarAggregate/Calendar.cs`

```csharp
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
```

### 📁 File: `src/AICalendar.Domain/Aggregates/CalendarAggregate/CalendarItem.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Aggregates.CalendarAggregate;

/// <summary>
/// Represents a single scheduled item in the calendar (bill, subscription, transfer)
/// </summary>
public class CalendarItem : Entity<CalendarItemId>
{
    public PredictionItemId PredictionItemId { get; private set; } = default!;
    public string Merchant { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }

    // Optional fields for transfers/bills
    public string? Account { get; private set; }
    public string? AccountName { get; private set; }
    public string? Description { get; private set; }

    public bool IsPaid { get; private set; }
    public DateTime? PaidDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private CalendarItem() { }

    private CalendarItem(
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account,
        string? accountName,
        string? description)
        : base(CalendarItemId.Create())
    {
        PredictionItemId = predictionItemId;
        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        Account = account;
        AccountName = accountName;
        Description = description;
        IsPaid = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static CalendarItem CreateFromPrediction(
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        string? account = null,
        string? accountName = null,
        string? description = null)
    {
        return new CalendarItem(
            predictionItemId,
            merchant,
            amount,
            dueDate,
            account,
            accountName,
            description
        );
    }

    public Result MarkAsPaid(DateTime paidDate)
    {
        if (IsPaid)
        {
            return Result.Failure("Item is already marked as paid.");
        }

        IsPaid = true;
        PaidDate = paidDate;

        return Result.Success();
    }
}
```

---

## 2. Domain Layer: Value Objects

### 📁 File: `src/AICalendar.Domain/ValueObjects/CalendarId.cs`

```csharp
using System.Text.Json.Serialization;

namespace AICalendar.Domain.ValueObjects;

public record CalendarId
{
    public Guid Value { get; init; }

    private CalendarId() { }

    [JsonConstructor]
    public CalendarId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CalendarId cannot be empty", nameof(value));

        Value = value;
    }

    public static CalendarId Create() => new(Guid.NewGuid());
    public static CalendarId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CalendarId id) => id.Value;
}
```

### 📁 File: `src/AICalendar.Domain/ValueObjects/CalendarItemId.cs`

```csharp
using System.Text.Json.Serialization;

namespace AICalendar.Domain.ValueObjects;

public record CalendarItemId
{
    public Guid Value { get; init; }

    private CalendarItemId() { }

    [JsonConstructor]
    public CalendarItemId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CalendarItemId cannot be empty", nameof(value));

        Value = value;
    }

    public static CalendarItemId Create() => new(Guid.NewGuid());
    public static CalendarItemId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CalendarItemId id) => id.Value;
}
```

---

## 3. Domain Layer: Events

### 📁 File: `src/AICalendar.Domain/Events/CalendarCreatedEvent.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarCreatedEvent(
    CalendarId CalendarId,
    UserId UserId,
    DateTime OccurredOn
) : IDomainEvent;
```

### 📁 File: `src/AICalendar.Domain/Events/CalendarItemAddedEvent.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarItemAddedEvent(
    CalendarId CalendarId,
    CalendarItemId CalendarItemId,
    UserId UserId,
    PredictionItemId PredictionItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    DateTime OccurredOn
) : IDomainEvent;
```

### 📁 File: `src/AICalendar.Domain/Events/CalendarItemPaidEvent.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarItemPaidEvent(
    CalendarId CalendarId,
    CalendarItemId CalendarItemId,
    UserId UserId,
    DateTime PaidDate,
    DateTime OccurredOn
) : IDomainEvent;
```

### 📁 File: `src/AICalendar.Domain/Events/CalendarItemRemovedEvent.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record CalendarItemRemovedEvent(
    CalendarId CalendarId,
    CalendarItemId CalendarItemId,
    UserId UserId,
    DateTime OccurredOn
) : IDomainEvent;
```

---

## 4. Domain Layer: Repository Interface

### 📁 File: `src/AICalendar.Domain/Interfaces/ICalendarRepository.cs`

```csharp
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
}
```

---

## 5. Application Layer: Event Handler

### 📁 File: `src/AICalendar.Application/Predictions/DomainEventHandlers/PredictionAcceptedCalendarHandler.cs`

**Replace the existing TODO implementation with:**

```csharp
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.Events;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionAcceptedCalendarHandler : INotificationHandler<DomainEventNotification<PredictionBatchAcceptedEvent>>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PredictionAcceptedCalendarHandler> _logger;

    public PredictionAcceptedCalendarHandler(
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ILogger<PredictionAcceptedCalendarHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PredictionBatchAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "CALENDAR: Handling batch acceptance for Prediction {PredictionId}. {Count} items accepted.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        // Get or create calendar for user
        var calendar = await _calendarRepository.GetByUserIdAsync(domainEvent.UserId, cancellationToken);

        if (calendar == null)
        {
            _logger.LogInformation("CALENDAR: Creating new calendar for user {UserId}", domainEvent.UserId.Value);
            calendar = Calendar.Create(domainEvent.UserId);
            await _calendarRepository.AddAsync(calendar, cancellationToken);
        }

        // Add each accepted item to the calendar
        foreach (var item in domainEvent.Items)
        {
            _logger.LogInformation(
                "CALENDAR: Adding item {ItemId} to Calendar: {Merchant} - ${Amount} due on {DueDate}",
                item.ItemId.Value,
                item.Merchant,
                item.Amount,
                item.DueDate.ToString("yyyy-MM-dd")
            );

            var result = calendar.AddItemFromPrediction(
                item.ItemId,
                item.Merchant,
                item.Amount,
                item.DueDate
            );

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "CALENDAR: Failed to add item {ItemId}: {Error}",
                    item.ItemId.Value,
                    result.Error
                );
            }
        }

        // Save changes (this will also trigger OutboxInterceptor to save domain events)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "CALENDAR: Successfully processed {Count} items for user {UserId}",
            domainEvent.Items.Count,
            domainEvent.UserId.Value
        );
    }
}
```

---

## 6. Application Layer: Commands

### 📁 File: `src/AICalendar.Application/Calendar/Commands/MarkItemAsPaid/MarkItemAsPaidCommand.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.MarkItemAsPaid;

public record MarkItemAsPaidCommand(
    CalendarItemId ItemId,
    DateTime PaidDate
) : IRequest<Result>;
```

### 📁 File: `src/AICalendar.Application/Calendar/Commands/MarkItemAsPaid/MarkItemAsPaidCommandHandler.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.Commands.MarkItemAsPaid;

public class MarkItemAsPaidCommandHandler : IRequestHandler<MarkItemAsPaidCommand, Result>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkItemAsPaidCommandHandler> _logger;

    public MarkItemAsPaidCommandHandler(
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ILogger<MarkItemAsPaidCommandHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkItemAsPaidCommand request, CancellationToken cancellationToken)
    {
        // Find the calendar containing this item
        var calendars = await _calendarRepository.GetAllAsync(cancellationToken);
        var calendar = calendars.FirstOrDefault(c => c.Items.Any(i => i.Id == request.ItemId));

        if (calendar == null)
        {
            return Result.Failure($"Calendar item {request.ItemId.Value} not found.");
        }

        var result = calendar.MarkItemAsPaid(request.ItemId, request.PaidDate);
        if (!result.IsSuccess)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Calendar item {ItemId} marked as paid on {PaidDate}",
            request.ItemId.Value,
            request.PaidDate
        );

        return Result.Success();
    }
}
```

---

## 7. Application Layer: Queries

### 📁 File: `src/AICalendar.Application/Calendar/Queries/GetUserCalendar/GetUserCalendarQuery.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public record GetUserCalendarQuery(UserId UserId) : IRequest<Result<CalendarDto>>;
```

### 📁 File: `src/AICalendar.Application/Calendar/Queries/GetUserCalendar/CalendarDto.cs`

```csharp
namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public record CalendarDto(
    Guid CalendarId,
    Guid UserId,
    List<CalendarItemDto> Items,
    DateTime CreatedAt
);

public record CalendarItemDto(
    Guid ItemId,
    Guid PredictionItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    string? Account,
    string? AccountName,
    string? Description,
    bool IsPaid,
    DateTime? PaidDate,
    DateTime CreatedAt
);
```

### 📁 File: `src/AICalendar.Application/Calendar/Queries/GetUserCalendar/GetUserCalendarQueryHandler.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public class GetUserCalendarQueryHandler : IRequestHandler<GetUserCalendarQuery, Result<CalendarDto>>
{
    private readonly ICalendarRepository _calendarRepository;

    public GetUserCalendarQueryHandler(ICalendarRepository calendarRepository)
    {
        _calendarRepository = calendarRepository;
    }

    public async Task<Result<CalendarDto>> Handle(GetUserCalendarQuery request, CancellationToken cancellationToken)
    {
        var calendar = await _calendarRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (calendar == null)
        {
            return Result<CalendarDto>.Failure($"Calendar for user {request.UserId.Value} not found.");
        }

        var dto = new CalendarDto(
            calendar.Id.Value,
            calendar.UserId.Value,
            calendar.Items.Select(i => new CalendarItemDto(
                i.Id.Value,
                i.PredictionItemId.Value,
                i.Merchant,
                i.Amount,
                i.DueDate,
                i.Account,
                i.AccountName,
                i.Description,
                i.IsPaid,
                i.PaidDate,
                i.CreatedAt
            )).ToList(),
            calendar.CreatedAt
        );

        return Result<CalendarDto>.Success(dto);
    }
}
```

---

## 8. Infrastructure Layer: Repository

### 📁 File: `src/AICalendar.Infrastructure/Persistence/Repositories/CalendarRepository.cs`

```csharp
using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Persistence.Repositories;

public class CalendarRepository : ICalendarRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Calendar?> GetByIdAsync(CalendarId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Calendar>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Calendar?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Calendar>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<List<Calendar>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Calendar>()
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Calendar calendar, CancellationToken cancellationToken = default)
    {
        await _context.Set<Calendar>().AddAsync(calendar, cancellationToken);
    }

    public Task UpdateAsync(Calendar calendar, CancellationToken cancellationToken = default)
    {
        _context.Set<Calendar>().Update(calendar);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Calendar calendar, CancellationToken cancellationToken = default)
    {
        _context.Set<Calendar>().Remove(calendar);
        return Task.CompletedTask;
    }
}
```

---

## 9. Infrastructure Layer: EF Configuration

### 📁 File: `src/AICalendar.Infrastructure/Persistence/Configurations/CalendarConfiguration.cs`

```csharp
using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICalendar.Infrastructure.Persistence.Configurations;

public class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
{
    public void Configure(EntityTypeBuilder<Calendar> builder)
    {
        builder.ToTable("Calendars");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => CalendarId.Create(value))
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .IsRequired();

        builder.HasIndex(c => c.UserId)
            .IsUnique();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Owned collection for CalendarItems
        builder.OwnsMany(c => c.Items, items =>
        {
            items.ToTable("CalendarItems");

            items.WithOwner().HasForeignKey("CalendarId");

            items.HasKey("Id");

            items.Property(i => i.Id)
                .HasConversion(
                    id => id.Value,
                    value => CalendarItemId.Create(value))
                .ValueGeneratedNever();

            items.Property(i => i.PredictionItemId)
                .HasConversion(
                    id => id.Value,
                    value => PredictionItemId.Create(value))
                .IsRequired();

            items.Property(i => i.Merchant)
                .HasMaxLength(200)
                .IsRequired();

            items.Property(i => i.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            items.Property(i => i.DueDate)
                .IsRequired();

            items.Property(i => i.Account)
                .HasMaxLength(100);

            items.Property(i => i.AccountName)
                .HasMaxLength(200);

            items.Property(i => i.Description)
                .HasMaxLength(500);

            items.Property(i => i.IsPaid)
                .IsRequired();

            items.Property(i => i.PaidDate);

            items.Property(i => i.CreatedAt)
                .IsRequired();

            items.HasIndex(i => i.PredictionItemId);
            items.HasIndex(i => i.DueDate);
        });

        // Ignore domain events (handled by base class)
        builder.Ignore(c => c.DomainEvents);
    }
}
```

### Update `ApplicationDbContext`

Add to `src/AICalendar.Infrastructure/Data/ApplicationDbContext.cs`:

```csharp
public DbSet<Calendar> Calendars => Set<Calendar>();
```

And in `OnModelCreating`:

```csharp
modelBuilder.ApplyConfiguration(new CalendarConfiguration());
```

---

## 10. API Layer: Controller

### 📁 File: `src/AICalendar.API/Controllers/CalendarController.cs`

```csharp
using AICalendar.Application.Calendar.Commands.MarkItemAsPaid;
using AICalendar.Application.Calendar.Queries.GetUserCalendar;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserCalendar(Guid userId)
    {
        var query = new GetUserCalendarQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("items/{itemId:guid}/mark-paid")]
    public async Task<IActionResult> MarkItemAsPaid(Guid itemId, [FromBody] MarkPaidRequest request)
    {
        var command = new MarkItemAsPaidCommand(
            CalendarItemId.Create(itemId),
            request.PaidDate
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

public record MarkPaidRequest(DateTime PaidDate);
```

---

## 11. Testing the Integration

### Step 1: Register Services in `Program.cs`

Add to `src/AICalendar.API/Program.cs`:

```csharp
// Register Calendar Repository
builder.Services.AddScoped<ICalendarRepository, CalendarRepository>();
```

### Step 2: Create Migration

```bash
dotnet ef migrations add AddCalendarAggregate --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
dotnet ef database update --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

### Step 3: Test the Flow

1. **Create a test prediction:**
   ```bash
   POST http://localhost:8080/api/predictions/test-seed
   ```

2. **Accept some items:**
   ```bash
   POST http://localhost:8080/api/predictions/batch-process
   Content-Type: application/json

   {
     "acceptedItemIds": ["<item-guid>"],
     "rejectedItemIds": []
   }
   ```

3. **Check Hangfire Dashboard:**
   - Navigate to `http://localhost:8080/hangfire`
   - Look for jobs: `Process Event: PredictionBatchAcceptedEvent | ID: <guid>`
   - Verify they succeeded

4. **Query the calendar:**
   ```bash
   GET http://localhost:8080/api/calendar/user/<user-guid>
   ```

5. **Mark an item as paid:**
   ```bash
   POST http://localhost:8080/api/calendar/items/<item-guid>/mark-paid
   Content-Type: application/json

   {
     "paidDate": "2025-12-02T00:00:00Z"
   }
   ```

---

## 🎉 Summary

You've now implemented:
- ✅ **Calendar Aggregate** with full domain logic
- ✅ **Event Handler** that consumes `PredictionBatchAcceptedEvent`
- ✅ **Repository Pattern** for data access
- ✅ **Commands & Queries** for CQRS
- ✅ **API Endpoints** for calendar management
- ✅ **EF Core Configuration** with proper relationships
- ✅ **Domain Events** for further integrations (e.g., reminders)

The Calendar aggregate is now fully integrated with the Prediction system via domain events and the Outbox pattern! 🚀
