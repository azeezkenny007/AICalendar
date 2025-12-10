# Hybrid Push Notifications with SignalR and Firebase

## Overview

This document explains how to implement a hybrid push notification system that combines SignalR for real-time in-app notifications with Firebase for traditional push notifications when the app is in the background or closed.

## Architecture

### The Hybrid Approach

```
┌─────────────────────────────────────────────────┐
│           Notification Service                  │
│                                                 │
│  ┌──────────────────────────────────────────┐ │
│  │   Determine User Connection Status        │ │
│  └──────────────────────────────────────────┘ │
│                    │                            │
│         ┌──────────┴──────────┐               │
│         ▼                     ▼                │
│  ┌─────────────┐      ┌──────────────┐       │
│  │   SignalR   │      │   Firebase   │       │
│  │  (Online)   │      │  (Offline)   │       │
│  └─────────────┘      └──────────────┘       │
└─────────────────────────────────────────────────┘
         │                      │
         ▼                      ▼
   ┌──────────┐          ┌──────────┐
   │  Active  │          │  Mobile  │
   │   User   │          │  Device  │
   └──────────┘          └──────────┘
```

### Key Concepts

1. **SignalR for Real-Time (In-App)**
   - Delivers instant notifications when user is actively connected
   - Provides real-time updates without polling
   - Removes need for in-app notification storage

2. **Firebase for Push Notifications (Out-of-App)**
   - Delivers notifications when app is closed or in background
   - Works across iOS and Android platforms
   - Reliable delivery through platform-specific services

3. **Hybrid Strategy**
   - Check if user is connected via SignalR
   - If connected → Send via SignalR
   - If not connected → Send via Firebase
   - No duplicate notifications

## Implementation Guide

### 1. SignalR Hub Setup

#### Create Notification Hub

```csharp
// Hubs/NotificationHub.cs
public class NotificationHub : Hub
{
    private readonly IConnectionManager _connectionManager;

    public NotificationHub(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Track user connection
            await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);

            // Join user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

#### Connection Manager Service

```csharp
// Services/ConnectionManager.cs
public interface IConnectionManager
{
    Task AddConnectionAsync(string userId, string connectionId);
    Task RemoveConnectionAsync(string userId, string connectionId);
    Task<bool> IsUserConnectedAsync(string userId);
    Task<List<string>> GetUserConnectionsAsync(string userId);
}

public class ConnectionManager : IConnectionManager
{
    private readonly IDistributedCache _cache;
    private const int ConnectionExpiryMinutes = 30;

    public ConnectionManager(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task AddConnectionAsync(string userId, string connectionId)
    {
        var key = $"signalr_connections:{userId}";
        var connections = await GetUserConnectionsAsync(userId);

        if (!connections.Contains(connectionId))
        {
            connections.Add(connectionId);
        }

        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(ConnectionExpiryMinutes)
        };

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(connections),
            options
        );
    }

    public async Task RemoveConnectionAsync(string userId, string connectionId)
    {
        var key = $"signalr_connections:{userId}";
        var connections = await GetUserConnectionsAsync(userId);

        connections.Remove(connectionId);

        if (connections.Any())
        {
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(ConnectionExpiryMinutes)
            };

            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(connections),
                options
            );
        }
        else
        {
            await _cache.RemoveAsync(key);
        }
    }

    public async Task<bool> IsUserConnectedAsync(string userId)
    {
        var connections = await GetUserConnectionsAsync(userId);
        return connections.Any();
    }

    public async Task<List<string>> GetUserConnectionsAsync(string userId)
    {
        var key = $"signalr_connections:{userId}";
        var data = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(data))
        {
            return new List<string>();
        }

        return JsonSerializer.Deserialize<List<string>>(data) ?? new List<string>();
    }
}
```

### 2. Hybrid Notification Service

```csharp
// Services/HybridNotificationService.cs
public interface IHybridNotificationService
{
    Task SendNotificationAsync(string userId, NotificationDto notification);
    Task SendNotificationToMultipleUsersAsync(List<string> userIds, NotificationDto notification);
}

public class HybridNotificationService : IHybridNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly IFirebaseService _firebaseService;
    private readonly ILogger<HybridNotificationService> _logger;

    public HybridNotificationService(
        IHubContext<NotificationHub> hubContext,
        IConnectionManager connectionManager,
        IFirebaseService firebaseService,
        ILogger<HybridNotificationService> logger)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _firebaseService = firebaseService;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string userId, NotificationDto notification)
    {
        try
        {
            // Check if user is connected via SignalR
            var isConnected = await _connectionManager.IsUserConnectedAsync(userId);

            if (isConnected)
            {
                // User is online - send via SignalR
                await SendViaSignalRAsync(userId, notification);
                _logger.LogInformation(
                    "Notification sent via SignalR to user {UserId}",
                    userId
                );
            }
            else
            {
                // User is offline - send via Firebase
                await SendViaFirebaseAsync(userId, notification);
                _logger.LogInformation(
                    "Notification sent via Firebase to user {UserId}",
                    userId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending notification to user {UserId}",
                userId
            );
            throw;
        }
    }

    public async Task SendNotificationToMultipleUsersAsync(
        List<string> userIds,
        NotificationDto notification)
    {
        var tasks = userIds.Select(userId => SendNotificationAsync(userId, notification));
        await Task.WhenAll(tasks);
    }

    private async Task SendViaSignalRAsync(string userId, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notification);
    }

    private async Task SendViaFirebaseAsync(string userId, NotificationDto notification)
    {
        // Get user's FCM token(s) from database
        var fcmTokens = await GetUserFcmTokensAsync(userId);

        if (!fcmTokens.Any())
        {
            _logger.LogWarning("No FCM tokens found for user {UserId}", userId);
            return;
        }

        // Send via Firebase
        await _firebaseService.SendPushNotificationAsync(fcmTokens, notification);
    }

    private async Task<List<string>> GetUserFcmTokensAsync(string userId)
    {
        // Implement logic to retrieve FCM tokens from your database
        // This should return all registered device tokens for the user
        throw new NotImplementedException();
    }
}
```

### 3. Notification DTO

```csharp
// DTOs/NotificationDto.cs
public class NotificationDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Data { get; set; }
    public string ActionUrl { get; set; }
    public string ImageUrl { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Transaction,
    Reminder,
    SystemAlert
}
```

### 4. Startup Configuration

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add SignalR
    services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

    // Add distributed cache for connection tracking (Redis recommended)
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = Configuration.GetConnectionString("Redis");
        options.InstanceName = "AICalendar_";
    });

    // Register services
    services.AddSingleton<IConnectionManager, ConnectionManager>();
    services.AddScoped<IHybridNotificationService, HybridNotificationService>();

    // Existing Firebase service
    services.AddScoped<IFirebaseService, FirebaseService>();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... other middleware

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapHub<NotificationHub>("/notificationHub");
    });
}
```

### 5. Usage Example

```csharp
// Example: Sending notification from a service or controller
public class TransactionService
{
    private readonly IHybridNotificationService _notificationService;

    public TransactionService(IHybridNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task ProcessTransactionAsync(Transaction transaction)
    {
        // Process transaction logic...

        // Send notification
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Transaction Completed",
            Message = $"Your transaction of ${transaction.Amount} was successful",
            Type = NotificationType.Transaction,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, string>
            {
                { "transactionId", transaction.Id.ToString() },
                { "amount", transaction.Amount.ToString() }
            },
            ActionUrl = $"/transactions/{transaction.Id}"
        };

        await _notificationService.SendNotificationAsync(
            transaction.UserId.ToString(),
            notification
        );
    }
}
```

## Client-Side Implementation

### JavaScript/TypeScript Client

```typescript
// signalRService.ts
import * as signalR from "@microsoft/signalr";

class SignalRService {
    private connection: signalR.HubConnection;

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub", {
                accessTokenFactory: () => this.getAccessToken()
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    // Custom retry logic
                    if (retryContext.elapsedMilliseconds < 60000) {
                        return Math.random() * 10000;
                    } else {
                        return null; // Stop retrying
                    }
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupConnectionHandlers();
    }

    private getAccessToken(): string {
        // Return JWT token from your auth system
        return localStorage.getItem('accessToken') || '';
    }

    private setupConnectionHandlers(): void {
        this.connection.on("ReceiveNotification", (notification) => {
            this.handleNotification(notification);
        });

        this.connection.onreconnecting((error) => {
            console.log("SignalR reconnecting:", error);
        });

        this.connection.onreconnected((connectionId) => {
            console.log("SignalR reconnected:", connectionId);
        });

        this.connection.onclose((error) => {
            console.log("SignalR connection closed:", error);
        });
    }

    public async start(): Promise<void> {
        try {
            await this.connection.start();
            console.log("SignalR connected");
        } catch (err) {
            console.error("SignalR connection error:", err);
            setTimeout(() => this.start(), 5000);
        }
    }

    public async stop(): Promise<void> {
        await this.connection.stop();
    }

    private handleNotification(notification: any): void {
        // Display notification in UI
        this.showInAppNotification(notification);

        // Trigger any callbacks
        this.notifyListeners(notification);
    }

    private showInAppNotification(notification: any): void {
        // Implement your UI notification logic
        // Examples: toast, modal, notification dropdown, etc.
        console.log("New notification:", notification);
    }

    private notifyListeners(notification: any): void {
        // Notify any registered listeners (e.g., notification store in state management)
    }
}

export const signalRService = new SignalRService();
```

### React Example

```typescript
// useSignalR.ts
import { useEffect } from 'react';
import { signalRService } from './signalRService';

export const useSignalR = () => {
    useEffect(() => {
        signalRService.start();

        return () => {
            signalRService.stop();
        };
    }, []);
};

// App.tsx
function App() {
    useSignalR(); // Initialize SignalR connection

    return (
        <div>
            {/* Your app content */}
        </div>
    );
}
```

### Mobile (React Native / Flutter)

For mobile apps, continue using Firebase SDK for push notifications when app is in background, but connect to SignalR when app is active.

## Benefits of Hybrid Approach

### 1. Real-Time Delivery
- SignalR provides instant notifications without polling
- No delay for users actively using the app
- Better user experience with immediate feedback

### 2. Reduced Database Load
- No need to store and query in-app notifications
- Notifications delivered directly to connected clients
- Database only stores historical notifications if needed

### 3. Cost Efficiency
- Fewer Firebase messages sent (only for offline users)
- Reduced database operations
- Lower infrastructure costs

### 4. Reliability
- Fallback to Firebase when SignalR unavailable
- No missed notifications
- Platform-native push notifications for background/closed app

### 5. Flexibility
- Different notification types for different scenarios
- Rich notification content via SignalR
- Standard push notifications via Firebase

## Scaling Considerations

### Redis Backplane for Multiple Servers

```csharp
services.AddSignalR()
    .AddStackExchangeRedis(Configuration.GetConnectionString("Redis"), options =>
    {
        options.Configuration.ChannelPrefix = "AICalendar";
    });
```

### Load Balancing

- Use sticky sessions or Redis backplane
- Ensure connection manager uses distributed cache
- Consider Azure SignalR Service for massive scale

### Connection Limits

- Monitor active connections
- Implement connection throttling if needed
- Use groups efficiently

## Testing Strategy

### 1. Test Online Scenario
- Connect user via SignalR
- Send notification
- Verify SignalR delivery
- Verify no Firebase message sent

### 2. Test Offline Scenario
- Disconnect SignalR
- Send notification
- Verify Firebase delivery
- Verify fallback logic

### 3. Test Reconnection
- Disconnect user
- Send notification (Firebase)
- Reconnect user
- Send notification (SignalR)
- Verify switch between methods

### 4. Test Multiple Devices
- Same user on multiple devices
- Some online, some offline
- Verify correct delivery method per device

## Monitoring and Logging

```csharp
// Log notification delivery method
_logger.LogInformation(
    "Notification {NotificationId} sent via {Method} to user {UserId}",
    notification.Id,
    isConnected ? "SignalR" : "Firebase",
    userId
);

// Track metrics
_metrics.RecordNotification(
    method: isConnected ? "SignalR" : "Firebase",
    type: notification.Type,
    userId: userId
);
```

## Security Considerations

### 1. Authentication
- Require JWT authentication for SignalR hub
- Validate user identity on connection
- Implement authorization for notification groups

### 2. Message Validation
- Validate notification content
- Sanitize user-generated content
- Implement rate limiting

### 3. Connection Security
- Use HTTPS/WSS only
- Implement CORS policies
- Token refresh for long-lived connections

## Migration Path

If you're adding this to existing Firebase-only system:

1. **Phase 1**: Implement SignalR infrastructure
   - Add SignalR hub and connection manager
   - No changes to existing Firebase logic

2. **Phase 2**: Create hybrid service
   - Implement hybrid notification service
   - Keep parallel Firebase service for rollback

3. **Phase 3**: Gradual rollout
   - Start with percentage of users
   - Monitor delivery success rates
   - Expand gradually

4. **Phase 4**: Full migration
   - Route all notifications through hybrid service
   - Keep Firebase as fallback only
   - Remove old in-app notification storage

## Troubleshooting

### Common Issues

1. **SignalR not connecting**
   - Check authentication token
   - Verify CORS configuration
   - Check firewall/proxy settings

2. **Notifications not received**
   - Verify connection manager tracking
   - Check logs for delivery method
   - Validate user ID mapping

3. **Duplicate notifications**
   - Check connection manager logic
   - Verify user connection state
   - Review timing of online/offline detection

## Conclusion

This hybrid approach provides the best of both worlds:
- Real-time delivery for active users via SignalR
- Reliable push notifications for offline users via Firebase
- No duplicate notifications
- Reduced infrastructure costs
- Better user experience

The system automatically chooses the optimal delivery method based on user connection status, ensuring notifications are always delivered efficiently.
