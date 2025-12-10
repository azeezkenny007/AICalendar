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
   - Reliable delivery through platform-specific services (APNs/FCM)

3. **Hybrid Strategy**
   - Check if user is connected via SignalR
   - If connected → Send via SignalR
   - If not connected → Send via Firebase
   - No duplicate notifications

---

## Backend Implementation (.NET)

### 1. Install Required NuGet Packages

```bash
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
dotnet add package StackExchange.Redis
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

### 2. Create Notification Models

```csharp
// Models/NotificationDto.cs
namespace AICalendar.Application.Notifications
{
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
}
```

### 3. Create SignalR Notification Hub

```csharp
// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AICalendar.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(
            IConnectionManager connectionManager,
            ILogger<NotificationHub> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
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

                _logger.LogInformation(
                    "User {UserId} connected with connection ID {ConnectionId}",
                    userId,
                    Context.ConnectionId
                );
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);

                _logger.LogInformation(
                    "User {UserId} disconnected with connection ID {ConnectionId}",
                    userId,
                    Context.ConnectionId
                );
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Optional: Client can ping to keep connection alive
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong");
        }
    }
}
```

### 4. Create Connection Manager Service

```csharp
// Services/IConnectionManager.cs
namespace AICalendar.Application.Services
{
    public interface IConnectionManager
    {
        Task AddConnectionAsync(string userId, string connectionId);
        Task RemoveConnectionAsync(string userId, string connectionId);
        Task<bool> IsUserConnectedAsync(string userId);
        Task<List<string>> GetUserConnectionsAsync(string userId);
    }
}
```

```csharp
// Services/ConnectionManager.cs
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AICalendar.Infrastructure.Services
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<ConnectionManager> _logger;
        private const int ConnectionExpiryMinutes = 30;

        public ConnectionManager(
            IDistributedCache cache,
            ILogger<ConnectionManager> logger)
        {
            _cache = cache;
            _logger = logger;
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

            _logger.LogDebug(
                "Added connection {ConnectionId} for user {UserId}",
                connectionId,
                userId
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

            _logger.LogDebug(
                "Removed connection {ConnectionId} for user {UserId}",
                connectionId,
                userId
            );
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
}
```

### 5. Create Device Token Repository

```csharp
// Repositories/IDeviceTokenRepository.cs
namespace AICalendar.Application.Repositories
{
    public interface IDeviceTokenRepository
    {
        Task<List<string>> GetUserDeviceTokensAsync(string userId);
        Task RegisterDeviceTokenAsync(string userId, string token, DevicePlatform platform);
        Task RemoveDeviceTokenAsync(string userId, string token);
    }

    public enum DevicePlatform
    {
        Android,
        iOS,
        Web
    }
}
```

```csharp
// Repositories/DeviceTokenRepository.cs
using AICalendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Repositories
{
    public class DeviceTokenRepository : IDeviceTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public DeviceTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetUserDeviceTokensAsync(string userId)
        {
            return await _context.DeviceTokens
                .Where(dt => dt.UserId == userId && dt.IsActive)
                .Select(dt => dt.Token)
                .ToListAsync();
        }

        public async Task RegisterDeviceTokenAsync(
            string userId,
            string token,
            DevicePlatform platform)
        {
            var existingToken = await _context.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.Token == token);

            if (existingToken != null)
            {
                existingToken.IsActive = true;
                existingToken.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var deviceToken = new DeviceToken
                {
                    UserId = userId,
                    Token = token,
                    Platform = platform.ToString(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                _context.DeviceTokens.Add(deviceToken);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveDeviceTokenAsync(string userId, string token)
        {
            var deviceToken = await _context.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.UserId == userId && dt.Token == token);

            if (deviceToken != null)
            {
                deviceToken.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}
```

### 6. Create Hybrid Notification Service

```csharp
// Services/IHybridNotificationService.cs
namespace AICalendar.Application.Services
{
    public interface IHybridNotificationService
    {
        Task SendNotificationAsync(string userId, NotificationDto notification);
        Task SendNotificationToMultipleUsersAsync(List<string> userIds, NotificationDto notification);
    }
}
```

```csharp
// Services/HybridNotificationService.cs
using Microsoft.AspNetCore.SignalR;
using AICalendar.API.Hubs;

namespace AICalendar.Infrastructure.Services
{
    public class HybridNotificationService : IHybridNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConnectionManager _connectionManager;
        private readonly IFirebaseService _firebaseService;
        private readonly IDeviceTokenRepository _deviceTokenRepository;
        private readonly ILogger<HybridNotificationService> _logger;

        public HybridNotificationService(
            IHubContext<NotificationHub> hubContext,
            IConnectionManager connectionManager,
            IFirebaseService firebaseService,
            IDeviceTokenRepository deviceTokenRepository,
            ILogger<HybridNotificationService> logger)
        {
            _hubContext = hubContext;
            _connectionManager = connectionManager;
            _firebaseService = firebaseService;
            _deviceTokenRepository = deviceTokenRepository;
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
                    // User is online - send via SignalR only
                    await SendViaSignalRAsync(userId, notification);
                    _logger.LogInformation(
                        "Notification {NotificationId} sent via SignalR to user {UserId}",
                        notification.Id,
                        userId
                    );
                }
                else
                {
                    // User is offline - send via Firebase
                    await SendViaFirebaseAsync(userId, notification);
                    _logger.LogInformation(
                        "Notification {NotificationId} sent via Firebase to user {UserId}",
                        notification.Id,
                        userId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending notification {NotificationId} to user {UserId}",
                    notification.Id,
                    userId
                );
                throw;
            }
        }

        public async Task SendNotificationToMultipleUsersAsync(
            List<string> userIds,
            NotificationDto notification)
        {
            var tasks = userIds.Select(userId =>
                SendNotificationAsync(userId, notification)
            );
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
            var deviceTokens = await _deviceTokenRepository.GetUserDeviceTokensAsync(userId);

            if (!deviceTokens.Any())
            {
                _logger.LogWarning("No device tokens found for user {UserId}", userId);
                return;
            }

            await _firebaseService.SendPushNotificationAsync(deviceTokens, notification);
        }
    }
}
```

### 7. Configure Services in Program.cs

```csharp
// Program.cs
using AICalendar.API.Hubs;
using AICalendar.Application.Services;
using AICalendar.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Configure CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // Web app
                "http://localhost:5173",  // Vite dev server
                "https://yourapp.com"     // Production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
})
.AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Add Redis for distributed cache (for connection tracking)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "AICalendar_";
});

// Optional: Add Redis backplane for SignalR (for multiple servers)
// builder.Services.AddSignalR()
//     .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"), options =>
//     {
//         options.Configuration.ChannelPrefix = "AICalendar";
//     });

// Register services
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddScoped<IHybridNotificationService, HybridNotificationService>();
builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
builder.Services.AddScoped<IFirebaseService, FirebaseService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };

        // Configure SignalR authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("SignalRPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
```

### 8. Create Device Token Controller

```csharp
// Controllers/DeviceTokenController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AICalendar.Application.Repositories;
using System.Security.Claims;

namespace AICalendar.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceTokenController : ControllerBase
    {
        private readonly IDeviceTokenRepository _deviceTokenRepository;

        public DeviceTokenController(IDeviceTokenRepository deviceTokenRepository)
        {
            _deviceTokenRepository = deviceTokenRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _deviceTokenRepository.RegisterDeviceTokenAsync(
                userId,
                request.Token,
                request.Platform
            );

            return Ok(new { message = "Device token registered successfully" });
        }

        [HttpDelete("unregister")]
        public async Task<IActionResult> UnregisterToken([FromBody] UnregisterTokenRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _deviceTokenRepository.RemoveDeviceTokenAsync(userId, request.Token);

            return Ok(new { message = "Device token removed successfully" });
        }
    }

    public class RegisterTokenRequest
    {
        public string Token { get; set; }
        public DevicePlatform Platform { get; set; }
    }

    public class UnregisterTokenRequest
    {
        public string Token { get; set; }
    }
}
```

### 9. Usage Example

```csharp
// Example: Using the service in a transaction controller
public class TransactionController : ControllerBase
{
    private readonly IHybridNotificationService _notificationService;

    public TransactionController(IHybridNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto dto)
    {
        // Create transaction logic...
        var transaction = await _transactionService.CreateAsync(dto);

        // Send hybrid notification
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
                { "amount", transaction.Amount.ToString() },
                { "type", transaction.Type }
            },
            ActionUrl = $"/transactions/{transaction.Id}"
        };

        await _notificationService.SendNotificationAsync(
            transaction.UserId.ToString(),
            notification
        );

        return Ok(transaction);
    }
}
```

---

## Android Client Implementation (Kotlin)

### 1. Add Dependencies (build.gradle.kts)

```kotlin
dependencies {
    // SignalR
    implementation("com.microsoft.signalr:signalr:7.0.0")

    // Firebase
    implementation(platform("com.google.firebase:firebase-bom:32.7.0"))
    implementation("com.google.firebase:firebase-messaging-ktx")

    // Coroutines
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-play-services:1.7.3")

    // Retrofit (for API calls)
    implementation("com.squareup.retrofit2:retrofit:2.9.0")
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")

    // OkHttp
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")
}
```

### 2. Create Notification Models

```kotlin
// models/NotificationDto.kt
package com.aicalendar.models

import com.google.gson.annotations.SerializedName

data class NotificationDto(
    @SerializedName("id")
    val id: String,

    @SerializedName("title")
    val title: String,

    @SerializedName("message")
    val message: String,

    @SerializedName("type")
    val type: NotificationType,

    @SerializedName("timestamp")
    val timestamp: String,

    @SerializedName("data")
    val data: Map<String, String>? = null,

    @SerializedName("actionUrl")
    val actionUrl: String? = null,

    @SerializedName("imageUrl")
    val imageUrl: String? = null
)

enum class NotificationType {
    @SerializedName("Info")
    INFO,

    @SerializedName("Success")
    SUCCESS,

    @SerializedName("Warning")
    WARNING,

    @SerializedName("Error")
    ERROR,

    @SerializedName("Transaction")
    TRANSACTION,

    @SerializedName("Reminder")
    REMINDER,

    @SerializedName("SystemAlert")
    SYSTEM_ALERT
}
```

### 3. Create SignalR Service

```kotlin
// services/SignalRService.kt
package com.aicalendar.services

import android.util.Log
import com.aicalendar.models.NotificationDto
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import com.microsoft.signalr.TransportEnum
import io.reactivex.rxjava3.subjects.PublishSubject
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import java.util.concurrent.TimeUnit

class SignalRService(
    private val baseUrl: String,
    private val tokenProvider: () -> String?
) {
    private var hubConnection: HubConnection? = null
    private val notificationSubject = PublishSubject.create<NotificationDto>()

    val notificationObservable = notificationSubject.hide()

    companion object {
        private const val TAG = "SignalRService"
        private const val HUB_PATH = "/notificationHub"
        private const val RECONNECT_DELAY_MS = 5000L
    }

    fun initialize() {
        hubConnection = HubConnectionBuilder.create("$baseUrl$HUB_PATH")
            .withAccessTokenProvider {
                val token = tokenProvider()
                if (token != null) {
                    io.reactivex.rxjava3.core.Single.just(token)
                } else {
                    io.reactivex.rxjava3.core.Single.error(Exception("No token available"))
                }
            }
            .withTransport(TransportEnum.WEBSOCKETS)
            .withHandshakeResponseTimeout(30, TimeUnit.SECONDS)
            .shouldSkipNegotiate(false)
            .build()

        setupHandlers()
    }

    private fun setupHandlers() {
        hubConnection?.on(
            "ReceiveNotification",
            { notification: NotificationDto ->
                Log.d(TAG, "Notification received: ${notification.title}")
                handleNotification(notification)
            },
            NotificationDto::class.java
        )

        hubConnection?.onClosed { error ->
            Log.e(TAG, "SignalR connection closed", error)
            // Auto reconnect
            CoroutineScope(Dispatchers.IO).launch {
                delay(RECONNECT_DELAY_MS)
                connect()
            }
        }
    }

    suspend fun connect() {
        try {
            if (hubConnection?.connectionState == HubConnectionState.DISCONNECTED) {
                hubConnection?.start()?.blockingAwait()
                Log.d(TAG, "SignalR connected successfully")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error connecting to SignalR", e)
            // Retry connection
            CoroutineScope(Dispatchers.IO).launch {
                delay(RECONNECT_DELAY_MS)
                connect()
            }
        }
    }

    suspend fun disconnect() {
        try {
            hubConnection?.stop()?.blockingAwait()
            Log.d(TAG, "SignalR disconnected")
        } catch (e: Exception) {
            Log.e(TAG, "Error disconnecting from SignalR", e)
        }
    }

    suspend fun ping() {
        try {
            hubConnection?.invoke("Ping")?.blockingAwait()
        } catch (e: Exception) {
            Log.e(TAG, "Error sending ping", e)
        }
    }

    fun isConnected(): Boolean {
        return hubConnection?.connectionState == HubConnectionState.CONNECTED
    }

    private fun handleNotification(notification: NotificationDto) {
        // Emit to observers
        notificationSubject.onNext(notification)

        // Show in-app notification
        showInAppNotification(notification)
    }

    private fun showInAppNotification(notification: NotificationDto) {
        // Implement your in-app notification UI logic here
        // For example: show toast, snackbar, or custom notification banner
        Log.d(TAG, "Show in-app notification: ${notification.title} - ${notification.message}")
    }
}
```

### 4. Create API Service for Device Token

```kotlin
// api/ApiService.kt
package com.aicalendar.api

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.POST

interface ApiService {
    @POST("api/devicetoken/register")
    suspend fun registerDeviceToken(
        @Body request: RegisterTokenRequest
    ): Response<RegisterTokenResponse>

    @DELETE("api/devicetoken/unregister")
    suspend fun unregisterDeviceToken(
        @Body request: UnregisterTokenRequest
    ): Response<UnregisterTokenResponse>
}

data class RegisterTokenRequest(
    val token: String,
    val platform: String = "Android"
)

data class RegisterTokenResponse(
    val message: String
)

data class UnregisterTokenRequest(
    val token: String
)

data class UnregisterTokenResponse(
    val message: String
)
```

### 5. Create Firebase Messaging Service

```kotlin
// services/MyFirebaseMessagingService.kt
package com.aicalendar.services

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import com.aicalendar.MainActivity
import com.aicalendar.R
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class MyFirebaseMessagingService : FirebaseMessagingService() {

    companion object {
        private const val TAG = "FCMService"
        private const val CHANNEL_ID = "aicalendar_notifications"
        private const val CHANNEL_NAME = "AI Calendar Notifications"
    }

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        Log.d(TAG, "New FCM token: $token")

        // Send token to backend
        CoroutineScope(Dispatchers.IO).launch {
            registerTokenWithBackend(token)
        }
    }

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)

        Log.d(TAG, "Message received from: ${message.from}")

        // Only show notification if app is in background
        // SignalR handles foreground notifications
        if (!isAppInForeground()) {
            message.notification?.let {
                showNotification(
                    title = it.title ?: "Notification",
                    body = it.body ?: "",
                    data = message.data
                )
            }
        }
    }

    private fun showNotification(
        title: String,
        body: String,
        data: Map<String, String>
    ) {
        createNotificationChannel()

        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            data.forEach { (key, value) ->
                putExtra(key, value)
            }
        }

        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            intent,
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        val notification = NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle(title)
            .setContentText(body)
            .setSmallIcon(R.drawable.ic_notification)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .build()

        val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        notificationManager.notify(System.currentTimeMillis().toInt(), notification)
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                CHANNEL_NAME,
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                description = "Notifications from AI Calendar"
            }

            val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            notificationManager.createNotificationChannel(channel)
        }
    }

    private suspend fun registerTokenWithBackend(token: String) {
        try {
            // Use your API service to register the token
            // val apiService = RetrofitClient.getApiService()
            // val response = apiService.registerDeviceToken(RegisterTokenRequest(token))
            Log.d(TAG, "Token registered with backend")
        } catch (e: Exception) {
            Log.e(TAG, "Error registering token", e)
        }
    }

    private fun isAppInForeground(): Boolean {
        // Implement logic to check if app is in foreground
        // You can use lifecycle callbacks or process importance
        return false
    }
}
```

### 6. Create Notification Manager

```kotlin
// managers/NotificationManager.kt
package com.aicalendar.managers

import android.content.Context
import android.util.Log
import com.aicalendar.api.ApiService
import com.aicalendar.api.RegisterTokenRequest
import com.aicalendar.services.SignalRService
import com.google.android.gms.tasks.Tasks
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class NotificationManager(
    private val context: Context,
    private val signalRService: SignalRService,
    private val apiService: ApiService
) {
    companion object {
        private const val TAG = "NotificationManager"
    }

    fun initialize() {
        // Initialize SignalR
        signalRService.initialize()

        // Subscribe to SignalR notifications
        signalRService.notificationObservable.subscribe { notification ->
            Log.d(TAG, "Received notification: ${notification.title}")
            // Handle notification in UI
        }

        // Get and register FCM token
        registerFCMToken()
    }

    suspend fun connect() {
        signalRService.connect()
    }

    suspend fun disconnect() {
        signalRService.disconnect()
    }

    private fun registerFCMToken() {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val token = Tasks.await(FirebaseMessaging.getInstance().token)
                Log.d(TAG, "FCM Token: $token")

                // Register with backend
                val response = apiService.registerDeviceToken(
                    RegisterTokenRequest(token = token, platform = "Android")
                )

                if (response.isSuccessful) {
                    Log.d(TAG, "FCM token registered successfully")
                } else {
                    Log.e(TAG, "Failed to register FCM token: ${response.errorBody()}")
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error getting FCM token", e)
            }
        }
    }
}
```

### 7. Application Setup

```kotlin
// Application.kt
package com.aicalendar

import android.app.Application
import com.aicalendar.managers.NotificationManager
import com.aicalendar.services.SignalRService

class AICalendarApplication : Application() {

    lateinit var notificationManager: NotificationManager

    override fun onCreate() {
        super.onCreate()

        // Initialize SignalR service
        val signalRService = SignalRService(
            baseUrl = "https://yourapi.com",
            tokenProvider = { getAuthToken() }
        )

        // Initialize notification manager
        notificationManager = NotificationManager(
            context = this,
            signalRService = signalRService,
            apiService = getApiService()
        )

        notificationManager.initialize()
    }

    private fun getAuthToken(): String? {
        // Return JWT token from your auth system
        return null // Implement your token retrieval logic
    }

    private fun getApiService(): com.aicalendar.api.ApiService {
        // Return your Retrofit API service
        throw NotImplementedException()
    }
}
```

### 8. MainActivity Integration

```kotlin
// MainActivity.kt
package com.aicalendar

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {

    private lateinit var notificationManager: NotificationManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        notificationManager = (application as AICalendarApplication).notificationManager
    }

    override fun onResume() {
        super.onResume()
        // Connect to SignalR when app comes to foreground
        lifecycleScope.launch {
            notificationManager.connect()
        }
    }

    override fun onPause() {
        super.onPause()
        // Disconnect from SignalR when app goes to background
        lifecycleScope.launch {
            notificationManager.disconnect()
        }
    }
}
```

### 9. AndroidManifest.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />

    <application
        android:name=".AICalendarApplication"
        android:allowBackup="true"
        android:icon="@mipmap/ic_launcher"
        android:label="@string/app_name"
        android:theme="@style/Theme.AICalendar">

        <activity
            android:name=".MainActivity"
            android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>

        <service
            android:name=".services.MyFirebaseMessagingService"
            android:exported="false">
            <intent-filter>
                <action android:name="com.google.firebase.MESSAGING_EVENT" />
            </intent-filter>
        </service>
    </application>
</manifest>
```

---

## iOS Client Implementation (Swift)

### 1. Install Dependencies via CocoaPods

```ruby
# Podfile
platform :ios, '13.0'

target 'AICalendar' do
  use_frameworks!

  # SignalR
  pod 'SwiftSignalRClient', '~> 0.9.0'

  # Firebase
  pod 'Firebase/Messaging'

  # Alamofire for networking
  pod 'Alamofire', '~> 5.8'
end
```

Then run:
```bash
pod install
```

### 2. Create Notification Models

```swift
// Models/NotificationDto.swift
import Foundation

struct NotificationDto: Codable {
    let id: String
    let title: String
    let message: String
    let type: NotificationType
    let timestamp: String
    let data: [String: String]?
    let actionUrl: String?
    let imageUrl: String?
}

enum NotificationType: String, Codable {
    case info = "Info"
    case success = "Success"
    case warning = "Warning"
    case error = "Error"
    case transaction = "Transaction"
    case reminder = "Reminder"
    case systemAlert = "SystemAlert"
}
```

### 3. Create SignalR Service

```swift
// Services/SignalRService.swift
import Foundation
import SignalRClient

class SignalRService {

    private var hubConnection: HubConnection?
    private let baseURL: String
    private let tokenProvider: () -> String?

    var onNotificationReceived: ((NotificationDto) -> Void)?

    private let reconnectDelay: TimeInterval = 5.0

    init(baseURL: String, tokenProvider: @escaping () -> String?) {
        self.baseURL = baseURL
        self.tokenProvider = tokenProvider
    }

    func initialize() {
        let url = URL(string: "\(baseURL)/notificationHub")!

        hubConnection = HubConnectionBuilder(url: url)
            .withLogging(minLogLevel: .info)
            .withAutoReconnect()
            .withHubConnectionDelegate(delegate: self)
            .withHttpConnectionOptions { options in
                options.accessTokenProvider = {
                    return self.tokenProvider()
                }
                options.headers = [:]
            }
            .build()

        setupHandlers()
    }

    private func setupHandlers() {
        hubConnection?.on(method: "ReceiveNotification", callback: { (notification: NotificationDto) in
            print("Notification received: \(notification.title)")
            self.handleNotification(notification)
        })
    }

    func connect() {
        guard hubConnection?.state == .disconnected else {
            print("SignalR already connected or connecting")
            return
        }

        hubConnection?.start { error in
            if let error = error {
                print("Error connecting to SignalR: \(error.localizedDescription)")
                // Retry connection
                DispatchQueue.main.asyncAfter(deadline: .now() + self.reconnectDelay) {
                    self.connect()
                }
            } else {
                print("SignalR connected successfully")
            }
        }
    }

    func disconnect() {
        hubConnection?.stop { error in
            if let error = error {
                print("Error disconnecting from SignalR: \(error.localizedDescription)")
            } else {
                print("SignalR disconnected")
            }
        }
    }

    func ping() {
        hubConnection?.invoke(method: "Ping", arguments: []) { error in
            if let error = error {
                print("Error sending ping: \(error.localizedDescription)")
            }
        }
    }

    func isConnected() -> Bool {
        return hubConnection?.state == .connected
    }

    private func handleNotification(_ notification: NotificationDto) {
        // Notify observers
        onNotificationReceived?(notification)

        // Show in-app notification
        showInAppNotification(notification)
    }

    private func showInAppNotification(_ notification: NotificationDto) {
        // Implement your in-app notification UI logic
        print("Show in-app notification: \(notification.title) - \(notification.message)")

        // Example: Post notification to NotificationCenter
        NotificationCenter.default.post(
            name: NSNotification.Name("NewNotificationReceived"),
            object: nil,
            userInfo: ["notification": notification]
        )
    }
}

extension SignalRService: HubConnectionDelegate {
    func connectionDidOpen(hubConnection: HubConnection) {
        print("SignalR connection opened")
    }

    func connectionDidFailToOpen(error: Error) {
        print("SignalR connection failed to open: \(error.localizedDescription)")
    }

    func connectionDidClose(error: Error?) {
        if let error = error {
            print("SignalR connection closed with error: \(error.localizedDescription)")
        } else {
            print("SignalR connection closed")
        }

        // Auto reconnect
        DispatchQueue.main.asyncAfter(deadline: .now() + reconnectDelay) {
            self.connect()
        }
    }

    func connectionWillReconnect(error: Error) {
        print("SignalR will reconnect: \(error.localizedDescription)")
    }

    func connectionDidReconnect() {
        print("SignalR reconnected")
    }
}
```

### 4. Create API Service for Device Token

```swift
// Services/APIService.swift
import Foundation
import Alamofire

class APIService {

    private let baseURL: String
    private let tokenProvider: () -> String?

    init(baseURL: String, tokenProvider: @escaping () -> String?) {
        self.baseURL = baseURL
        self.tokenProvider = tokenProvider
    }

    func registerDeviceToken(token: String, completion: @escaping (Result<RegisterTokenResponse, Error>) -> Void) {
        let url = "\(baseURL)/api/devicetoken/register"

        let parameters: [String: Any] = [
            "token": token,
            "platform": "iOS"
        ]

        let headers: HTTPHeaders = [
            "Authorization": "Bearer \(tokenProvider() ?? "")",
            "Content-Type": "application/json"
        ]

        AF.request(url, method: .post, parameters: parameters, encoding: JSONEncoding.default, headers: headers)
            .validate()
            .responseDecodable(of: RegisterTokenResponse.self) { response in
                switch response.result {
                case .success(let result):
                    completion(.success(result))
                case .failure(let error):
                    completion(.failure(error))
                }
            }
    }

    func unregisterDeviceToken(token: String, completion: @escaping (Result<UnregisterTokenResponse, Error>) -> Void) {
        let url = "\(baseURL)/api/devicetoken/unregister"

        let parameters: [String: Any] = [
            "token": token
        ]

        let headers: HTTPHeaders = [
            "Authorization": "Bearer \(tokenProvider() ?? "")",
            "Content-Type": "application/json"
        ]

        AF.request(url, method: .delete, parameters: parameters, encoding: JSONEncoding.default, headers: headers)
            .validate()
            .responseDecodable(of: UnregisterTokenResponse.self) { response in
                switch response.result {
                case .success(let result):
                    completion(.success(result))
                case .failure(let error):
                    completion(.failure(error))
                }
            }
    }
}

struct RegisterTokenResponse: Codable {
    let message: String
}

struct UnregisterTokenResponse: Codable {
    let message: String
}
```

### 5. Create Firebase Messaging Handler

```swift
// Services/FirebaseMessagingService.swift
import Foundation
import FirebaseMessaging
import UserNotifications

class FirebaseMessagingService: NSObject {

    private let apiService: APIService

    init(apiService: APIService) {
        self.apiService = apiService
        super.init()

        Messaging.messaging().delegate = self
        UNUserNotificationCenter.current().delegate = self
    }

    func requestNotificationPermission() {
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .badge, .sound]) { granted, error in
            if granted {
                print("Notification permission granted")
                DispatchQueue.main.async {
                    UIApplication.shared.registerForRemoteNotifications()
                }
            } else if let error = error {
                print("Error requesting notification permission: \(error.localizedDescription)")
            }
        }
    }

    func handleToken(_ token: String) {
        print("FCM Token: \(token)")

        // Register with backend
        apiService.registerDeviceToken(token: token) { result in
            switch result {
            case .success:
                print("FCM token registered successfully")
            case .failure(let error):
                print("Error registering FCM token: \(error.localizedDescription)")
            }
        }
    }
}

extension FirebaseMessagingService: MessagingDelegate {
    func messaging(_ messaging: Messaging, didReceiveRegistrationToken fcmToken: String?) {
        guard let fcmToken = fcmToken else { return }
        handleToken(fcmToken)
    }
}

extension FirebaseMessagingService: UNUserNotificationCenterDelegate {
    // Handle notification when app is in foreground
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification,
        withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void
    ) {
        let userInfo = notification.request.content.userInfo
        print("Notification received in foreground: \(userInfo)")

        // Don't show notification if SignalR is connected (already handled)
        // Only show if disconnected
        completionHandler([.banner, .sound, .badge])
    }

    // Handle notification tap
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        didReceive response: UNNotificationResponse,
        withCompletionHandler completionHandler: @escaping () -> Void
    ) {
        let userInfo = response.notification.request.content.userInfo
        print("Notification tapped: \(userInfo)")

        // Handle notification action
        // Navigate to specific screen based on userInfo

        completionHandler()
    }
}
```

### 6. Create Notification Manager

```swift
// Managers/NotificationManager.swift
import Foundation

class NotificationManager {

    private let signalRService: SignalRService
    private let firebaseService: FirebaseMessagingService

    init(signalRService: SignalRService, firebaseService: FirebaseMessagingService) {
        self.signalRService = signalRService
        self.firebaseService = firebaseService
    }

    func initialize() {
        // Initialize SignalR
        signalRService.initialize()

        // Subscribe to SignalR notifications
        signalRService.onNotificationReceived = { notification in
            print("Received notification: \(notification.title)")
            // Handle notification in UI
            self.handleNotification(notification)
        }

        // Request notification permissions
        firebaseService.requestNotificationPermission()
    }

    func connect() {
        signalRService.connect()
    }

    func disconnect() {
        signalRService.disconnect()
    }

    private func handleNotification(_ notification: NotificationDto) {
        // Handle notification display in UI
        // Update badge count, show banner, etc.
    }
}
```

### 7. AppDelegate Setup

```swift
// AppDelegate.swift
import UIKit
import Firebase

@main
class AppDelegate: UIResponder, UIApplicationDelegate {

    var window: UIWindow?
    var notificationManager: NotificationManager?

    func application(
        _ application: UIApplication,
        didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
    ) -> Bool {

        // Configure Firebase
        FirebaseApp.configure()

        // Initialize services
        let apiService = APIService(
            baseURL: "https://yourapi.com",
            tokenProvider: { self.getAuthToken() }
        )

        let signalRService = SignalRService(
            baseURL: "https://yourapi.com",
            tokenProvider: { self.getAuthToken() }
        )

        let firebaseService = FirebaseMessagingService(apiService: apiService)

        // Initialize notification manager
        notificationManager = NotificationManager(
            signalRService: signalRService,
            firebaseService: firebaseService
        )

        notificationManager?.initialize()

        return true
    }

    func applicationWillEnterForeground(_ application: UIApplication) {
        // Connect to SignalR when app comes to foreground
        notificationManager?.connect()
    }

    func applicationDidEnterBackground(_ application: UIApplication) {
        // Disconnect from SignalR when app goes to background
        notificationManager?.disconnect()
    }

    func application(
        _ application: UIApplication,
        didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data
    ) {
        Messaging.messaging().apnsToken = deviceToken
    }

    func application(
        _ application: UIApplication,
        didFailToRegisterForRemoteNotificationsWithError error: Error
    ) {
        print("Failed to register for remote notifications: \(error.localizedDescription)")
    }

    private func getAuthToken() -> String? {
        // Return JWT token from your auth system
        return nil // Implement your token retrieval logic
    }
}
```

### 8. Info.plist Configuration

Add the following keys to your Info.plist:

```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
</array>

<key>FirebaseAppDelegateProxyEnabled</key>
<false/>
```

---

## Web Client Implementation (JavaScript/TypeScript)

### 1. Install Dependencies

```bash
npm install @microsoft/signalr
# or
yarn add @microsoft/signalr
```

### 2. Create Notification Types

```typescript
// types/notification.ts
export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  type: NotificationType;
  timestamp: string;
  data?: Record<string, string>;
  actionUrl?: string;
  imageUrl?: string;
}

export enum NotificationType {
  Info = 'Info',
  Success = 'Success',
  Warning = 'Warning',
  Error = 'Error',
  Transaction = 'Transaction',
  Reminder = 'Reminder',
  SystemAlert = 'SystemAlert'
}
```

### 3. Create SignalR Service

```typescript
// services/signalRService.ts
import * as signalR from '@microsoft/signalr';
import { NotificationDto } from '../types/notification';

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private baseUrl: string;
  private tokenProvider: () => string | null;
  private reconnectDelay = 5000;
  private notificationCallbacks: Set<(notification: NotificationDto) => void> = new Set();

  constructor(baseUrl: string, tokenProvider: () => string | null) {
    this.baseUrl = baseUrl;
    this.tokenProvider = tokenProvider;
  }

  public initialize(): void {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/notificationHub`, {
        accessTokenFactory: () => {
          const token = this.tokenProvider();
          return token || '';
        },
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.elapsedMilliseconds < 60000) {
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          } else {
            return null; // Stop retrying after 1 minute
          }
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupHandlers();
  }

  private setupHandlers(): void {
    if (!this.connection) return;

    this.connection.on('ReceiveNotification', (notification: NotificationDto) => {
      console.log('Notification received:', notification.title);
      this.handleNotification(notification);
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
    });

    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      // Attempt to reconnect
      setTimeout(() => this.connect(), this.reconnectDelay);
    });
  }

  public async connect(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Disconnected) {
      console.log('SignalR already connected or connecting');
      return;
    }

    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
    } catch (err) {
      console.error('Error connecting to SignalR:', err);
      setTimeout(() => this.connect(), this.reconnectDelay);
    }
  }

  public async disconnect(): Promise<void> {
    if (!this.connection) return;

    try {
      await this.connection.stop();
      console.log('SignalR disconnected');
    } catch (err) {
      console.error('Error disconnecting from SignalR:', err);
    }
  }

  public async ping(): Promise<void> {
    if (!this.connection) return;

    try {
      await this.connection.invoke('Ping');
    } catch (err) {
      console.error('Error sending ping:', err);
    }
  }

  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  public onNotification(callback: (notification: NotificationDto) => void): () => void {
    this.notificationCallbacks.add(callback);

    // Return unsubscribe function
    return () => {
      this.notificationCallbacks.delete(callback);
    };
  }

  private handleNotification(notification: NotificationDto): void {
    // Notify all subscribers
    this.notificationCallbacks.forEach(callback => {
      callback(notification);
    });

    // Show in-app notification
    this.showInAppNotification(notification);
  }

  private showInAppNotification(notification: NotificationDto): void {
    // Implement your UI notification logic
    console.log(`Show notification: ${notification.title} - ${notification.message}`);

    // Example: Dispatch custom event
    window.dispatchEvent(
      new CustomEvent('notification-received', {
        detail: notification
      })
    );
  }
}
```

### 4. Create API Service for Device Token (Firebase Web)

```typescript
// services/firebaseService.ts
import { initializeApp } from 'firebase/app';
import { getMessaging, getToken, onMessage } from 'firebase/messaging';

const firebaseConfig = {
  apiKey: 'YOUR_API_KEY',
  authDomain: 'YOUR_AUTH_DOMAIN',
  projectId: 'YOUR_PROJECT_ID',
  storageBucket: 'YOUR_STORAGE_BUCKET',
  messagingSenderId: 'YOUR_MESSAGING_SENDER_ID',
  appId: 'YOUR_APP_ID'
};

const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

export class FirebaseService {
  private apiBaseUrl: string;

  constructor(apiBaseUrl: string) {
    this.apiBaseUrl = apiBaseUrl;
  }

  public async requestPermissionAndGetToken(): Promise<string | null> {
    try {
      const permission = await Notification.requestPermission();

      if (permission === 'granted') {
        const token = await getToken(messaging, {
          vapidKey: 'YOUR_VAPID_KEY'
        });

        console.log('FCM Token:', token);

        // Register token with backend
        await this.registerToken(token);

        return token;
      } else {
        console.log('Notification permission denied');
        return null;
      }
    } catch (error) {
      console.error('Error getting FCM token:', error);
      return null;
    }
  }

  public setupForegroundMessaging(): void {
    onMessage(messaging, (payload) => {
      console.log('Message received in foreground:', payload);

      // Don't show if SignalR is connected (already handled)
      // This is backup for when SignalR fails

      if (payload.notification) {
        new Notification(payload.notification.title || 'Notification', {
          body: payload.notification.body,
          icon: payload.notification.icon,
          data: payload.data
        });
      }
    });
  }

  private async registerToken(token: string): Promise<void> {
    try {
      const authToken = localStorage.getItem('authToken');

      const response = await fetch(`${this.apiBaseUrl}/api/devicetoken/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}`
        },
        body: JSON.stringify({
          token,
          platform: 'Web'
        })
      });

      if (response.ok) {
        console.log('FCM token registered successfully');
      } else {
        console.error('Failed to register FCM token');
      }
    } catch (error) {
      console.error('Error registering token:', error);
    }
  }
}
```

### 5. Create Notification Manager

```typescript
// managers/notificationManager.ts
import { SignalRService } from '../services/signalRService';
import { FirebaseService } from '../services/firebaseService';
import { NotificationDto } from '../types/notification';

export class NotificationManager {
  private signalRService: SignalRService;
  private firebaseService: FirebaseService;

  constructor(apiBaseUrl: string, tokenProvider: () => string | null) {
    this.signalRService = new SignalRService(apiBaseUrl, tokenProvider);
    this.firebaseService = new FirebaseService(apiBaseUrl);
  }

  public async initialize(): Promise<void> {
    // Initialize SignalR
    this.signalRService.initialize();

    // Subscribe to notifications
    this.signalRService.onNotification((notification) => {
      this.handleNotification(notification);
    });

    // Initialize Firebase
    await this.firebaseService.requestPermissionAndGetToken();
    this.firebaseService.setupForegroundMessaging();
  }

  public async connect(): Promise<void> {
    await this.signalRService.connect();
  }

  public async disconnect(): Promise<void> {
    await this.signalRService.disconnect();
  }

  public onNotification(callback: (notification: NotificationDto) => void): () => void {
    return this.signalRService.onNotification(callback);
  }

  private handleNotification(notification: NotificationDto): void {
    console.log('Handling notification:', notification);
    // Additional UI handling logic
  }
}
```

### 6. React Integration

```typescript
// hooks/useNotifications.ts
import { useEffect, useState } from 'react';
import { NotificationManager } from '../managers/notificationManager';
import { NotificationDto } from '../types/notification';

let notificationManager: NotificationManager | null = null;

export const useNotifications = () => {
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);

  useEffect(() => {
    // Initialize notification manager
    if (!notificationManager) {
      notificationManager = new NotificationManager(
        'https://yourapi.com',
        () => localStorage.getItem('authToken')
      );

      notificationManager.initialize();
    }

    // Connect to SignalR
    notificationManager.connect();

    // Subscribe to notifications
    const unsubscribe = notificationManager.onNotification((notification) => {
      setNotifications(prev => [notification, ...prev]);
    });

    // Handle visibility change
    const handleVisibilityChange = () => {
      if (document.hidden) {
        notificationManager?.disconnect();
      } else {
        notificationManager?.connect();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      unsubscribe();
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, []);

  return { notifications };
};
```

```typescript
// App.tsx
import React from 'react';
import { useNotifications } from './hooks/useNotifications';

function App() {
  const { notifications } = useNotifications();

  return (
    <div className="App">
      <h1>AI Calendar</h1>

      {notifications.length > 0 && (
        <div className="notifications">
          <h2>Recent Notifications</h2>
          {notifications.map(notification => (
            <div key={notification.id} className="notification">
              <h3>{notification.title}</h3>
              <p>{notification.message}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default App;
```

### 7. Firebase Service Worker (public/firebase-messaging-sw.js)

```javascript
// public/firebase-messaging-sw.js
importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-messaging-compat.js');

firebase.initializeApp({
  apiKey: 'YOUR_API_KEY',
  authDomain: 'YOUR_AUTH_DOMAIN',
  projectId: 'YOUR_PROJECT_ID',
  storageBucket: 'YOUR_STORAGE_BUCKET',
  messagingSenderId: 'YOUR_MESSAGING_SENDER_ID',
  appId: 'YOUR_APP_ID'
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
  console.log('Received background message:', payload);

  const notificationTitle = payload.notification.title;
  const notificationOptions = {
    body: payload.notification.body,
    icon: payload.notification.icon || '/logo192.png',
    data: payload.data
  };

  self.registration.showNotification(notificationTitle, notificationOptions);
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const urlToOpen = event.notification.data?.actionUrl || '/';

  event.waitUntil(
    clients.openWindow(urlToOpen)
  );
});
```

---

## Summary

### How It Works

1. **Backend (.NET)**:
   - SignalR Hub tracks connected users via Connection Manager
   - Hybrid Notification Service checks if user is online
   - If online → sends via SignalR
   - If offline → sends via Firebase

2. **Android (Kotlin)**:
   - Connects to SignalR when app is in foreground
   - Receives real-time notifications via SignalR
   - Firebase handles notifications when app is in background
   - Registers FCM token with backend

3. **iOS (Swift)**:
   - Connects to SignalR when app is active
   - Receives real-time notifications via SignalR
   - APNs (via Firebase) handles background notifications
   - Registers FCM token with backend

4. **Web (TypeScript)**:
   - Connects to SignalR when browser tab is active
   - Receives real-time notifications via SignalR
   - Service Worker handles background notifications
   - Registers FCM token with backend

### Benefits

- No duplicate notifications
- Real-time delivery for active users
- Reliable push for offline users
- Reduced database load (no in-app notification storage needed)
- Better user experience