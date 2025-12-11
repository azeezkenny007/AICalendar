# SignalR Real-Time Updates Implementation Guide

## Overview

This guide covers implementing SignalR for **in-app real-time updates** when users are actively online. SignalR provides instant, bidirectional communication between server and client.

**⚠️ Important Limitations:**
- Only works when user has app open and connected
- Does NOT work when app is closed or in background
- Requires active WebSocket/Long-polling connection
- For background notifications, use Firebase (see `FIREBASE_NOTIFICATION_IMPLEMENTATION.md`)

**✅ Best Use Cases:**
- Live updates while user browses the app
- Instant reflection of changes made by user
- Real-time calendar synchronization
- Live prediction updates
- Collaborative features

---

## Phase 1: Backend Setup

### Step 1.1: Install SignalR Package

Already included in ASP.NET Core 6+, but verify:

```bash
# Check if already installed (should be included by default)
dotnet list package | findstr SignalR

# If not found, install:
dotnet add src/AICalendar.API/AICalendar.API.csproj package Microsoft.AspNetCore.SignalR
```

### Step 1.2: Create SignalR Hub

**File:** `src/AICalendar.API/Hubs/CalendarHub.cs`

```csharp
using Microsoft.AspNetCore.SignalR;

namespace AICalendar.API.Hubs;

/// <summary>
/// SignalR hub for real-time calendar updates
/// </summary>
public class CalendarHub : Hub
{
    private readonly ILogger<CalendarHub> _logger;

    public CalendarHub(ILogger<CalendarHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier; // Requires authentication
        _logger.LogInformation("User {UserId} connected to CalendarHub. ConnectionId: {ConnectionId}",
            userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from CalendarHub. ConnectionId: {ConnectionId}",
            userId, Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with error", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can join their personal calendar room
    /// </summary>
    public async Task JoinCalendarRoom(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogInformation("User {UserId} joined their calendar room", userId);
    }

    /// <summary>
    /// Client can leave their calendar room
    /// </summary>
    public async Task LeaveCalendarRoom(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogInformation("User {UserId} left their calendar room", userId);
    }
}
```

### Step 1.3: Create Real-Time Notification Service

**File:** `src/AICalendar.Application/Services/SignalR/IRealtimeNotificationService.cs`

```csharp
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Application.Services.SignalR;

/// <summary>
/// Service for sending real-time notifications to connected clients
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>
    /// Notifies user that a calendar item was added
    /// </summary>
    Task NotifyCalendarItemAdded(UserId userId, object calendarItem);

    /// <summary>
    /// Notifies user that a calendar item was updated
    /// </summary>
    Task NotifyCalendarItemUpdated(UserId userId, object calendarItem);

    /// <summary>
    /// Notifies user that a calendar item was marked as paid
    /// </summary>
    Task NotifyCalendarItemPaid(UserId userId, CalendarItemId itemId, DateTime paidDate);

    /// <summary>
    /// Notifies user that a calendar item was removed
    /// </summary>
    Task NotifyCalendarItemRemoved(UserId userId, CalendarItemId itemId);

    /// <summary>
    /// Notifies user that new predictions are available
    /// </summary>
    Task NotifyPredictionsGenerated(UserId userId, int predictionCount);

    /// <summary>
    /// Notifies user of an upcoming reminder
    /// </summary>
    Task NotifyUpcomingReminder(UserId userId, string merchant, DateTime dueDate, decimal amount);
}
```

**File:** `src/AICalendar.Infrastructure/Services/SignalR/RealtimeNotificationService.cs`

```csharp
using AICalendar.Application.Services.SignalR;
using AICalendar.API.Hubs;
using AICalendar.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Services.SignalR;

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<CalendarHub> _hubContext;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<CalendarHub> hubContext,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyCalendarItemAdded(UserId userId, object calendarItem)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId.Value}")
                .SendAsync("CalendarItemAdded", calendarItem);

            _logger.LogInformation("Sent CalendarItemAdded notification to user {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send CalendarItemAdded notification to user {UserId}", userId.Value);
        }
    }

    public async Task NotifyCalendarItemUpdated(UserId userId, object calendarItem)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId.Value}")
                .SendAsync("CalendarItemUpdated", calendarItem);

            _logger.LogInformation("Sent CalendarItemUpdated notification to user {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send CalendarItemUpdated notification to user {UserId}", userId.Value);
        }
    }

    public async Task NotifyCalendarItemPaid(UserId userId, CalendarItemId itemId, DateTime paidDate)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId.Value}")
                .SendAsync("CalendarItemPaid", new
                {
                    ItemId = itemId.Value,
                    PaidDate = paidDate
                });

            _logger.LogInformation("Sent CalendarItemPaid notification to user {UserId} for item {ItemId}",
                userId.Value, itemId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send CalendarItemPaid notification to user {UserId}", userId.Value);
        }
    }

    public async Task NotifyCalendarItemRemoved(UserId userId, CalendarItemId itemId)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId.Value}")
                .SendAsync("CalendarItemRemoved", new { ItemId = itemId.Value });

            _logger.LogInformation("Sent CalendarItemRemoved notification to user {UserId} for item {ItemId}",
                userId.Value, itemId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send CalendarItemRemoved notification to user {UserId}", userId.Value);
        }
    }

    public async Task NotifyPredictionsGenerated(UserId userId, int predictionCount)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId.Value}")
                .SendAsync("PredictionsGenerated", new
                {
                    Count = predictionCount,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent PredictionsGenerated notification to user {UserId} ({Count} predictions)",
                userId.Value, predictionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send PredictionsGenerated notification to user {UserId}", userId.Value);
        }
    }

    public async Task NotifyUpcomingReminder(UserId userId, string merchant, DateTime dueDate, decimal amount)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId.Value}")
                .SendAsync("UpcomingReminder", new
                {
                    Merchant = merchant,
                    DueDate = dueDate,
                    Amount = amount,
                    Message = $"Reminder: {merchant} payment of ${amount:F2} due soon"
                });

            _logger.LogInformation("Sent UpcomingReminder notification to user {UserId} for {Merchant}",
                userId.Value, merchant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send UpcomingReminder notification to user {UserId}", userId.Value);
        }
    }
}
```

### Step 1.4: Configure SignalR in Program.cs

**File:** `src/AICalendar.API/Program.cs`

Add after the existing service registrations:

```csharp
// ============================================
// SIGNALR CONFIGURATION
// ============================================
builder.Services.AddSignalR(options =>
{
    // Enable detailed errors in development
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();

    // Keep connection alive
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

    // Max message size (1MB)
    options.MaximumReceiveMessageSize = 1024 * 1024;
});

// Register realtime notification service
builder.Services.AddScoped<AICalendar.Application.Services.SignalR.IRealtimeNotificationService,
    AICalendar.Infrastructure.Services.SignalR.RealtimeNotificationService>();
```

Add endpoint mapping (after `app.UseAuthorization();`):

```csharp
// Map SignalR hubs
app.MapHub<AICalendar.API.Hubs.CalendarHub>("/hubs/calendar");
```

### Step 1.5: Integrate with Event Handlers

Update existing event handlers to send real-time notifications.

**Example:** `src/AICalendar.Application/Predictions/DomainEventHandlers/PredictionAcceptedCalendarHandler.cs`

```csharp
using AICalendar.Application.Services.SignalR;

public class PredictionAcceptedCalendarHandler : IDomainEventHandler<PredictionAcceptedEvent>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IRealtimeNotificationService _realtimeNotificationService; // ADD THIS
    private readonly ILogger<PredictionAcceptedCalendarHandler> _logger;

    public PredictionAcceptedCalendarHandler(
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IRealtimeNotificationService realtimeNotificationService, // ADD THIS
        ILogger<PredictionAcceptedCalendarHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _realtimeNotificationService = realtimeNotificationService; // ADD THIS
        _logger = logger;
    }

    public async Task Handle(PredictionAcceptedEvent domainEvent, CancellationToken cancellationToken)
    {
        // ... existing code ...

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);

        // 🆕 SEND REAL-TIME NOTIFICATION
        foreach (var item in domainEvent.Items)
        {
            await _realtimeNotificationService.NotifyCalendarItemAdded(
                domainEvent.UserId,
                new
                {
                    Merchant = item.Merchant,
                    Amount = item.Amount,
                    DueDate = item.DueDate,
                    Account = item.Account,
                    Description = item.Description
                });
        }

        _logger.LogInformation("Added {Count} calendar items for user {UserId} and sent real-time notifications",
            domainEvent.Items.Count, domainEvent.UserId.Value);
    }
}
```

Similarly update:
- `EditCalendarItemCommandHandler` → Call `NotifyCalendarItemUpdated`
- `MarkItemAsPaidCommandHandler` → Call `NotifyCalendarItemPaid`
- `RemoveCalendarItemCommandHandler` → Call `NotifyCalendarItemRemoved`

---

## Phase 2: Frontend Integration

### Option 2.1: Flutter Mobile App

**File:** `lib/services/signalr_service.dart`

```dart
import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter/foundation.dart';

class SignalRService {
  late HubConnection _hubConnection;
  final String userId;
  final Function(Map<String, dynamic>) onCalendarItemAdded;
  final Function(Map<String, dynamic>) onCalendarItemUpdated;
  final Function(Map<String, dynamic>) onCalendarItemPaid;
  final Function(Map<String, dynamic>) onCalendarItemRemoved;
  final Function(Map<String, dynamic>) onPredictionsGenerated;
  final Function(Map<String, dynamic>) onUpcomingReminder;

  SignalRService({
    required this.userId,
    required this.onCalendarItemAdded,
    required this.onCalendarItemUpdated,
    required this.onCalendarItemPaid,
    required this.onCalendarItemRemoved,
    required this.onPredictionsGenerated,
    required this.onUpcomingReminder,
  });

  Future<void> connect() async {
    // Create hub connection
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          'https://your-api.com/hubs/calendar',
          HttpConnectionOptions(
            accessTokenFactory: () async => await _getAccessToken(),
            logging: (level, message) => print('SignalR: $message'),
          ),
        )
        .withAutomaticReconnect()
        .build();

    // Register event handlers
    _hubConnection.on('CalendarItemAdded', (args) {
      if (args != null && args.isNotEmpty) {
        onCalendarItemAdded(args[0] as Map<String, dynamic>);
      }
    });

    _hubConnection.on('CalendarItemUpdated', (args) {
      if (args != null && args.isNotEmpty) {
        onCalendarItemUpdated(args[0] as Map<String, dynamic>);
      }
    });

    _hubConnection.on('CalendarItemPaid', (args) {
      if (args != null && args.isNotEmpty) {
        onCalendarItemPaid(args[0] as Map<String, dynamic>);
      }
    });

    _hubConnection.on('CalendarItemRemoved', (args) {
      if (args != null && args.isNotEmpty) {
        onCalendarItemRemoved(args[0] as Map<String, dynamic>);
      }
    });

    _hubConnection.on('PredictionsGenerated', (args) {
      if (args != null && args.isNotEmpty) {
        onPredictionsGenerated(args[0] as Map<String, dynamic>);
      }
    });

    _hubConnection.on('UpcomingReminder', (args) {
      if (args != null && args.isNotEmpty) {
        onUpcomingReminder(args[0] as Map<String, dynamic>);
      }
    });

    // Start connection
    await _hubConnection.start();
    print('SignalR Connected! ConnectionId: ${_hubConnection.connectionId}');

    // Join user's calendar room
    await _hubConnection.invoke('JoinCalendarRoom', args: [userId]);
  }

  Future<void> disconnect() async {
    await _hubConnection.invoke('LeaveCalendarRoom', args: [userId]);
    await _hubConnection.stop();
    print('SignalR Disconnected');
  }

  Future<String> _getAccessToken() async {
    // Get JWT token from your auth service
    // Example: return await AuthService.getToken();
    return 'your-jwt-token';
  }
}
```

**Usage in Flutter:**

```dart
// In your main app or calendar screen
class CalendarScreen extends StatefulWidget {
  @override
  _CalendarScreenState createState() => _CalendarScreenState();
}

class _CalendarScreenState extends State<CalendarScreen> {
  late SignalRService _signalRService;

  @override
  void initState() {
    super.initState();
    _initSignalR();
  }

  void _initSignalR() {
    _signalRService = SignalRService(
      userId: 'current-user-id',
      onCalendarItemAdded: (data) {
        setState(() {
          // Add item to local list
          print('New calendar item: ${data['Merchant']}');
          // Show snackbar or toast
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('New item added: ${data['Merchant']}')),
          );
        });
      },
      onCalendarItemUpdated: (data) {
        setState(() {
          // Update item in local list
          print('Calendar item updated');
        });
      },
      onCalendarItemPaid: (data) {
        setState(() {
          // Mark item as paid in local list
          print('Item paid: ${data['ItemId']}');
        });
      },
      onCalendarItemRemoved: (data) {
        setState(() {
          // Remove item from local list
          print('Item removed: ${data['ItemId']}');
        });
      },
      onPredictionsGenerated: (data) {
        // Show notification that new predictions are available
        print('New predictions: ${data['Count']}');
      },
      onUpcomingReminder: (data) {
        // Show in-app reminder notification
        print('Reminder: ${data['Message']}');
      },
    );

    _signalRService.connect();
  }

  @override
  void dispose() {
    _signalRService.disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    // Your calendar UI
    return Scaffold(
      appBar: AppBar(title: Text('Calendar')),
      body: ListView(/* calendar items */),
    );
  }
}
```

**Add dependency to `pubspec.yaml`:**

```yaml
dependencies:
  signalr_netcore: ^1.3.6
```

### Option 2.2: React Web App

**Install SignalR client:**

```bash
npm install @microsoft/signalr
```

**File:** `src/services/signalRService.ts`

```typescript
import * as signalR from '@microsoft/signalr';

export interface CalendarItem {
  merchant: string;
  amount: number;
  dueDate: string;
  account?: string;
  description?: string;
}

export class SignalRService {
  private connection: signalR.HubConnection;
  private userId: string;

  constructor(userId: string) {
    this.userId = userId;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://your-api.com/hubs/calendar', {
        accessTokenFactory: () => this.getAccessToken(),
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers(): void {
    this.connection.on('CalendarItemAdded', (item: CalendarItem) => {
      console.log('Calendar item added:', item);
      // Dispatch Redux action or update state
      window.dispatchEvent(new CustomEvent('calendar:item-added', { detail: item }));
    });

    this.connection.on('CalendarItemUpdated', (item: CalendarItem) => {
      console.log('Calendar item updated:', item);
      window.dispatchEvent(new CustomEvent('calendar:item-updated', { detail: item }));
    });

    this.connection.on('CalendarItemPaid', (data: { itemId: string; paidDate: string }) => {
      console.log('Calendar item paid:', data);
      window.dispatchEvent(new CustomEvent('calendar:item-paid', { detail: data }));
    });

    this.connection.on('CalendarItemRemoved', (data: { itemId: string }) => {
      console.log('Calendar item removed:', data);
      window.dispatchEvent(new CustomEvent('calendar:item-removed', { detail: data }));
    });

    this.connection.on('PredictionsGenerated', (data: { count: number; timestamp: string }) => {
      console.log('Predictions generated:', data);
      window.dispatchEvent(new CustomEvent('predictions:generated', { detail: data }));
    });

    this.connection.on('UpcomingReminder', (data: any) => {
      console.log('Upcoming reminder:', data);
      window.dispatchEvent(new CustomEvent('reminder:upcoming', { detail: data }));
      // Show toast notification
    });
  }

  async connect(): Promise<void> {
    try {
      await this.connection.start();
      console.log('SignalR Connected! ConnectionId:', this.connection.connectionId);

      // Join user's calendar room
      await this.connection.invoke('JoinCalendarRoom', this.userId);
    } catch (err) {
      console.error('SignalR connection error:', err);
      // Retry after 5 seconds
      setTimeout(() => this.connect(), 5000);
    }
  }

  async disconnect(): Promise<void> {
    try {
      await this.connection.invoke('LeaveCalendarRoom', this.userId);
      await this.connection.stop();
      console.log('SignalR Disconnected');
    } catch (err) {
      console.error('SignalR disconnect error:', err);
    }
  }

  private async getAccessToken(): Promise<string> {
    // Get JWT token from your auth service
    return localStorage.getItem('jwt-token') || '';
  }
}
```

**Usage in React:**

```typescript
// src/hooks/useSignalR.ts
import { useEffect, useState } from 'react';
import { SignalRService } from '../services/signalRService';

export function useSignalR(userId: string) {
  const [signalR, setSignalR] = useState<SignalRService | null>(null);

  useEffect(() => {
    const service = new SignalRService(userId);
    service.connect();
    setSignalR(service);

    return () => {
      service.disconnect();
    };
  }, [userId]);

  return signalR;
}

// In your Calendar component
function CalendarPage() {
  const userId = 'current-user-id';
  const signalR = useSignalR(userId);

  useEffect(() => {
    const handleItemAdded = (event: CustomEvent) => {
      console.log('New item added:', event.detail);
      // Update your state/Redux
    };

    window.addEventListener('calendar:item-added', handleItemAdded as EventListener);

    return () => {
      window.removeEventListener('calendar:item-added', handleItemAdded as EventListener);
    };
  }, []);

  return <div>Your calendar UI</div>;
}
```

---

## Phase 3: Testing

### Test 1: Connection Test

**Backend logs should show:**
```
User {userId} connected to CalendarHub. ConnectionId: {connectionId}
User {userId} joined their calendar room
```

**Frontend console should show:**
```
SignalR Connected! ConnectionId: abc123
```

### Test 2: Real-Time Update Test

1. Open calendar page in browser/app
2. Use Postman/Swagger to add a calendar item via API
3. Verify the item appears instantly in the UI without refreshing

### Test 3: Multi-Device Test

1. Open calendar on 2 devices (e.g., phone + web browser) with same user
2. Mark item as paid on device 1
3. Verify device 2 updates instantly

### Test 4: Reconnection Test

1. Connect to SignalR
2. Disable internet for 10 seconds
3. Re-enable internet
4. Verify SignalR reconnects automatically
5. Verify missed events are NOT replayed (this is expected)

---

## Phase 4: Production Considerations

### 4.1: Authentication

Enable authentication for SignalR:

```csharp
// In Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* your JWT config */);

// In CalendarHub.cs
[Authorize]
public class CalendarHub : Hub
{
    // Now Context.UserIdentifier will contain the authenticated user ID
}
```

### 4.2: Scaling with Redis Backplane

For multiple server instances, use Redis backplane:

```bash
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

```csharp
// In Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"), options =>
    {
        options.Configuration.ChannelPrefix = "aicalendar:signalr";
    });
```

### 4.3: Connection Limits

Monitor connection counts and set limits:

```csharp
builder.Services.AddSignalR(options =>
{
    options.MaximumParallelInvocationsPerClient = 1;
    options.StreamBufferCapacity = 10;
});
```

### 4.4: CORS Configuration

Ensure CORS allows SignalR:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins("https://your-frontend.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

app.UseCors("AllowSignalR");
```

---

## Troubleshooting

### Issue 1: "Failed to connect to SignalR"

**Solution:**
- Verify `/hubs/calendar` endpoint is mapped
- Check CORS allows credentials
- Verify JWT token is valid
- Check firewall/proxy doesn't block WebSockets

### Issue 2: "Connection drops frequently"

**Solution:**
- Increase `ClientTimeoutInterval` and `KeepAliveInterval`
- Check network stability
- Enable automatic reconnect on client

### Issue 3: "Events not received"

**Solution:**
- Verify client called `JoinCalendarRoom(userId)`
- Check `userId` matches between client and server
- Verify event handler is registered before connection starts

### Issue 4: "Multiple duplicate events"

**Solution:**
- Ensure you're not registering event handlers multiple times
- Use `connection.off('EventName')` before `connection.on('EventName')`

---

## Comparison: SignalR vs Firebase

| Feature | SignalR | Firebase |
|---------|---------|----------|
| **Works when app closed** | ❌ No | ✅ Yes |
| **Works when app in background** | ❌ No | ✅ Yes |
| **Battery usage** | Higher (persistent connection) | Lower (push only) |
| **Latency** | ~50-200ms | ~500ms-2s |
| **Requires internet** | ✅ Yes | ✅ Yes |
| **Best for** | In-app real-time updates | Background notifications |
| **Setup complexity** | Medium | Medium-High |
| **Cost** | Server bandwidth | Free tier: 10M/month |

---

## Summary

✅ **Use SignalR when:**
- User is actively using the app
- Need instant updates (< 200ms)
- Updates are only relevant while user is viewing the screen
- Examples: Live calendar sync, chat, collaborative editing

❌ **Don't use SignalR for:**
- Reminders when app is closed
- Critical notifications (payment due alerts)
- Battery-sensitive scenarios

For reminders and critical notifications, use **Firebase** (see `FIREBASE_NOTIFICATION_IMPLEMENTATION.md`) or the **Hybrid approach** (see `HYBRID_NOTIFICATION_IMPLEMENTATION.md`).

---

## Checklist

### Backend
- [ ] SignalR package verified/installed
- [ ] CalendarHub created with authentication
- [ ] IRealtimeNotificationService interface created
- [ ] RealtimeNotificationService implemented
- [ ] SignalR configured in Program.cs
- [ ] Hub endpoint mapped (`/hubs/calendar`)
- [ ] Event handlers integrated (PredictionAccepted, Edit, MarkPaid, Remove)
- [ ] CORS configured with `AllowCredentials`

### Frontend (Flutter)
- [ ] `signalr_netcore` package added to pubspec.yaml
- [ ] SignalRService class created
- [ ] Event handlers implemented
- [ ] Connection/disconnection in widget lifecycle
- [ ] Access token provider configured

### Frontend (React)
- [ ] `@microsoft/signalr` npm package installed
- [ ] SignalRService class created
- [ ] Custom events dispatched for state updates
- [ ] useSignalR hook created
- [ ] Reconnection handling implemented

### Testing
- [ ] Connection test passed
- [ ] Real-time update test passed
- [ ] Multi-device sync test passed
- [ ] Reconnection test passed

### Production
- [ ] Authentication enabled on hub
- [ ] Redis backplane configured (if scaling)
- [ ] Connection limits set
- [ ] Monitoring/logging configured
