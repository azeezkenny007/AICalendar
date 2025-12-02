# User Feedback Implementation Guide

## Overview
This guide will walk you through implementing the **User Feedback Aggregate** to capture and analyze user acceptance/rejection patterns. The Feedback system consumes domain events raised by the Prediction aggregate to improve AI prediction accuracy over time.

---

## 🎯 Objective
Build a complete User Feedback aggregate that:
- Listens to `PredictionBatchRejectedEvent` and `PredictionBatchAcceptedEvent`
- Records positive and negative feedback for AI model training
- Tracks patterns in user behavior (what they accept vs reject)
- Provides analytics for prediction accuracy improvement

---

## 📋 Table of Contents
1. [Domain Layer: Aggregates & Entities](#1-domain-layer-aggregates--entities)
2. [Domain Layer: Value Objects & Enums](#2-domain-layer-value-objects--enums)
3. [Domain Layer: Events](#3-domain-layer-events)
4. [Domain Layer: Repository Interface](#4-domain-layer-repository-interface)
5. [Application Layer: Event Handlers](#5-application-layer-event-handlers)
6. [Application Layer: Queries](#6-application-layer-queries)
7. [Infrastructure Layer: Repository](#7-infrastructure-layer-repository)
8. [Infrastructure Layer: EF Configuration](#8-infrastructure-layer-ef-configuration)
9. [API Layer: Controller](#9-api-layer-controller)
10. [Testing the Integration](#10-testing-the-integration)

---

## 1. Domain Layer: Aggregates & Entities

### 📁 File: `src/AICalendar.Domain/Aggregates/UserFeedbackAggregate/UserFeedback.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.Events;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Aggregates.UserFeedbackAggregate;

/// <summary>
/// UserFeedback aggregate root - captures user acceptance/rejection patterns for AI training
/// </summary>
public class UserFeedback : AggregateRoot<UserFeedbackId>
{
    public UserId UserId { get; private set; } = default!;
    public PredictionId PredictionId { get; private set; } = default!;
    public PredictionItemId PredictionItemId { get; private set; } = default!;

    public FeedbackType Type { get; private set; }
    public FeedbackAction Action { get; private set; }

    // Prediction details at time of feedback
    public string Merchant { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }

    // For accepted items - track if user edited before accepting
    public bool WasEdited { get; private set; }
    public decimal? OriginalAmount { get; private set; }
    public DateTime? OriginalDueDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private UserFeedback() { }

    private UserFeedback(
        UserId userId,
        PredictionId predictionId,
        PredictionItemId predictionItemId,
        FeedbackType type,
        FeedbackAction action,
        string merchant,
        decimal amount,
        DateTime dueDate,
        bool wasEdited = false,
        decimal? originalAmount = null,
        DateTime? originalDueDate = null)
    {
        Id = UserFeedbackId.Create();
        UserId = userId;
        PredictionId = predictionId;
        PredictionItemId = predictionItemId;
        Type = type;
        Action = action;
        Merchant = merchant;
        Amount = amount;
        DueDate = dueDate;
        WasEdited = wasEdited;
        OriginalAmount = originalAmount;
        OriginalDueDate = originalDueDate;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates positive feedback when user accepts a prediction
    /// </summary>
    public static UserFeedback CreatePositive(
        UserId userId,
        PredictionId predictionId,
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        bool wasEdited,
        decimal? originalAmount,
        DateTime? originalDueDate)
    {
        var feedback = new UserFeedback(
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Positive,
            FeedbackAction.Accepted,
            merchant,
            amount,
            dueDate,
            wasEdited,
            originalAmount,
            originalDueDate
        );

        feedback.AddDomainEvent(new FeedbackRecordedEvent(
            feedback.Id,
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Positive,
            wasEdited,
            DateTime.UtcNow
        ));

        return feedback;
    }

    /// <summary>
    /// Creates negative feedback when user rejects a prediction
    /// </summary>
    public static UserFeedback CreateNegative(
        UserId userId,
        PredictionId predictionId,
        PredictionItemId predictionItemId,
        string merchant,
        decimal amount,
        DateTime dueDate)
    {
        var feedback = new UserFeedback(
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Negative,
            FeedbackAction.Rejected,
            merchant,
            amount,
            dueDate
        );

        feedback.AddDomainEvent(new FeedbackRecordedEvent(
            feedback.Id,
            userId,
            predictionId,
            predictionItemId,
            FeedbackType.Negative,
            false,
            DateTime.UtcNow
        ));

        return feedback;
    }
}
```

---

## 2. Domain Layer: Value Objects & Enums

### 📁 File: `src/AICalendar.Domain/ValueObjects/UserFeedbackId.cs`

```csharp
using System.Text.Json.Serialization;

namespace AICalendar.Domain.ValueObjects;

public record UserFeedbackId
{
    public Guid Value { get; init; }

    private UserFeedbackId() { }

    [JsonConstructor]
    public UserFeedbackId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserFeedbackId cannot be empty", nameof(value));

        Value = value;
    }

    public static UserFeedbackId Create() => new(Guid.NewGuid());
    public static UserFeedbackId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserFeedbackId id) => id.Value;
}
```

### 📁 File: `src/AICalendar.Domain/Aggregates/UserFeedbackAggregate/FeedbackType.cs`

```csharp
namespace AICalendar.Domain.Aggregates.UserFeedbackAggregate;

/// <summary>
/// Type of feedback from user
/// </summary>
public enum FeedbackType
{
    /// <summary>
    /// User accepted the prediction
    /// </summary>
    Positive = 1,

    /// <summary>
    /// User rejected the prediction
    /// </summary>
    Negative = 2
}
```

### 📁 File: `src/AICalendar.Domain/Aggregates/UserFeedbackAggregate/FeedbackAction.cs`

```csharp
namespace AICalendar.Domain.Aggregates.UserFeedbackAggregate;

/// <summary>
/// Action taken by user on prediction
/// </summary>
public enum FeedbackAction
{
    /// <summary>
    /// User accepted the prediction item
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// User rejected the prediction item
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// User edited then accepted (indicates prediction was close but not perfect)
    /// </summary>
    EditedThenAccepted = 3
}
```

---

## 3. Domain Layer: Events

### 📁 File: `src/AICalendar.Domain/Events/FeedbackRecordedEvent.cs`

```csharp
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Events;

public record FeedbackRecordedEvent(
    UserFeedbackId FeedbackId,
    UserId UserId,
    PredictionId PredictionId,
    PredictionItemId PredictionItemId,
    FeedbackType Type,
    bool WasEdited,
    DateTime OccurredOn
) : IDomainEvent;
```

---

## 4. Domain Layer: Repository Interface

### 📁 File: `src/AICalendar.Domain/Interfaces/IUserFeedbackRepository.cs`

```csharp
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Interfaces;

public interface IUserFeedbackRepository
{
    Task<UserFeedback?> GetByIdAsync(UserFeedbackId id, CancellationToken cancellationToken = default);
    Task<List<UserFeedback>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<UserFeedback>> GetByPredictionIdAsync(PredictionId predictionId, CancellationToken cancellationToken = default);
    Task<List<UserFeedback>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserFeedback feedback, CancellationToken cancellationToken = default);
    Task AddRangeAsync(List<UserFeedback> feedbacks, CancellationToken cancellationToken = default);
}
```

---

## 5. Application Layer: Event Handlers

### 📁 File: `src/AICalendar.Application/Predictions/DomainEventHandlers/PredictionRejectedFeedbackHandler.cs`

**Replace the existing TODO implementation with:**

```csharp
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Events;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionRejectedFeedbackHandler : INotificationHandler<DomainEventNotification<PredictionBatchRejectedEvent>>
{
    private readonly IUserFeedbackRepository _feedbackRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PredictionRejectedFeedbackHandler> _logger;

    public PredictionRejectedFeedbackHandler(
        IUserFeedbackRepository feedbackRepository,
        IUnitOfWork unitOfWork,
        ILogger<PredictionRejectedFeedbackHandler> logger)
    {
        _feedbackRepository = feedbackRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PredictionBatchRejectedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "FEEDBACK: Handling batch rejection for Prediction {PredictionId}. {Count} items rejected.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        var feedbacks = new List<UserFeedback>();

        foreach (var item in domainEvent.Items)
        {
            _logger.LogInformation(
                "FEEDBACK: Recording negative feedback for item {ItemId}: {Merchant} - ${Amount}",
                item.ItemId.Value,
                item.Merchant,
                item.Amount
            );

            var feedback = UserFeedback.CreateNegative(
                domainEvent.UserId,
                domainEvent.PredictionId,
                item.ItemId,
                item.Merchant,
                item.Amount,
                item.DueDate
            );

            feedbacks.Add(feedback);
        }

        await _feedbackRepository.AddRangeAsync(feedbacks, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "FEEDBACK: Successfully recorded {Count} negative feedback entries for user {UserId}",
            feedbacks.Count,
            domainEvent.UserId.Value
        );
    }
}
```

### 📁 File: `src/AICalendar.Application/Predictions/DomainEventHandlers/PredictionAcceptedFeedbackHandler.cs`

**Create a new handler for accepted items:**

```csharp
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Events;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionAcceptedFeedbackHandler : INotificationHandler<DomainEventNotification<PredictionBatchAcceptedEvent>>
{
    private readonly IUserFeedbackRepository _feedbackRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PredictionAcceptedFeedbackHandler> _logger;

    public PredictionAcceptedFeedbackHandler(
        IUserFeedbackRepository feedbackRepository,
        IUnitOfWork unitOfWork,
        ILogger<PredictionAcceptedFeedbackHandler> logger)
    {
        _feedbackRepository = feedbackRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PredictionBatchAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "FEEDBACK: Handling batch acceptance for Prediction {PredictionId}. {Count} items accepted.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        var feedbacks = new List<UserFeedback>();

        foreach (var item in domainEvent.Items)
        {
            var feedbackType = item.IsEdited ? "edited then accepted" : "accepted as-is";

            _logger.LogInformation(
                "FEEDBACK: Recording positive feedback for item {ItemId}: {Merchant} - ${Amount} ({Type})",
                item.ItemId.Value,
                item.Merchant,
                item.Amount,
                feedbackType
            );

            var feedback = UserFeedback.CreatePositive(
                domainEvent.UserId,
                domainEvent.PredictionId,
                item.ItemId,
                item.Merchant,
                item.Amount,
                item.DueDate,
                item.IsEdited,
                item.OriginalAmount,
                item.OriginalDueDate
            );

            feedbacks.Add(feedback);
        }

        await _feedbackRepository.AddRangeAsync(feedbacks, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "FEEDBACK: Successfully recorded {Count} positive feedback entries for user {UserId}",
            feedbacks.Count,
            domainEvent.UserId.Value
        );
    }
}
```

---

## 6. Application Layer: Queries

### 📁 File: `src/AICalendar.Application/Feedback/Queries/GetUserFeedbackStats/GetUserFeedbackStatsQuery.cs`

```csharp
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;

public record GetUserFeedbackStatsQuery(UserId UserId) : IRequest<Result<FeedbackStatsDto>>;
```

### 📁 File: `src/AICalendar.Application/Feedback/Queries/GetUserFeedbackStats/FeedbackStatsDto.cs`

```csharp
namespace AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;

public record FeedbackStatsDto(
    Guid UserId,
    int TotalFeedbacks,
    int PositiveFeedbacks,
    int NegativeFeedbacks,
    int EditedBeforeAccepted,
    decimal AcceptanceRate,
    decimal EditRate
);
```

### 📁 File: `src/AICalendar.Application/Feedback/Queries/GetUserFeedbackStats/GetUserFeedbackStatsQueryHandler.cs`

```csharp
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;

public class GetUserFeedbackStatsQueryHandler : IRequestHandler<GetUserFeedbackStatsQuery, Result<FeedbackStatsDto>>
{
    private readonly IUserFeedbackRepository _feedbackRepository;

    public GetUserFeedbackStatsQueryHandler(IUserFeedbackRepository feedbackRepository)
    {
        _feedbackRepository = feedbackRepository;
    }

    public async Task<Result<FeedbackStatsDto>> Handle(GetUserFeedbackStatsQuery request, CancellationToken cancellationToken)
    {
        var feedbacks = await _feedbackRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (!feedbacks.Any())
        {
            return Result<FeedbackStatsDto>.Success(new FeedbackStatsDto(
                request.UserId.Value,
                0, 0, 0, 0, 0, 0
            ));
        }

        var total = feedbacks.Count;
        var positive = feedbacks.Count(f => f.Type == FeedbackType.Positive);
        var negative = feedbacks.Count(f => f.Type == FeedbackType.Negative);
        var edited = feedbacks.Count(f => f.WasEdited);

        var acceptanceRate = total > 0 ? (decimal)positive / total * 100 : 0;
        var editRate = positive > 0 ? (decimal)edited / positive * 100 : 0;

        var dto = new FeedbackStatsDto(
            request.UserId.Value,
            total,
            positive,
            negative,
            edited,
            Math.Round(acceptanceRate, 2),
            Math.Round(editRate, 2)
        );

        return Result<FeedbackStatsDto>.Success(dto);
    }
}
```

---

## 7. Infrastructure Layer: Repository

### 📁 File: `src/AICalendar.Infrastructure/Persistence/Repositories/UserFeedbackRepository.cs`

```csharp
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Persistence.Repositories;

public class UserFeedbackRepository : IUserFeedbackRepository
{
    private readonly ApplicationDbContext _context;

    public UserFeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserFeedback?> GetByIdAsync(UserFeedbackId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<List<UserFeedback>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserFeedback>> GetByPredictionIdAsync(PredictionId predictionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .Where(f => f.PredictionId == predictionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserFeedback>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserFeedback>()
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserFeedback feedback, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserFeedback>().AddAsync(feedback, cancellationToken);
    }

    public async Task AddRangeAsync(List<UserFeedback> feedbacks, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserFeedback>().AddRangeAsync(feedbacks, cancellationToken);
    }
}
```

---

## 8. Infrastructure Layer: EF Configuration

### 📁 File: `src/AICalendar.Infrastructure/Persistence/Configurations/UserFeedbackConfiguration.cs`

```csharp
using AICalendar.Domain.Aggregates.UserFeedbackAggregate;
using AICalendar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICalendar.Infrastructure.Persistence.Configurations;

public class UserFeedbackConfiguration : IEntityTypeConfiguration<UserFeedback>
{
    public void Configure(EntityTypeBuilder<UserFeedback> builder)
    {
        builder.ToTable("UserFeedbacks");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(
                id => id.Value,
                value => UserFeedbackId.Create(value))
            .ValueGeneratedNever();

        builder.Property(f => f.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .IsRequired();

        builder.Property(f => f.PredictionId)
            .HasConversion(
                id => id.Value,
                value => PredictionId.Create(value))
            .IsRequired();

        builder.Property(f => f.PredictionItemId)
            .HasConversion(
                id => id.Value,
                value => PredictionItemId.Create(value))
            .IsRequired();

        builder.Property(f => f.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(f => f.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(f => f.Merchant)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(f => f.DueDate)
            .IsRequired();

        builder.Property(f => f.WasEdited)
            .IsRequired();

        builder.Property(f => f.OriginalAmount)
            .HasPrecision(18, 2);

        builder.Property(f => f.OriginalDueDate);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.PredictionId);
        builder.HasIndex(f => f.Type);
        builder.HasIndex(f => f.CreatedAt);

        // Ignore domain events
        builder.Ignore(f => f.DomainEvents);
    }
}
```

### Update `ApplicationDbContext`

Add to `src/AICalendar.Infrastructure/Data/ApplicationDbContext.cs`:

```csharp
public DbSet<UserFeedback> UserFeedbacks => Set<UserFeedback>();
```

And in `OnModelCreating`:

```csharp
modelBuilder.ApplyConfiguration(new UserFeedbackConfiguration());
```

---

## 9. API Layer: Controller

### 📁 File: `src/AICalendar.API/Controllers/FeedbackController.cs`

```csharp
using AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedbackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats/user/{userId:guid}")]
    public async Task<IActionResult> GetUserStats(Guid userId)
    {
        var query = new GetUserFeedbackStatsQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
```

---

## 10. Testing the Integration

### Step 1: Register Services in `Program.cs`

Add to `src/AICalendar.API/Program.cs`:

```csharp
// Register User Feedback Repository
builder.Services.AddScoped<IUserFeedbackRepository, UserFeedbackRepository>();
```

### Step 2: Create Migration

```bash
dotnet ef migrations add AddUserFeedbackAggregate --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
dotnet ef database update --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

### Step 3: Test the Flow

1. **Create a test prediction:**
   ```bash
   POST http://localhost:8080/api/predictions/test-seed
   ```

2. **Accept and reject some items:**
   ```bash
   POST http://localhost:8080/api/predictions/batch-process
   Content-Type: application/json

   {
     "acceptedItemIds": ["<item-guid-1>"],
     "rejectedItemIds": ["<item-guid-2>"]
   }
   ```

3. **Check Hangfire Dashboard:**
   - Navigate to `http://localhost:8080/hangfire`
   - Look for jobs:
     - `Process Event: PredictionBatchAcceptedEvent | ID: <guid>`
     - `Process Event: PredictionBatchRejectedEvent | ID: <guid>`
   - Verify they succeeded

4. **Query feedback stats:**
   ```bash
   GET http://localhost:8080/api/feedback/stats/user/<user-guid>
   ```

   **Expected Response:**
   ```json
   {
     "userId": "...",
     "totalFeedbacks": 2,
     "positiveFeedbacks": 1,
     "negativeFeedbacks": 1,
     "editedBeforeAccepted": 0,
     "acceptanceRate": 50.00,
     "editRate": 0.00
   }
   ```

---

## 🎉 Summary

You've now implemented:
- ✅ **UserFeedback Aggregate** with domain logic for tracking user behavior
- ✅ **Event Handlers** that consume both `PredictionBatchAcceptedEvent` and `PredictionBatchRejectedEvent`
- ✅ **Repository Pattern** for data access
- ✅ **Analytics Queries** for tracking acceptance rates and edit patterns
- ✅ **API Endpoints** for feedback statistics
- ✅ **EF Core Configuration** with proper indexes
- ✅ **Domain Events** for further integrations (e.g., ML model training)

The User Feedback system now captures valuable data about prediction accuracy, which can be used to:
- 📊 Track AI model performance over time
- 🎯 Identify patterns in what users accept vs reject
- 🔧 Fine-tune prediction algorithms based on user behavior
- 📈 Generate reports on prediction accuracy by merchant, amount range, etc.

This feedback loop is essential for continuous improvement of the AI prediction system! 🚀
