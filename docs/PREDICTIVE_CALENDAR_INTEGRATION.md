# ALAT Predictive Calendar - System Integration Guide

## 📋 Table of Contents
1. [System Overview](#system-overview)
2. [Architecture Flow](#architecture-flow)
3. [Component Details](#component-details)
4. [AI Engine Integration](#ai-engine-integration)
5. [Data Flow Sequence](#data-flow-sequence)
6. [File Structure Mapping](#file-structure-mapping)
7. [Implementation Guide](#implementation-guide)

---

## 🎯 System Overview

The ALAT Predictive Calendar transforms ALAT from a reactive to a proactive financial assistant by:
- Analyzing user transaction history (2-3 months)
- Generating personalized predictions (max 10 per cycle)
- Allowing user feedback (Keep/Discard)
- Building finalized calendar from accepted predictions
- Sending timely reminders

### Core Goals
- **Primary**: Predict recurring payments, generate suggested calendar, process user feedback
- **Secondary**: Improve on-time payments, reduce late transactions, build emotional connection

---

## 🏗️ Architecture Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                         MOBILE APP (ALAT)                            │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │ Suggested  │  │ Keep/Discard │  │   Calendar   │                 │
│  │ Calendar   │→ │    Flow      │→ │     View     │                 │
│  │   Screen   │  │              │  │              │                 │
│  └────────────┘  └──────────────┘  └──────────────┘                │
└────────────┬────────────────────────────────────────────────────────┘
             │ REST API / SignalR
             ▼
┌─────────────────────────────────────────────────────────────────────┐
│               AICalendar.API (Entry Point)                           │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ Predictions      │  │ Calendar         │  │ Reminders        │  │
│  │ Controller       │  │ Controller       │  │ Controller       │  │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘  │
└───────────┼────────────────────┼─────────────────────┼─────────────┘
            │                    │                     │
            ▼                    ▼                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│         AICalendar.Application (Business Logic / CQRS)               │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    MediatR Pipeline                          │   │
│  │  Validation → Logging → Caching → UnitOfWork                │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  Commands (Write):                    Queries (Read):                │
│  ┌──────────────────────────┐        ┌──────────────────────────┐  │
│  │ GeneratePredictions      │        │ GetUserPredictions       │  │
│  │ ProcessUserFeedback      │        │ GetPredictionStatus      │  │
│  │ CreateCalendarItem       │        │ GetUserCalendar          │  │
│  │ ScheduleReminder         │        └──────────────────────────┘  │
│  └──────────────────────────┘                                       │
│                                                                      │
│  Event Handlers:                                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ PredictionsGeneratedEventHandler → Cache predictions        │   │
│  │ PredictionAcceptedEventHandler → Create calendar item       │   │
│  │ UserFeedbackEventHandler → Send feedback to AI              │   │
│  └──────────────────────────────────────────────────────────────┘   │
└────────┬─────────────────────────────────────────┬──────────────────┘
         │                                         │
         ▼                                         ▼
┌─────────────────────────────────┐    ┌─────────────────────────────┐
│  AICalendar.Infrastructure      │    │   AICalendar.Domain         │
│                                  │    │                             │
│  ┌────────────────────────────┐ │    │  Aggregates:                │
│  │ Repositories               │ │    │  ┌─────────────────────┐    │
│  │ - PredictionRepository     │ │    │  │ Prediction          │    │
│  │ - CalendarRepository       │ │◄───┤  │ - PredictionItems   │    │
│  │ - TransactionRepository    │ │    │  │ - ConfidenceScores  │    │
│  │ - UserFeedbackRepository   │ │    │  └─────────────────────┘    │
│  └────────────────────────────┘ │    │  ┌─────────────────────┐    │
│                                  │    │  │ Calendar            │    │
│  ┌────────────────────────────┐ │    │  │ - CalendarItems     │    │
│  │ External Services          │ │    │  │ - Reminders         │    │
│  │ - AIServiceClient (gRPC)   │ │    │  └─────────────────────┘    │
│  │ - RedisCacheService        │ │    │  ┌─────────────────────┐    │
│  │ - NotificationService      │ │    │  │ UserFeedback        │    │
│  └────────────────────────────┘ │    │  │ - FeedbackActions   │    │
│                                  │    │  └─────────────────────┘    │
│  ┌────────────────────────────┐ │    │                             │
│  │ Background Jobs (Hangfire) │ │    │  Entities:                  │
│  │ - BatchPredictionJob       │ │    │  - Transaction              │
│  │ - SendReminderJob          │ │    │  - User                     │
│  │ - CleanupExpiredJob        │ │    │                             │
│  └────────────────────────────┘ │    └─────────────────────────────┘
│                                  │
│  ┌────────────────────────────┐ │
│  │ Persistence (EF Core)      │ │
│  │ - ApplicationDbContext     │ │
│  │ - UnitOfWork              │ │
│  └────────────────────────────┘ │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│      External Systems                │
│  ┌────────────┐  ┌────────────────┐  │
│  │ AI/ML      │  │ Azure SQL DB   │  │
│  │ Engine     │  │ Redis Cache    │  │
│  │ (Python)   │  │                │  │
│  └────────────┘  └────────────────┘  │
└──────────────────────────────────────┘
```

---

## 🔧 Component Details

### 1. **Domain Layer** (`AICalendar.Domain/`)

#### Core Business Entities

**Location:** `src/AICalendar.Domain/Entities/`

- **User.cs**
  - Represents ALAT bank customer
  - Properties: `FirstName`, `LastName`, `Username`, `Email`, `CreatedAt`
  - Navigation: Collection of `Transactions`

- **Transaction.cs**
  - Historical financial transactions (2-3 months)
  - Properties: `UserId`, `Amount`, `Description`, `Type`, `TransactionDate`, `CreatedAt`
  - Used by AI engine for pattern detection

#### Aggregates

**Location:** `src/AICalendar.Domain/Aggregates/`

##### **PredictionAggregate/**

- **Prediction.cs** (Aggregate Root)
  - Manages prediction lifecycle
  - Contains collection of `PredictionItems` (max 10)
  - Enforces business rules (max 10 items, status transitions)
  - Properties: `UserId`, `Cycle`, `GeneratedAt`, `ExpiresAt`, `Status`

- **PredictionItem.cs** (Entity)
  - Individual predicted financial task
  - Properties:
    - `TransactionId` (from AI response)
    - `PredictedDate` (predicted_next_date from AI)
    - `Amount`
    - `Description`
    - `ConfidenceScore` (final_confidence from AI)
    - `PatternType` (derived from is_recurring)
    - `Explanation` (from AI response)
    - `IsAccepted` (Keep/Discard state)
    - `RuleConfidence` (from AI)
    - `MLConfidence` (from AI)

- **ConfidenceScore.cs** (Value Object)
  - Encapsulates confidence level (0.0-1.0)
  - Computed from AI's `final_confidence`
  - Levels: High (≥0.8), Medium (0.5-0.79), Low (<0.5)

- **PredictionCycle.cs** (Value Object)
  - Time period: Monthly, Weekly, Bi-Weekly
  - Immutable, validates date ranges

- **PredictionStatus.cs** (Enumeration)
  - Pending → Generated → Reviewing → Completed/Expired

- **PatternType.cs** (Enumeration)
  - Derived from AI's `is_recurring` field
  - Values: Recurring, OneTime

##### **CalendarAggregate/**

- **Calendar.cs** (Aggregate Root)
  - User's finalized financial calendar
  - Contains only accepted (Kept) predictions
  - Properties: `UserId`, `Month`, `Year`

- **CalendarItem.cs** (Entity)
  - Created when user taps "Keep" on prediction
  - Properties: `DueDate`, `Amount`, `Description`, `IsCompleted`, `CompletedAt`
  - Links to original `PredictionItemId`

- **Reminder.cs** (Entity)
  - Notification scheduled for calendar item
  - Properties: `CalendarItemId`, `ScheduledFor`, `Status`, `SentAt`

- **ReminderStatus.cs** (Enumeration)
  - Scheduled → Sent → Acknowledged → Missed

##### **UserFeedbackAggregate/**

- **UserFeedback.cs** (Aggregate Root)
  - Captures Keep/Discard decisions
  - Sent to AI for continuous learning
  - Properties: `UserId`, `PredictionItemId`, `Action`, `Timestamp`

- **FeedbackAction.cs** (Value Object)
  - Keep or Discard

- **FeedbackType.cs** (Enumeration)
  - Positive (Keep), Negative (Discard)

#### Domain Events

**Location:** `src/AICalendar.Domain/Events/`

- **PredictionGeneratedEvent.cs**
  - Raised when AI completes prediction generation
  - Triggers caching and notification

- **PredictionAcceptedEvent.cs**
  - User tapped "Keep"
  - Triggers CalendarItem creation and Reminder scheduling

- **PredictionRejectedEvent.cs**
  - User tapped "Discard"
  - Sends negative feedback to AI

- **CalendarItemCreatedEvent.cs**
  - Finalized calendar item added

- **ReminderScheduledEvent.cs**
  - Notification queued

#### Repositories (Interfaces)

**Location:** `src/AICalendar.Domain/Interfaces/`

- `IPredictionRepository.cs`
- `ICalendarRepository.cs`
- `ITransactionRepository.cs`
- `IUserFeedbackRepository.cs`

---

### 2. **Application Layer** (`AICalendar.Application/`)

#### Commands (Write Operations)

**Location:** `src/AICalendar.Application/Predictions/Commands/`

##### **GeneratePredictions/**

**Trigger**: Mobile app requests predictions OR nightly batch job

**Files**:
- `GeneratePredictionsCommand.cs`
  ```csharp
  public record GeneratePredictionsCommand(Guid UserId, PredictionCycle Cycle);
  ```

- `GeneratePredictionsCommandHandler.cs`
  - **Step 1**: Fetch user's transactions (2-3 months) from `ITransactionRepository`
  - **Step 2**: Call AI engine via `IAIServiceClient.GeneratePredictionsAsync()`
  - **Step 3**: Map AI response to `Prediction` aggregate with `PredictionItems`
  - **Step 4**: Save to DB via `IPredictionRepository`
  - **Step 5**: Publish `PredictionGeneratedEvent`
  - **Performance**: ≤5 seconds

- `GeneratePredictionsCommandValidator.cs`
  - Validates UserId exists
  - Validates Cycle is valid

##### **ProcessUserFeedback/**

**Trigger**: User taps Keep/Discard on predictions

**Files**:
- `ProcessUserFeedbackCommand.cs`
  ```csharp
  public record ProcessUserFeedbackCommand(
      Guid UserId,
      List<FeedbackItem> FeedbackItems
  );

  public record FeedbackItem(Guid PredictionItemId, bool IsAccepted);
  ```

- `ProcessUserFeedbackCommandHandler.cs`
  - **Step 1**: Load `Prediction` aggregate from repository
  - **Step 2**: For each feedback item:
    - If `IsAccepted == true`: Mark item as accepted, publish `PredictionAcceptedEvent`
    - If `IsAccepted == false`: Mark as rejected, publish `PredictionRejectedEvent`
  - **Step 3**: Save feedback to `IUserFeedbackRepository`
  - **Step 4**: Update prediction status to `Completed`

##### **CreateCalendarItem/** (in Calendar folder)

**Trigger**: `PredictionAcceptedEvent` raised

**Files**:
- `CreateCalendarItemCommand.cs`
- `CreateCalendarItemCommandHandler.cs`
  - Creates `CalendarItem` from accepted `PredictionItem`
  - Publishes `CalendarItemCreatedEvent`

##### **ScheduleReminder/** (in Reminders folder)

**Trigger**: `CalendarItemCreatedEvent` raised

**Files**:
- `ScheduleReminderCommand.cs`
- `ScheduleReminderCommandHandler.cs`
  - Schedules Hangfire job for notification
  - Creates `Reminder` entity

#### Queries (Read Operations)

**Location:** `src/AICalendar.Application/Predictions/Queries/`

##### **GetUserPredictions/**

**Trigger**: Mobile app opens Suggested Calendar screen

**Files**:
- `GetUserPredictionsQuery.cs`
  ```csharp
  public record GetUserPredictionsQuery(Guid UserId, PredictionCycle Cycle);
  ```

- `GetUserPredictionsQueryHandler.cs`
  - **Step 1**: Check Redis cache (`predictions:{userId}:{cycle}`)
  - **Step 2**: If miss, query `IPredictionRepository`
  - **Step 3**: Map to `PredictionDto` (contains AI explanation, confidence levels)
  - **Step 4**: Cache result (7-day TTL)

- `PredictionDto.cs`
  ```csharp
  public record PredictionDto(
      Guid Id,
      List<PredictionItemDto> Items,
      DateTime GeneratedAt,
      DateTime ExpiresAt,
      string Status
  );

  public record PredictionItemDto(
      Guid Id,
      Guid TransactionId,
      DateTime PredictedDate,
      decimal Amount,
      string Description,
      double ConfidenceScore,
      string ConfidenceLevel, // "High", "Medium", "Low"
      string Explanation,
      string PatternType,
      double RuleConfidence,
      double MLConfidence
  );
  ```

##### **GetUserCalendar/**

**Trigger**: Mobile app opens Calendar view

**Files**:
- `GetUserCalendarQuery.cs`
- `GetUserCalendarQueryHandler.cs`
- `CalendarDto.cs`

#### Event Handlers

**Location:** `src/AICalendar.Application/Predictions/EventHandlers/`

- **PredictionsGeneratedEventHandler.cs**
  - Caches predictions in Redis
  - Sends SignalR notification: "Your predictions are ready!"

- **PredictionAcceptedEventHandler.cs**
  - Triggers `CreateCalendarItemCommand`
  - Triggers `ScheduleReminderCommand`

- **UserFeedbackEventHandler.cs**
  - Sends feedback to AI via `IAIServiceClient.SendFeedbackAsync()`

#### Background Jobs

**Location:** `src/AICalendar.Application/BackgroundJobs/`

- **BatchPredictionJob.cs**
  - Runs nightly (e.g., 2 AM)
  - Fetches all eligible users
  - Enqueues `GeneratePredictionsCommand` for each

- **SendReminderJob.cs**
  - Triggered by Hangfire at scheduled time
  - Sends push notification via `INotificationService`

- **CleanupExpiredPredictionsJob.cs**
  - Runs daily
  - Deletes predictions with `Status = Expired`

#### MediatR Behaviours

**Location:** `src/AICalendar.Application/Common/Behaviours/`

- **ValidationBehaviour.cs**
  - Validates commands/queries using FluentValidation

- **LoggingBehaviour.cs**
  - Logs request/response for audit trail

- **UnitOfWorkBehaviour.cs**
  - Wraps handler in database transaction
  - Auto-saves changes via `IUnitOfWork`

- **CachingBehaviour.cs**
  - Caches query results in Redis

---

### 3. **Infrastructure Layer** (`AICalendar.Infrastructure/`)

#### Persistence

**Location:** `src/AICalendar.Infrastructure/Persistence/`

- **ApplicationDbContext.cs**
  - EF Core DbContext
  - DbSets: `Users`, `Transactions`, `Predictions`, `Calendars`, `Feedbacks`

- **Configurations/**
  - `PredictionConfiguration.cs` - EF fluent API for Prediction aggregate
  - `CalendarConfiguration.cs`
  - `UserFeedbackConfiguration.cs`

- **Repositories/**
  - `PredictionRepository.cs` - Implements `IPredictionRepository`
    - `GetByUserAndCycleAsync()`
    - `AddAsync()`, `UpdateAsync()`
  - `CalendarRepository.cs`
  - `TransactionRepository.cs`
    - `GetUserTransactionsAsync(userId, startDate, endDate)` - Fetches 2-3 months
  - `UserFeedbackRepository.cs`

- **UnitOfWork/**
  - `UnitOfWork.cs` - Wraps `DbContext.SaveChangesAsync()`, publishes domain events

#### External Services

**Location:** `src/AICalendar.Infrastructure/ExternalServices/`

##### **AI/ (gRPC Client)**

- **AIServiceClient.cs**
  - Implements `IAIServiceClient`

  ```csharp
  public interface IAIServiceClient
  {
      Task<AIPredictionResponse> GeneratePredictionsAsync(
          Guid userId,
          List<TransactionDto> transactions
      );

      Task SendFeedbackAsync(UserFeedbackDto feedback);
  }
  ```

- **Models/**
  - `AIPredictionRequest.cs`
    ```csharp
    public record AIPredictionRequest(
        Guid UserId,
        List<TransactionDto> Transactions
    );

    public record TransactionDto(
        Guid Id,
        decimal Amount,
        string Description,
        string Type,
        DateTime Date
    );
    ```

  - `AIPredictionResponse.cs`
    ```csharp
    public record AIPredictionResponse(
        List<AIPredictionItem> Predictions
    );

    public record AIPredictionItem(
        Guid TransactionId,
        double FinalConfidence,
        bool IsRecurring,
        DateTime PredictedNextDate,
        string Explanation,
        double RuleConfidence,
        double MLConfidence
    );
    ```

    **Mapping from AI Engine Response:**
    ```json
    {
        "transaction_id": "guid",           → TransactionId
        "final_confidence": 0.85,           → FinalConfidence
        "is_recurring": true,               → IsRecurring
        "predicted_next_date": "2025-01-15",→ PredictedNextDate
        "explanation": "...",               → Explanation
        "rule_confidence": 0.9,             → RuleConfidence
        "ml_confidence": 0.8                → MLConfidence
    }
    ```

- **Protos/**
  - `predictions.proto` - gRPC service definition (shared with AI team)

- **Configuration/**
  - `AIServiceOptions.cs` - AI service URL, timeout, retry policy

##### **Cache/**

- **RedisCacheService.cs**
  - Implements `ICacheService`
  - Methods: `GetAsync<T>()`, `SetAsync<T>()`, `RemoveAsync()`
  - Cache keys:
    - `predictions:{userId}:{cycle}` - 7 day TTL
    - `calendar:{userId}:{month}` - 30 day TTL

##### **Notifications/**

- **SignalRNotificationService.cs**
  - Sends real-time updates to mobile app
  - Hub: `PredictionHub`

- **NotificationService.cs**
  - Sends push notifications for reminders

#### Background Jobs

**Location:** `src/AICalendar.Infrastructure/BackgroundJobs/`

- **HangfireJobScheduler.cs**
  - Registers recurring jobs

  ```csharp
  RecurringJob.AddOrUpdate<BatchPredictionJob>(
      "batch-predictions",
      job => job.ExecuteAsync(),
      Cron.Daily(2) // 2 AM daily
  );
  ```

- **HangfireConfiguration.cs**
  - Configures Hangfire storage (SQL Server)
  - Dashboard authentication

#### Resilience

**Location:** `src/AICalendar.Infrastructure/Resilience/`

- **ResiliencePolicies.cs**
  - Polly retry policy for AI service calls

  ```csharp
  Policy
      .Handle<HttpRequestException>()
      .WaitAndRetryAsync(3, retryAttempt =>
          TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
      );
  ```

---

### 4. **API Layer** (`AICalendar.API/`)

#### Controllers

**Location:** `src/AICalendar.API/Controllers/`

- **PredictionsController.cs**

  ```csharp
  [ApiController]
  [Route("api/predictions")]
  public class PredictionsController : ControllerBase
  {
      // GET api/predictions/{userId}?cycle=monthly
      [HttpGet("{userId}")]
      public async Task<ActionResult<PredictionDto>> GetPredictions(
          Guid userId,
          [FromQuery] string cycle
      )
      {
          var query = new GetUserPredictionsQuery(userId, cycle);
          var result = await _mediator.Send(query);
          return Ok(result);
      }

      // POST api/predictions/{userId}/generate
      [HttpPost("{userId}/generate")]
      public async Task<IActionResult> GeneratePredictions(Guid userId)
      {
          var command = new GeneratePredictionsCommand(userId, PredictionCycle.Monthly);
          await _mediator.Send(command);
          return Accepted();
      }

      // POST api/predictions/feedback
      [HttpPost("feedback")]
      public async Task<IActionResult> ProcessFeedback(
          [FromBody] ProcessUserFeedbackCommand command
      )
      {
          await _mediator.Send(command);
          return NoContent();
      }
  }
  ```

- **CalendarController.cs**
  - `GET api/calendar/{userId}?month=2025-01`
  - `GET api/calendar/{userId}/items/{itemId}`
  - `PUT api/calendar/items/{itemId}/complete`

- **RemindersController.cs**
  - `GET api/reminders/{userId}`
  - `POST api/reminders/{itemId}/acknowledge`

#### SignalR Hubs

**Location:** `src/AICalendar.API/Hubs/`

- **PredictionHub.cs**

  ```csharp
  public class PredictionHub : Hub
  {
      public async Task NotifyPredictionsReady(Guid userId)
      {
          await Clients.User(userId.ToString())
              .SendAsync("PredictionsReady");
      }
  }
  ```

#### Middleware

**Location:** `src/AICalendar.API/Middleware/`

- **ExceptionHandlingMiddleware.cs**
  - Global exception handler
  - Returns consistent error responses

- **RequestLoggingMiddleware.cs**
  - Logs all HTTP requests/responses

---

## 🤖 AI Engine Integration

### AI Response Format

**Expected Response from AI/ML Engine:**

```json
{
  "predictions": [
    {
      "transaction_id": "guid-here",
      "final_confidence": 0.85,
      "is_recurring": true,
      "predicted_next_date": "2025-01-15T00:00:00Z",
      "explanation": "Monthly electricity bill - PHCN payment detected on 15th of each month for last 3 months",
      "rule_confidence": 0.9,
      "ml_confidence": 0.8
    },
    {
      "transaction_id": "guid-here",
      "final_confidence": 0.72,
      "is_recurring": true,
      "predicted_next_date": "2025-01-10T00:00:00Z",
      "explanation": "Bi-weekly transfer to family member - occurs every 2 weeks",
      "rule_confidence": 0.75,
      "ml_confidence": 0.69
    }
  ]
}
```

### Mapping AI Response to Domain Model

**File:** `AICalendar.Infrastructure/ExternalServices/AI/AIServiceClient.cs`

```csharp
public async Task<Prediction> MapAIResponseToPrediction(
    Guid userId,
    AIPredictionResponse aiResponse,
    PredictionCycle cycle
)
{
    var prediction = Prediction.Create(userId, cycle);

    // Limit to max 10 items (business rule)
    var topPredictions = aiResponse.Predictions
        .OrderByDescending(p => p.FinalConfidence)
        .Take(10)
        .ToList();

    foreach (var aiItem in topPredictions)
    {
        // Fetch original transaction for amount/description
        var transaction = await _transactionRepository
            .GetByIdAsync(aiItem.TransactionId);

        var predictionItem = PredictionItem.Create(
            transactionId: aiItem.TransactionId,
            predictedDate: aiItem.PredictedNextDate,
            amount: transaction.Amount,
            description: transaction.Description,
            confidenceScore: ConfidenceScore.Create(aiItem.FinalConfidence),
            patternType: aiItem.IsRecurring
                ? PatternType.Recurring
                : PatternType.OneTime,
            explanation: aiItem.Explanation,
            ruleConfidence: aiItem.RuleConfidence,
            mlConfidence: aiItem.MLConfidence
        );

        prediction.AddItem(predictionItem);
    }

    return prediction;
}
```

### AI Communication Flow

```
Backend → AI Engine:
1. GeneratePredictionsCommand triggered
2. Fetch user transactions (2-3 months)
3. Send to AI via gRPC: GeneratePredictionsAsync(userId, transactions)
4. AI analyzes patterns and returns predictions
5. Backend maps response to Prediction aggregate
6. Save to database
7. Cache in Redis
8. Notify mobile app via SignalR

Mobile App → Backend:
1. User reviews predictions
2. Taps Keep/Discard on each item
3. POST /api/predictions/feedback
4. ProcessUserFeedbackCommand executes
5. Backend sends feedback to AI: SendFeedbackAsync(feedback)
6. AI learns from user preferences
```

---

## 📊 Data Flow Sequence

### Sequence 1: Generate Predictions

```
┌──────────┐      ┌──────────┐      ┌───────────┐      ┌──────────┐      ┌─────────┐
│ Mobile   │      │   API    │      │Application│      │Infra     │      │AI Engine│
│  App     │      │Controller│      │  Handler  │      │Repository│      │         │
└────┬─────┘      └────┬─────┘      └─────┬─────┘      └────┬─────┘      └────┬────┘
     │                 │                   │                 │                 │
     │ GET /predictions│                   │                 │                 │
     ├────────────────>│                   │                 │                 │
     │                 │ GetUserPredictions│                 │                 │
     │                 │      Query        │                 │                 │
     │                 ├──────────────────>│                 │                 │
     │                 │                   │ Check Redis     │                 │
     │                 │                   │ Cache           │                 │
     │                 │                   ├────────────────>│                 │
     │                 │                   │ MISS            │                 │
     │                 │                   │<────────────────┤                 │
     │                 │                   │                 │                 │
     │                 │                   │ Get Transactions│                 │
     │                 │                   ├────────────────>│                 │
     │                 │                   │<────────────────┤                 │
     │                 │                   │                 │                 │
     │                 │                   │ Generate        │                 │
     │                 │                   │ Predictions     │                 │
     │                 │                   ├─────────────────────────────────>│
     │                 │                   │                 │                 │
     │                 │                   │                 │ AI Analysis     │
     │                 │                   │                 │ (Pattern        │
     │                 │                   │                 │  Detection)     │
     │                 │                   │                 │                 │
     │                 │                   │ AI Response     │                 │
     │                 │                   │<─────────────────────────────────┤
     │                 │                   │                 │                 │
     │                 │                   │ Map to Domain   │                 │
     │                 │                   │ Model           │                 │
     │                 │                   │                 │                 │
     │                 │                   │ Save Prediction │                 │
     │                 │                   ├────────────────>│                 │
     │                 │                   │<────────────────┤                 │
     │                 │                   │                 │                 │
     │                 │                   │ Cache Result    │                 │
     │                 │                   ├────────────────>│                 │
     │                 │                   │                 │                 │
     │                 │ PredictionDto     │                 │                 │
     │                 │<──────────────────┤                 │                 │
     │                 │                   │                 │                 │
     │ 200 OK          │                   │                 │                 │
     │ {predictions}   │                   │                 │                 │
     │<────────────────┤                   │                 │                 │
     │                 │                   │                 │                 │
```

### Sequence 2: Keep/Discard Flow

```
┌──────────┐      ┌──────────┐      ┌───────────┐      ┌──────────┐      ┌─────────┐
│ Mobile   │      │   API    │      │Application│      │  Domain  │      │AI Engine│
│  App     │      │Controller│      │  Handler  │      │ Aggregate│      │         │
└────┬─────┘      └────┬─────┘      └─────┬─────┘      └────┬─────┘      └────┬────┘
     │                 │                   │                 │                 │
     │ User taps KEEP  │                   │                 │                 │
     │ on predictions  │                   │                 │                 │
     │                 │                   │                 │                 │
     │ POST /feedback  │                   │                 │                 │
     ├────────────────>│                   │                 │                 │
     │                 │ ProcessUserFeedback                 │                 │
     │                 │      Command      │                 │                 │
     │                 ├──────────────────>│                 │                 │
     │                 │                   │ Load Prediction │                 │
     │                 │                   │ Aggregate       │                 │
     │                 │                   ├────────────────>│                 │
     │                 │                   │<────────────────┤                 │
     │                 │                   │                 │                 │
     │                 │                   │ Accept Items    │                 │
     │                 │                   │ (Keep)          │                 │
     │                 │                   ├────────────────>│                 │
     │                 │                   │                 │                 │
     │                 │                   │ Raise Events:   │                 │
     │                 │                   │ - PredictionAccepted              │
     │                 │                   │ - PredictionRejected              │
     │                 │                   │<────────────────┤                 │
     │                 │                   │                 │                 │
     │                 │                   │ Save Changes    │                 │
     │                 │                   │ (UnitOfWork)    │                 │
     │                 │                   │                 │                 │
     │                 │ 204 No Content    │                 │                 │
     │<────────────────┤<──────────────────┤                 │                 │
     │                 │                   │                 │                 │
     │                 │                   │ Event Handler:  │                 │
     │                 │                   │ PredictionAccepted                │
     │                 │                   │                 │                 │
     │                 │                   │ Create          │                 │
     │                 │                   │ CalendarItem    │                 │
     │                 │                   │                 │                 │
     │                 │                   │ Schedule        │                 │
     │                 │                   │ Reminder        │                 │
     │                 │                   │                 │                 │
     │                 │                   │ Send Feedback   │                 │
     │                 │                   │ to AI           │                 │
     │                 │                   ├─────────────────────────────────>│
     │                 │                   │                 │                 │
     │                 │                   │                 │ AI learns       │
     │                 │                   │                 │ preferences     │
     │                 │                   │                 │                 │
```

### Sequence 3: Reminder Flow

```
┌──────────┐      ┌──────────┐      ┌───────────┐      ┌──────────┐
│Hangfire  │      │Background│      │Notification       │ Mobile   │
│Scheduler │      │   Job    │      │  Service  │      │  App     │
└────┬─────┘      └────┬─────┘      └─────┬─────┘      └────┬─────┘
     │                 │                   │                 │
     │ Scheduled time  │                   │                 │
     │ reached         │                   │                 │
     ├────────────────>│                   │                 │
     │                 │                   │                 │
     │                 │ Execute           │                 │
     │                 │ SendReminderJob   │                 │
     │                 │                   │                 │
     │                 │ Load Reminder     │                 │
     │                 │ & CalendarItem    │                 │
     │                 │                   │                 │
     │                 │ Send Push         │                 │
     │                 │ Notification      │                 │
     │                 ├──────────────────>│                 │
     │                 │                   │                 │
     │                 │                   │ Push to device  │
     │                 │                   ├────────────────>│
     │                 │                   │                 │
     │                 │                   │                 │ "₦5,000 PHCN
     │                 │                   │                 │  bill due
     │                 │                   │                 │  tomorrow"
     │                 │                   │                 │
     │                 │ Update Reminder   │                 │
     │                 │ Status = Sent     │                 │
     │                 │                   │                 │
```

---

## 🗂️ File Structure Mapping

### Domain → Product Requirements

| Product Requirement | Domain File | Purpose |
|---------------------|------------|---------|
| **Max 10 predictions** | `Prediction.cs` | Aggregate enforces max 10 items rule |
| **Keep/Discard** | `PredictionItem.cs` | `IsAccepted` property tracks user decision |
| **Confidence scoring** | `ConfidenceScore.cs` | Maps AI's `final_confidence` to High/Med/Low |
| **Pattern detection** | `PatternType.cs` | Maps AI's `is_recurring` to Recurring/OneTime |
| **Finalized calendar** | `Calendar.cs` | Contains only accepted predictions |
| **Reminders** | `Reminder.cs` | Scheduled notifications for calendar items |
| **User feedback** | `UserFeedback.cs` | Captures Keep/Discard for AI learning |

### Application → User Stories

| User Story | Application File | Implementation |
|-----------|-----------------|----------------|
| **"I want ALAT to predict payments"** | `GeneratePredictionsCommandHandler.cs` | Calls AI engine, maps response, saves predictions |
| **"I want to keep predictions I approve"** | `ProcessUserFeedbackCommandHandler.cs` | Sets `IsAccepted = true`, raises `PredictionAcceptedEvent` |
| **"I want to discard predictions"** | `ProcessUserFeedbackCommandHandler.cs` | Sets `IsAccepted = false`, raises `PredictionRejectedEvent` |
| **"I want my final calendar"** | `GetUserCalendarQueryHandler.cs` | Returns only accepted predictions as calendar items |
| **"I want reminders"** | `ScheduleReminderCommandHandler.cs` | Creates Hangfire job for push notification |

### Infrastructure → Technical Requirements

| Requirement | Infrastructure File | Technology |
|------------|-------------------|-----------|
| **≤5 sec generation** | `AIServiceClient.cs` | gRPC + Polly retry + Redis cache |
| **≥70% accuracy** | AI engine (external) | Rules + ML model |
| **≥98% notification delivery** | `NotificationService.cs` | Hangfire + retry logic |
| **Zero PII leakage** | `ApplicationDbContext.cs` | Encrypted columns, masked logs |

---

## 🚀 Implementation Guide

### Phase 1: Foundation (Week 1-2)

1. **Domain Layer**
   - Implement `Prediction`, `PredictionItem`, `ConfidenceScore` aggregates
   - Create domain events
   - Define repositories

2. **Infrastructure - Database**
   - Create EF Core configurations
   - Generate initial migration
   - Seed sample transaction data

### Phase 2: AI Integration (Week 3-4)

1. **Infrastructure - AI Service**
   - Implement `AIServiceClient` with gRPC
   - Define proto files with AI team
   - Add Polly resilience policies

2. **Application - Commands**
   - `GeneratePredictionsCommandHandler`
   - Map AI response to domain model
   - Validate max 10 items

### Phase 3: User Interaction (Week 5-6)

1. **Application - Feedback**
   - `ProcessUserFeedbackCommandHandler`
   - Event handlers for Keep/Discard
   - Create `Calendar` from accepted items

2. **API - Controllers**
   - `PredictionsController` endpoints
   - `CalendarController` endpoints
   - SignalR hub for real-time updates

### Phase 4: Reminders & Background Jobs (Week 7-8)

1. **Application - Reminders**
   - `ScheduleReminderCommandHandler`
   - `SendReminderJob` background job

2. **Infrastructure - Hangfire**
   - Configure batch prediction job
   - Configure cleanup job
   - Dashboard setup

### Phase 5: Caching & Optimization (Week 9-10)

1. **Infrastructure - Redis**
   - `RedisCacheService` implementation
   - Cache predictions (7-day TTL)
   - Cache calendar (30-day TTL)

2. **Application - Performance**
   - Add caching behaviour to queries
   - Optimize database queries
   - Load testing

---

## 📈 Key Metrics to Track

| Metric | Target | Where Logged |
|--------|--------|-------------|
| Prediction generation time | ≤5 seconds | `LoggingBehaviour.cs` |
| Prediction accuracy | ≥70% | User feedback analysis |
| Cache hit rate | ≥85% | Redis metrics |
| Notification delivery | ≥98% | `NotificationService.cs` |
| Keep/Discard ratio | Monitor | `UserFeedbackRepository` |

---

## 🎯 Next Steps

1. **Review this document** with development team
2. **Align with AI team** on gRPC contract and response format
3. **Create initial database migrations** for Domain entities
4. **Implement core aggregates** (Prediction, Calendar)
5. **Build AI service client** and test integration
6. **Develop mobile app screens** for Suggested Calendar

---

## 📚 Additional Resources

- [Clean Architecture Principles](docs/architecture/ADR-001-CleanArchitecture.md)
- [CQRS with MediatR](docs/architecture/ADR-002-CQRS-MediatR.md)
- [gRPC Contract Definition](docs/api/gRPC-Contract.md)
- [Database Schema](docs/database/DatabaseSchema.md)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-29
**Maintained By:** Backend Team Lead
