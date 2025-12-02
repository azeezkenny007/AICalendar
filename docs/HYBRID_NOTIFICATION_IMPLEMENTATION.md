# Hybrid Notification Implementation Guide (Firebase + SignalR)

## Overview

This guide combines **Firebase Cloud Messaging** and **SignalR** to provide the best of both worlds:

- **Firebase FCM**: Reliable background notifications (app closed/background)
- **SignalR**: Instant real-time updates (app open/active)

**🎯 Strategy:**
1. **Always send Firebase notification** (guaranteed delivery)
2. **Also send SignalR update** (instant delivery if connected)
3. Frontend receives whichever arrives first

This ensures:
- ✅ Critical reminders always delivered (Firebase)
- ✅ Instant updates when user is active (SignalR)
- ✅ No duplicate UI updates (deduplication logic)

---

## Phase 1: Backend Architecture

### Step 1.1: Unified Notification Interface

**File:** `src/AICalendar.Application/Services/Notifications/INotificationService.cs`

```csharp
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Application.Services.Notifications;

/// <summary>
/// Unified notification service that handles both Firebase and SignalR
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends notification via both Firebase (background) and SignalR (real-time)
    /// </summary>
    Task SendCalendarItemAddedNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        decimal amount,
        DateTime dueDate);

    /// <summary>
    /// Sends notification when calendar item is updated
    /// </summary>
    Task SendCalendarItemUpdatedNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        decimal amount,
        DateTime dueDate);

    /// <summary>
    /// Sends notification when item is marked as paid
    /// </summary>
    Task SendCalendarItemPaidNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        DateTime paidDate);

    /// <summary>
    /// Sends notification when item is removed
    /// </summary>
    Task SendCalendarItemRemovedNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant);

    /// <summary>
    /// Sends reminder notification at scheduled intervals
    /// Uses Firebase for reliability
    /// </summary>
    Task SendReminderNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        ReminderType reminderType,
        TimeSpan timeUntilDue);

    /// <summary>
    /// Sends notification when new predictions are generated
    /// </summary>
    Task SendPredictionsGeneratedNotification(
        UserId userId,
        int predictionCount);
}

public enum ReminderType
{
    PreDue24Hours,
    PreDue12Hours,
    PreDue3Hours,
    PreDue30Minutes,
    DueNow,
    Overdue3Hours,
    Overdue12Hours,
    Overdue24Hours
}
```

### Step 1.2: Hybrid Notification Service Implementation

**File:** `src/AICalendar.Infrastructure/Services/Notifications/HybridNotificationService.cs`

```csharp
using AICalendar.Application.Services.Notifications;
using AICalendar.Application.Services.SignalR;
using AICalendar.Domain.ValueObjects;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Services.Notifications;

/// <summary>
/// Hybrid notification service that sends via both Firebase and SignalR
/// </summary>
public class HybridNotificationService : INotificationService
{
    private readonly IFirebaseNotificationService _firebaseService;
    private readonly IRealtimeNotificationService _signalRService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<HybridNotificationService> _logger;

    public HybridNotificationService(
        IFirebaseNotificationService firebaseService,
        IRealtimeNotificationService signalRService,
        IUserRepository userRepository,
        ILogger<HybridNotificationService> logger)
    {
        _firebaseService = firebaseService;
        _signalRService = signalRService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task SendCalendarItemAddedNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        decimal amount,
        DateTime dueDate)
    {
        _logger.LogInformation(
            "Sending hybrid notification: CalendarItemAdded for user {UserId}, merchant {Merchant}",
            userId.Value, merchant);

        var data = new
        {
            ItemId = itemId.Value,
            Merchant = merchant,
            Amount = amount,
            DueDate = dueDate,
            Type = "CalendarItemAdded"
        };

        // Strategy: Fire both simultaneously, don't wait
        var firebaseTask = SendFirebaseNotification(
            userId,
            "New Calendar Item",
            $"{merchant} - ${amount:F2} due on {dueDate:MMM dd}",
            data);

        var signalRTask = _signalRService.NotifyCalendarItemAdded(userId, data);

        // Wait for both (but don't fail if one fails)
        await Task.WhenAll(
            WrapWithErrorHandling(firebaseTask, "Firebase", userId),
            WrapWithErrorHandling(signalRTask, "SignalR", userId)
        );
    }

    public async Task SendCalendarItemUpdatedNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        decimal amount,
        DateTime dueDate)
    {
        _logger.LogInformation(
            "Sending hybrid notification: CalendarItemUpdated for user {UserId}, item {ItemId}",
            userId.Value, itemId.Value);

        var data = new
        {
            ItemId = itemId.Value,
            Merchant = merchant,
            Amount = amount,
            DueDate = dueDate,
            Type = "CalendarItemUpdated"
        };

        var firebaseTask = SendFirebaseNotification(
            userId,
            "Calendar Item Updated",
            $"{merchant} updated - ${amount:F2} due {dueDate:MMM dd}",
            data);

        var signalRTask = _signalRService.NotifyCalendarItemUpdated(userId, data);

        await Task.WhenAll(
            WrapWithErrorHandling(firebaseTask, "Firebase", userId),
            WrapWithErrorHandling(signalRTask, "SignalR", userId)
        );
    }

    public async Task SendCalendarItemPaidNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        DateTime paidDate)
    {
        _logger.LogInformation(
            "Sending hybrid notification: CalendarItemPaid for user {UserId}, item {ItemId}",
            userId.Value, itemId.Value);

        var data = new
        {
            ItemId = itemId.Value,
            Merchant = merchant,
            PaidDate = paidDate,
            Type = "CalendarItemPaid"
        };

        var firebaseTask = SendFirebaseNotification(
            userId,
            "Payment Completed",
            $"{merchant} marked as paid",
            data);

        var signalRTask = _signalRService.NotifyCalendarItemPaid(userId, itemId, paidDate);

        await Task.WhenAll(
            WrapWithErrorHandling(firebaseTask, "Firebase", userId),
            WrapWithErrorHandling(signalRTask, "SignalR", userId)
        );
    }

    public async Task SendCalendarItemRemovedNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant)
    {
        _logger.LogInformation(
            "Sending hybrid notification: CalendarItemRemoved for user {UserId}, item {ItemId}",
            userId.Value, itemId.Value);

        var data = new
        {
            ItemId = itemId.Value,
            Merchant = merchant,
            Type = "CalendarItemRemoved"
        };

        var firebaseTask = SendFirebaseNotification(
            userId,
            "Calendar Item Removed",
            $"{merchant} removed from calendar",
            data);

        var signalRTask = _signalRService.NotifyCalendarItemRemoved(userId, itemId);

        await Task.WhenAll(
            WrapWithErrorHandling(firebaseTask, "Firebase", userId),
            WrapWithErrorHandling(signalRTask, "SignalR", userId)
        );
    }

    public async Task SendReminderNotification(
        UserId userId,
        CalendarItemId itemId,
        string merchant,
        decimal amount,
        DateTime dueDate,
        ReminderType reminderType,
        TimeSpan timeUntilDue)
    {
        _logger.LogInformation(
            "Sending hybrid reminder: {ReminderType} for user {UserId}, merchant {Merchant}",
            reminderType, userId.Value, merchant);

        var (title, body) = GetReminderMessage(merchant, amount, dueDate, reminderType, timeUntilDue);

        var data = new
        {
            ItemId = itemId.Value,
            Merchant = merchant,
            Amount = amount,
            DueDate = dueDate,
            ReminderType = reminderType.ToString(),
            Type = "Reminder"
        };

        // For reminders, Firebase is critical (user might not be in app)
        // SignalR is bonus (instant update if they are in app)
        var firebaseTask = SendFirebaseNotification(userId, title, body, data, isHighPriority: true);
        var signalRTask = _signalRService.NotifyUpcomingReminder(userId, merchant, dueDate, amount);

        await Task.WhenAll(
            WrapWithErrorHandling(firebaseTask, "Firebase", userId),
            WrapWithErrorHandling(signalRTask, "SignalR", userId)
        );
    }

    public async Task SendPredictionsGeneratedNotification(UserId userId, int predictionCount)
    {
        _logger.LogInformation(
            "Sending hybrid notification: PredictionsGenerated for user {UserId}, count {Count}",
            userId.Value, predictionCount);

        var data = new
        {
            Count = predictionCount,
            Timestamp = DateTime.UtcNow,
            Type = "PredictionsGenerated"
        };

        var firebaseTask = SendFirebaseNotification(
            userId,
            "New Predictions Available",
            $"{predictionCount} new payment predictions ready to review",
            data);

        var signalRTask = _signalRService.NotifyPredictionsGenerated(userId, predictionCount);

        await Task.WhenAll(
            WrapWithErrorHandling(firebaseTask, "Firebase", userId),
            WrapWithErrorHandling(signalRTask, "SignalR", userId)
        );
    }

    // ============================================
    // PRIVATE HELPER METHODS
    // ============================================

    private async Task SendFirebaseNotification(
        UserId userId,
        string title,
        string body,
        object data,
        bool isHighPriority = false)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user?.FcmToken == null)
        {
            _logger.LogWarning("User {UserId} has no FCM token, skipping Firebase notification", userId.Value);
            return;
        }

        await _firebaseService.SendNotificationAsync(
            user.FcmToken,
            title,
            body,
            data,
            isHighPriority);
    }

    private (string Title, string Body) GetReminderMessage(
        string merchant,
        decimal amount,
        DateTime dueDate,
        ReminderType reminderType,
        TimeSpan timeUntilDue)
    {
        return reminderType switch
        {
            ReminderType.PreDue24Hours => (
                "Payment Reminder - 24 Hours",
                $"{merchant} payment of ${amount:F2} due tomorrow at {dueDate:h:mm tt}"
            ),
            ReminderType.PreDue12Hours => (
                "Payment Reminder - 12 Hours",
                $"{merchant} payment of ${amount:F2} due in 12 hours ({dueDate:h:mm tt})"
            ),
            ReminderType.PreDue3Hours => (
                "Payment Reminder - 3 Hours",
                $"{merchant} payment of ${amount:F2} due in 3 hours ({dueDate:h:mm tt})"
            ),
            ReminderType.PreDue30Minutes => (
                "Payment Reminder - 30 Minutes",
                $"{merchant} payment of ${amount:F2} due in 30 minutes!"
            ),
            ReminderType.DueNow => (
                "Payment Due Now",
                $"{merchant} payment of ${amount:F2} is due now"
            ),
            ReminderType.Overdue3Hours => (
                "Overdue Payment",
                $"{merchant} payment of ${amount:F2} is 3 hours overdue"
            ),
            ReminderType.Overdue12Hours => (
                "Overdue Payment",
                $"{merchant} payment of ${amount:F2} is 12 hours overdue"
            ),
            ReminderType.Overdue24Hours => (
                "Overdue Payment",
                $"{merchant} payment of ${amount:F2} is 24 hours overdue"
            ),
            _ => ("Payment Reminder", $"{merchant} - ${amount:F2}")
        };
    }

    private async Task WrapWithErrorHandling(Task task, string serviceName, UserId userId)
    {
        try
        {
            await task;
            _logger.LogDebug("{ServiceName} notification sent successfully for user {UserId}",
                serviceName, userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{ServiceName} notification failed for user {UserId}. Continuing with other services.",
                serviceName, userId.Value);
            // Don't throw - we want other notifications to continue
        }
    }
}
```

### Step 1.3: Update Dependency Registration

**File:** `src/AICalendar.API/Program.cs`

```csharp
// ============================================
// NOTIFICATION SERVICES (HYBRID)
// ============================================

// 1. Register Firebase service
builder.Services.AddSingleton<AICalendar.Application.Services.Notifications.IFirebaseNotificationService,
    AICalendar.Infrastructure.Services.Notifications.FirebaseNotificationService>();

// 2. Register SignalR service
builder.Services.AddScoped<AICalendar.Application.Services.SignalR.IRealtimeNotificationService,
    AICalendar.Infrastructure.Services.SignalR.RealtimeNotificationService>();

// 3. Register Hybrid service (uses both Firebase + SignalR)
builder.Services.AddScoped<AICalendar.Application.Services.Notifications.INotificationService,
    AICalendar.Infrastructure.Services.Notifications.HybridNotificationService>();
```

### Step 1.4: Update Event Handlers

**Example:** `src/AICalendar.Application/Predictions/DomainEventHandlers/PredictionAcceptedCalendarHandler.cs`

```csharp
using AICalendar.Application.Services.Notifications;

public class PredictionAcceptedCalendarHandler : IDomainEventHandler<PredictionAcceptedEvent>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly INotificationService _notificationService; // 🆕 HYBRID SERVICE
    private readonly ILogger<PredictionAcceptedCalendarHandler> _logger;

    public PredictionAcceptedCalendarHandler(
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        INotificationService notificationService, // 🆕
        ILogger<PredictionAcceptedCalendarHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(PredictionAcceptedEvent domainEvent, CancellationToken cancellationToken)
    {
        // ... existing code to add calendar items ...

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);

        // 🆕 SEND HYBRID NOTIFICATIONS (Firebase + SignalR)
        foreach (var item in domainEvent.Items)
        {
            await _notificationService.SendCalendarItemAddedNotification(
                domainEvent.UserId,
                CalendarItemId.Create(), // Get actual ID from saved item
                item.Merchant,
                item.Amount,
                item.DueDate
            );
        }

        _logger.LogInformation("Added {Count} calendar items and sent hybrid notifications for user {UserId}",
            domainEvent.Items.Count, domainEvent.UserId.Value);
    }
}
```

Similarly update:
- `EditCalendarItemCommandHandler` → `SendCalendarItemUpdatedNotification`
- `MarkItemAsPaidCommandHandler` → `SendCalendarItemPaidNotification`
- Calendar `RemoveItem` handler → `SendCalendarItemRemovedNotification`

### Step 1.5: Update SendRemindersJob

**File:** `src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs`

```csharp
using AICalendar.Application.Services.Notifications;
using AICalendar.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.BackgroundJobs;

public class SendRemindersJob
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly INotificationService _notificationService; // 🆕 HYBRID
    private readonly ILogger<SendRemindersJob> _logger;

    public SendRemindersJob(
        ICalendarRepository calendarRepository,
        INotificationService notificationService,
        ILogger<SendRemindersJob> logger)
    {
        _calendarRepository = calendarRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task SendDueReminders()
    {
        _logger.LogInformation("Starting hybrid reminder processing at {Time}", DateTime.UtcNow);

        try
        {
            var now = DateTime.UtcNow;

            // Define reminder windows
            var reminderWindows = new[]
            {
                (Type: ReminderType.PreDue24Hours, Start: now.AddHours(24), End: now.AddHours(24).AddMinutes(5)),
                (Type: ReminderType.PreDue12Hours, Start: now.AddHours(12), End: now.AddHours(12).AddMinutes(5)),
                (Type: ReminderType.PreDue3Hours, Start: now.AddHours(3), End: now.AddHours(3).AddMinutes(5)),
                (Type: ReminderType.PreDue30Minutes, Start: now.AddMinutes(30), End: now.AddMinutes(35)),
                (Type: ReminderType.DueNow, Start: now.AddMinutes(-5), End: now.AddMinutes(5)),
                (Type: ReminderType.Overdue3Hours, Start: now.AddHours(-3).AddMinutes(-5), End: now.AddHours(-3)),
                (Type: ReminderType.Overdue12Hours, Start: now.AddHours(-12).AddMinutes(-5), End: now.AddHours(-12)),
                (Type: ReminderType.Overdue24Hours, Start: now.AddHours(-24).AddMinutes(-5), End: now.AddHours(-24))
            };

            foreach (var window in reminderWindows)
            {
                // Get unpaid items with due dates in this window
                var items = await _calendarRepository.GetUnpaidItemsByDueDateRangeAsync(
                    window.Start,
                    window.End
                );

                _logger.LogInformation("Found {Count} items for {ReminderType} reminder",
                    items.Count, window.Type);

                foreach (var item in items)
                {
                    var timeUntilDue = item.DueDate - now;

                    // 🆕 SEND HYBRID NOTIFICATION (Firebase + SignalR)
                    await _notificationService.SendReminderNotification(
                        item.Calendar.UserId,
                        item.Id,
                        item.Merchant,
                        item.Amount,
                        item.DueDate,
                        window.Type,
                        timeUntilDue
                    );
                }
            }

            _logger.LogInformation("Hybrid reminder processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hybrid reminder processing");
            throw;
        }
    }
}
```

---

## Phase 2: Frontend Integration (Deduplication)

### Strategy: Prevent Duplicate Notifications

The frontend will receive the same notification from both Firebase and SignalR. We need deduplication logic.

### Flutter Implementation

**File:** `lib/services/notification_deduplicator.dart`

```dart
class NotificationDeduplicator {
  // Track recently processed notification IDs
  final Map<String, DateTime> _processedNotifications = {};
  final Duration _deduplicationWindow = Duration(seconds: 5);

  /// Returns true if notification should be processed, false if duplicate
  bool shouldProcess(String notificationId) {
    final now = DateTime.now();

    // Clean up old entries
    _processedNotifications.removeWhere(
      (key, timestamp) => now.difference(timestamp) > _deduplicationWindow,
    );

    // Check if already processed recently
    if (_processedNotifications.containsKey(notificationId)) {
      print('Duplicate notification detected: $notificationId');
      return false;
    }

    // Mark as processed
    _processedNotifications[notificationId] = now;
    return true;
  }

  String generateNotificationId(String type, String itemId) {
    return '$type:$itemId';
  }
}
```

**File:** `lib/services/unified_notification_handler.dart`

```dart
import 'package:firebase_messaging/firebase_messaging.dart';
import 'notification_deduplicator.dart';

class UnifiedNotificationHandler {
  final NotificationDeduplicator _deduplicator = NotificationDeduplicator();

  void handleFirebaseMessage(RemoteMessage message) {
    final data = message.data;
    final type = data['Type'] as String?;
    final itemId = data['ItemId'] as String?;

    if (type == null || itemId == null) return;

    final notificationId = _deduplicator.generateNotificationId(type, itemId);

    if (_deduplicator.shouldProcess(notificationId)) {
      _processNotification(type, data);
    }
  }

  void handleSignalREvent(String eventType, Map<String, dynamic> data) {
    final itemId = data['ItemId'] as String?;

    if (itemId == null) return;

    final notificationId = _deduplicator.generateNotificationId(eventType, itemId);

    if (_deduplicator.shouldProcess(notificationId)) {
      _processNotification(eventType, data);
    }
  }

  void _processNotification(String type, Map<String, dynamic> data) {
    print('Processing notification: $type');

    switch (type) {
      case 'CalendarItemAdded':
        // Update UI: Add item to calendar list
        _handleCalendarItemAdded(data);
        break;
      case 'CalendarItemUpdated':
        // Update UI: Refresh item in calendar list
        _handleCalendarItemUpdated(data);
        break;
      case 'CalendarItemPaid':
        // Update UI: Mark item as paid
        _handleCalendarItemPaid(data);
        break;
      case 'CalendarItemRemoved':
        // Update UI: Remove item from calendar list
        _handleCalendarItemRemoved(data);
        break;
      case 'Reminder':
        // Show reminder notification
        _handleReminder(data);
        break;
      case 'PredictionsGenerated':
        // Show badge or notification
        _handlePredictionsGenerated(data);
        break;
    }
  }

  void _handleCalendarItemAdded(Map<String, dynamic> data) {
    // Dispatch to your state management (Provider, Bloc, Riverpod, etc.)
    print('Calendar item added: ${data['Merchant']}');
  }

  void _handleCalendarItemUpdated(Map<String, dynamic> data) {
    print('Calendar item updated: ${data['ItemId']}');
  }

  void _handleCalendarItemPaid(Map<String, dynamic> data) {
    print('Calendar item paid: ${data['ItemId']}');
  }

  void _handleCalendarItemRemoved(Map<String, dynamic> data) {
    print('Calendar item removed: ${data['ItemId']}');
  }

  void _handleReminder(Map<String, dynamic> data) {
    print('Reminder: ${data['Merchant']} - ${data['ReminderType']}');
  }

  void _handlePredictionsGenerated(Map<String, dynamic> data) {
    print('Predictions generated: ${data['Count']}');
  }
}
```

**Usage:**

```dart
// In main.dart or app initialization
final notificationHandler = UnifiedNotificationHandler();

// Firebase setup
FirebaseMessaging.onMessage.listen((RemoteMessage message) {
  notificationHandler.handleFirebaseMessage(message);
});

FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
  notificationHandler.handleFirebaseMessage(message);
});

// SignalR setup
signalRService.connection.on('CalendarItemAdded', (args) {
  notificationHandler.handleSignalREvent('CalendarItemAdded', args[0]);
});

signalRService.connection.on('CalendarItemUpdated', (args) {
  notificationHandler.handleSignalREvent('CalendarItemUpdated', args[0]);
});
// ... etc for all events
```

### React Implementation

**File:** `src/services/notificationDeduplicator.ts`

```typescript
export class NotificationDeduplicator {
  private processedNotifications = new Map<string, number>();
  private readonly deduplicationWindow = 5000; // 5 seconds

  shouldProcess(notificationId: string): boolean {
    const now = Date.now();

    // Clean up old entries
    for (const [key, timestamp] of this.processedNotifications.entries()) {
      if (now - timestamp > this.deduplicationWindow) {
        this.processedNotifications.delete(key);
      }
    }

    // Check if already processed
    if (this.processedNotifications.has(notificationId)) {
      console.log('Duplicate notification detected:', notificationId);
      return false;
    }

    // Mark as processed
    this.processedNotifications.set(notificationId, now);
    return true;
  }

  generateNotificationId(type: string, itemId: string): string {
    return `${type}:${itemId}`;
  }
}
```

**File:** `src/services/unifiedNotificationHandler.ts`

```typescript
import { NotificationDeduplicator } from './notificationDeduplicator';

export class UnifiedNotificationHandler {
  private deduplicator = new NotificationDeduplicator();

  handleFirebaseMessage(payload: any): void {
    const { Type, ItemId } = payload.data;

    if (!Type || !ItemId) return;

    const notificationId = this.deduplicator.generateNotificationId(Type, ItemId);

    if (this.deduplicator.shouldProcess(notificationId)) {
      this.processNotification(Type, payload.data);
    }
  }

  handleSignalREvent(eventType: string, data: any): void {
    const itemId = data.ItemId;

    if (!itemId) return;

    const notificationId = this.deduplicator.generateNotificationId(eventType, itemId);

    if (this.deduplicator.shouldProcess(notificationId)) {
      this.processNotification(eventType, data);
    }
  }

  private processNotification(type: string, data: any): void {
    console.log('Processing notification:', type, data);

    switch (type) {
      case 'CalendarItemAdded':
        this.handleCalendarItemAdded(data);
        break;
      case 'CalendarItemUpdated':
        this.handleCalendarItemUpdated(data);
        break;
      case 'CalendarItemPaid':
        this.handleCalendarItemPaid(data);
        break;
      case 'CalendarItemRemoved':
        this.handleCalendarItemRemoved(data);
        break;
      case 'Reminder':
        this.handleReminder(data);
        break;
      case 'PredictionsGenerated':
        this.handlePredictionsGenerated(data);
        break;
    }
  }

  private handleCalendarItemAdded(data: any): void {
    // Dispatch Redux action or update React state
    window.dispatchEvent(new CustomEvent('calendar:refresh'));
  }

  private handleCalendarItemUpdated(data: any): void {
    window.dispatchEvent(new CustomEvent('calendar:item-updated', { detail: data }));
  }

  private handleCalendarItemPaid(data: any): void {
    window.dispatchEvent(new CustomEvent('calendar:item-paid', { detail: data }));
  }

  private handleCalendarItemRemoved(data: any): void {
    window.dispatchEvent(new CustomEvent('calendar:item-removed', { detail: data }));
  }

  private handleReminder(data: any): void {
    // Show toast or in-app notification
    console.log('Reminder:', data.Merchant, data.ReminderType);
  }

  private handlePredictionsGenerated(data: any): void {
    // Show badge
    console.log('Predictions generated:', data.Count);
  }
}
```

---

## Phase 3: Testing

### Test 1: Dual Delivery

**Goal:** Verify both Firebase and SignalR receive notification

1. Open app on device (SignalR connected)
2. Ensure Firebase token is registered
3. Add calendar item via API
4. **Expected:**
   - SignalR event received (instant, ~100ms)
   - Firebase notification received (~1-2 seconds)
   - Only ONE UI update (deduplication worked)

### Test 2: Firebase Fallback

**Goal:** Verify Firebase works when SignalR disconnected

1. Close app completely (SignalR disconnected)
2. Trigger reminder via Hangfire job
3. **Expected:**
   - Firebase notification appears in system tray
   - Tap notification opens app

### Test 3: SignalR Priority

**Goal:** Verify SignalR is faster when connected

1. Open app (SignalR connected)
2. Mark item as paid
3. **Expected:**
   - SignalR update appears instantly
   - Firebase notification arrives later but is deduplicated

### Test 4: Network Interruption

**Goal:** Test resilience

1. Open app with good internet
2. Disable WiFi/data for 30 seconds
3. Re-enable internet
4. Trigger notification
5. **Expected:**
   - SignalR reconnects automatically
   - Firebase notification still delivered

---

## Phase 4: Monitoring & Metrics

### Add Logging

```csharp
_logger.LogInformation(
    "Hybrid notification sent: {Type} | Firebase: {FirebaseSuccess} | SignalR: {SignalRSuccess} | User: {UserId}",
    notificationType, firebaseSuccess, signalRSuccess, userId);
```

### Track Metrics

**File:** `src/AICalendar.Infrastructure/Services/Notifications/NotificationMetrics.cs`

```csharp
public class NotificationMetrics
{
    public int FirebaseSuccessCount { get; set; }
    public int FirebaseFailureCount { get; set; }
    public int SignalRSuccessCount { get; set; }
    public int SignalRFailureCount { get; set; }
    public double AverageFirebaseLatencyMs { get; set; }
    public double AverageSignalRLatencyMs { get; set; }
}
```

### Dashboard Queries

Monitor in Hangfire dashboard or Application Insights:

```sql
-- Firebase delivery rate
SELECT
    COUNT(*) FILTER (WHERE firebase_success = true) * 100.0 / COUNT(*) as firebase_delivery_rate
FROM notification_logs
WHERE created_at > NOW() - INTERVAL '24 hours';

-- Average latency comparison
SELECT
    AVG(signalr_latency_ms) as signalr_avg_latency,
    AVG(firebase_latency_ms) as firebase_avg_latency
FROM notification_logs
WHERE created_at > NOW() - INTERVAL '1 hour';
```

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    BACKEND (ASP.NET Core)                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Event Handlers / Background Jobs                          │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────────────────────┐                          │
│  │ HybridNotificationService    │                          │
│  │  (INotificationService)      │                          │
│  └─────────┬───────────┬────────┘                          │
│            │           │                                    │
│    ┌───────▼─────┐  ┌──▼──────────────┐                   │
│    │  Firebase   │  │  SignalR        │                   │
│    │  Service    │  │  Service        │                   │
│    └───────┬─────┘  └──┬──────────────┘                   │
│            │           │                                    │
└────────────┼───────────┼────────────────────────────────────┘
             │           │
             │           │ (WebSocket)
             │           │
    (FCM Cloud) │           │
             │           │
             ▼           ▼
┌─────────────────────────────────────────────────────────────┐
│                  FRONTEND (Flutter/React)                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Firebase Handler ──┐       SignalR Handler                │
│                     │              │                        │
│                     ▼              ▼                        │
│              ┌───────────────────────┐                      │
│              │ NotificationDedup     │                      │
│              │ (5-second window)     │                      │
│              └──────────┬────────────┘                      │
│                         │                                   │
│                         ▼                                   │
│              ┌───────────────────────┐                      │
│              │ Process Notification  │                      │
│              │ (Update UI once)      │                      │
│              └───────────────────────┘                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘

Notification Flow:
1. Backend sends BOTH Firebase + SignalR simultaneously
2. Frontend receives whichever arrives first
3. Deduplicator prevents second one from causing UI update
4. Result: Best latency + guaranteed delivery
```

---

## Summary

| Aspect | Implementation |
|--------|---------------|
| **Backend** | `HybridNotificationService` sends both Firebase + SignalR |
| **Frontend** | `NotificationDeduplicator` prevents duplicate UI updates |
| **Latency** | SignalR: ~100ms, Firebase: ~1-2s |
| **Reliability** | Firebase guarantees delivery even when app closed |
| **Best of both worlds** | ✅ Instant updates when online + Reliable background delivery |

---

## Checklist

### Backend
- [ ] Firebase implementation complete (see `FIREBASE_NOTIFICATION_IMPLEMENTATION.md`)
- [ ] SignalR implementation complete (see `SIGNALR_REALTIME_IMPLEMENTATION.md`)
- [ ] `INotificationService` interface created with all methods
- [ ] `HybridNotificationService` implemented
- [ ] Error handling with `WrapWithErrorHandling`
- [ ] Reminder message builder implemented
- [ ] DI registration updated in Program.cs
- [ ] Event handlers updated to use `INotificationService`
- [ ] SendRemindersJob updated with all 8 reminder types

### Frontend
- [ ] `NotificationDeduplicator` class created
- [ ] `UnifiedNotificationHandler` class created
- [ ] Firebase message handler integrated
- [ ] SignalR event handler integrated
- [ ] UI update logic implemented (state management)
- [ ] Deduplication window tested (5 seconds)

### Testing
- [ ] Dual delivery test passed
- [ ] Firebase fallback test passed
- [ ] SignalR priority test passed
- [ ] Network interruption test passed
- [ ] Deduplication test passed

### Monitoring
- [ ] Logging added for both services
- [ ] Metrics tracking implemented
- [ ] Dashboard/queries configured
- [ ] Alert thresholds set (e.g., <95% delivery rate)
