# Azure Notification Hub Implementation Guide

Complete step-by-step guide to implement push notifications using **Azure Notification Hub exclusively** (no Firebase dependency at all) in the AICalendar project.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Phase 1: Azure Setup](#phase-1-azure-setup)
4. [Phase 2: Backend Implementation](#phase-2-backend-implementation)
5. [Phase 3: Database Changes](#phase-3-database-changes)
6. [Phase 4: Frontend Integration](#phase-4-frontend-integration)
7. [Phase 5: Testing](#phase-5-testing)
8. [Phase 6: Production Deployment](#phase-6-production-deployment)
9. [Troubleshooting](#troubleshooting)

---

## Overview

### What We're Building

```
┌─────────────────────────────────────────────────────────────┐
│                  NOTIFICATION FLOW                           │
└─────────────────────────────────────────────────────────────┘

User logs in → Gets platform-specific device token → Sends to backend
    ↓
Backend registers device with Azure Notification Hub
    ↓
Azure creates Installation with tags (userId, platform, etc.)
    ↓
Hangfire SendRemindersJob runs every hour
    ↓
Checks unpaid calendar items
    ↓
Sends notification via Azure Notification Hub SDK
    ↓
Azure routes to appropriate platform (APNs for iOS, FCM for Android)
    ↓
User's device receives push notification
```

### Technologies Used

- **Backend**: .NET 8 + Azure Notification Hub SDK
- **Cloud Service**: Azure Notification Hub (handles all platform routing)
- **Frontend Android**: Kotlin + Google Play Services (FCM)
- **Frontend iOS**: Swift + UserNotifications Framework (APNs)
- **Database**: SQL Server (add device token + installation ID columns)
- **Job Scheduler**: Hangfire (already configured)

### Key Architecture Benefits

- **Single Backend Integration**: One SDK for all platforms
- **Platform Abstraction**: Azure handles APNs/FCM/WNS routing
- **Advanced Segmentation**: Tag-based targeting (by user, platform, groups)
- **Template Support**: Define notification templates once, localize per user
- **Enterprise Ready**: Scales to millions of devices

---

## Prerequisites

### Requirements

- [ ] Azure account ([Create free account](https://azure.microsoft.com/free/))
- [ ] .NET 8 SDK installed
- [ ] SQL Server running
- [ ] **For Android**: Google Cloud Console access (for FCM Server Key)
- [ ] **For iOS**: Apple Developer account (for APNs certificate or auth key)

### Estimated Time

- Azure setup: 45 minutes
- Backend implementation: 2 hours
- Database changes: 30 minutes
- Frontend integration: 2 hours (1 hour per platform)
- Testing: 1 hour
- **Total**: ~6 hours

### Cost Estimate

**Azure Notification Hub Pricing Tiers:**

| Tier | Price | Devices | Push/Month | Features |
|------|-------|---------|------------|----------|
| **Free** | $0 | 500 | 1 million | Basic push |
| **Basic** | $10/month | Unlimited | 10 million | Basic push + telemetry |
| **Standard** | $200/month | Unlimited | 10 million | Templates, scheduled push, analytics |

**Recommendation**: Start with **Free** for development, upgrade to **Basic** ($10/month) for production.

---

## Phase 1: Azure Setup

### Step 1.1: Create Notification Hub Namespace

1. Go to [Azure Portal](https://portal.azure.com/)
2. Click **"Create a resource"** → Search for **"Notification Hubs"**
3. Click **"Create"**

**Namespace Configuration**:
```
Subscription:     [Your subscription]
Resource Group:   AICalendar-RG (create new)
Namespace Name:   aicalendar-notifications (must be globally unique)
Location:         East US (or nearest region)
Pricing Tier:     Free (can upgrade later)
```

4. Click **"Review + create"** → **"Create"**
5. Wait for deployment (~2 minutes)

### Step 1.2: Create Notification Hub

1. Navigate to your namespace: `aicalendar-notifications`
2. In the left menu, click **"Notification Hubs"**
3. Click **"+ Notification Hub"**
4. **Hub Name**: `aicalendar-hub`
5. Click **"Create"**

### Step 1.3: Configure Android Platform (FCM)

Azure Notification Hub uses **Firebase Cloud Messaging (FCM)** for Android delivery, but you **don't need Firebase SDK** in your Android app. You only need the FCM Server Key.

#### Get FCM Server Key

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project: `AICalendar` (or use existing)
3. Go to **Project Settings** (gear icon) → **Cloud Messaging** tab
4. Under **"Cloud Messaging API (Legacy)"**:
   - If not enabled, click **"Enable"**
   - Copy the **"Server key"** (starts with `AAAA...`)
   - Copy the **"Sender ID"** (numeric ID)

⚠️ **Note**: You're only using Firebase Console to get the FCM credentials. Your Android app will **NOT** use Firebase SDK.

#### Configure in Azure

1. In Azure Portal, navigate to your Notification Hub: `aicalendar-hub`
2. In the left menu, click **"Google (GCM/FCM)"**
3. Paste the **"API Key"** (the Server Key from Firebase)
4. Click **"Save"**

### Step 1.4: Configure iOS Platform (APNs)

Azure Notification Hub connects directly to **Apple Push Notification service (APNs)**. You have two authentication options:

#### Option A: Token-Based Authentication (Recommended)

**Advantages**: No expiration, works for all apps in your team, easier to manage.

1. Go to [Apple Developer Portal](https://developer.apple.com/account/)
2. Navigate to **Certificates, Identifiers & Profiles** → **Keys**
3. Click **"+"** to create a new key
4. **Key Name**: `AICalendar APNs Key`
5. Check **"Apple Push Notifications service (APNs)"**
6. Click **"Continue"** → **"Register"**
7. **Download the .p8 file** (⚠️ Only shown once!)
8. Note down:
   - **Key ID** (10 characters, e.g., `AB12CD34EF`)
   - **Team ID** (found in top-right corner, e.g., `XYZ1234ABC`)

**Configure in Azure:**

1. In Azure Portal, navigate to your Notification Hub: `aicalendar-hub`
2. Click **"Apple (APNS)"** in the left menu
3. Select **"Token"** mode
4. **Upload the .p8 file**
5. Enter **Key ID** (from step 8)
6. Enter **Team ID** (from step 8)
7. Enter **Bundle ID**: `com.aicalendar.app` (your iOS app bundle ID)
8. Select **"Sandbox"** (for development) or **"Production"**
9. Click **"Save"**

#### Option B: Certificate-Based Authentication (Legacy)

1. Go to [Apple Developer Portal](https://developer.apple.com/account/)
2. Navigate to **Certificates, Identifiers & Profiles** → **Certificates**
3. Click **"+"** to create certificate
4. Select **"Apple Push Notification service SSL (Sandbox & Production)"**
5. Choose your **App ID**
6. Generate **Certificate Signing Request (CSR)**:
   - Open **Keychain Access** (Mac)
   - Menu: **Keychain Access** → **Certificate Assistant** → **Request a Certificate From a Certificate Authority**
   - Email: your email
   - Common Name: `AICalendar APNs`
   - Select **"Saved to disk"**
7. Upload CSR to Apple Developer Portal
8. Download the `.cer` file
9. Double-click to import to Keychain Access
10. Export as `.p12`:
    - Right-click certificate in Keychain
    - **Export** → Save as `.p12`
    - Set password (remember it!)

**Configure in Azure:**

1. In Azure Portal, navigate to your Notification Hub: `aicalendar-hub`
2. Click **"Apple (APNS)"** in the left menu
3. Select **"Certificate"** mode
4. Upload the `.p12` file
5. Enter the password
6. Select **"Sandbox"** or **"Production"**
7. Click **"Save"**

### Step 1.5: Get Connection Strings

1. In your Notification Hub (`aicalendar-hub`), click **"Access Policies"** in left menu
2. You'll see two policies:
   - **DefaultListenSharedAccessSignature** (read-only, for clients)
   - **DefaultFullSharedAccessSignature** (full access, for backend)

3. Click **"DefaultFullSharedAccessSignature"**
4. Copy **"Connection string-Primary"**

Example format:
```
Endpoint=sb://aicalendar-notifications.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=YOUR_KEY_HERE
```

⚠️ **SECURITY WARNING**: Keep this secret! Never commit to Git!

---

## Phase 2: Backend Implementation

### Step 2.1: Install NuGet Package

```bash
cd src/AICalendar.API
dotnet add package Microsoft.Azure.NotificationHubs --version 4.2.0
```

### Step 2.2: Add Configuration

**File**: `src/AICalendar.API/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AzureNotificationHub": {
    "ConnectionString": "",
    "HubName": ""
  }
}
```

**File**: `src/AICalendar.API/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "AzureNotificationHub": {
    "ConnectionString": "Endpoint=sb://aicalendar-notifications.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=YOUR_KEY_HERE",
    "HubName": "aicalendar-hub"
  }
}
```

⚠️ **Add to `.gitignore`**:
```gitignore
# Development settings with secrets
**/appsettings.Development.json
```

### Step 2.3: Create Configuration Model

**File**: `src/AICalendar.Infrastructure/Configuration/AzureNotificationHubSettings.cs`

```csharp
namespace AICalendar.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Azure Notification Hub
/// </summary>
public class AzureNotificationHubSettings
{
    public const string SectionName = "AzureNotificationHub";

    /// <summary>
    /// Azure Notification Hub connection string (DefaultFullSharedAccessSignature)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the notification hub (e.g., "aicalendar-hub")
    /// </summary>
    public string HubName { get; set; } = string.Empty;

    /// <summary>
    /// Whether Azure Notification Hub is properly configured
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) &&
        !string.IsNullOrWhiteSpace(HubName);
}
```

### Step 2.4: Create Notification Service Interface

**File**: `src/AICalendar.Application/Common/Interfaces/INotificationService.cs`

```csharp
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Application.Common.Interfaces;

/// <summary>
/// Service for managing push notifications via Azure Notification Hub
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Registers a device for push notifications
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="deviceToken">Platform-specific device token (FCM token for Android, APNs token for iOS)</param>
    /// <param name="platform">Platform: "android" or "ios"</param>
    /// <param name="tags">Optional additional tags for segmentation</param>
    /// <returns>Installation ID created in Azure Notification Hub</returns>
    Task<string> RegisterDeviceAsync(
        UserId userId,
        string deviceToken,
        string platform,
        List<string>? tags = null
    );

    /// <summary>
    /// Unregisters a device from push notifications
    /// </summary>
    /// <param name="installationId">The Azure installation ID</param>
    Task UnregisterDeviceAsync(string installationId);

    /// <summary>
    /// Sends a push notification to a specific user (all their devices)
    /// </summary>
    /// <param name="userId">The user to notify</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message body</param>
    /// <param name="data">Optional custom data payload</param>
    Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null
    );

    /// <summary>
    /// Sends push notifications to multiple users in bulk
    /// </summary>
    /// <param name="notifications">List of notifications to send</param>
    Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications
    );

    /// <summary>
    /// Sends notification to all users matching a specific tag
    /// </summary>
    /// <param name="tag">The tag to target (e.g., "platform:android", "userId:123")</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message body</param>
    /// <param name="data">Optional custom data payload</param>
    Task SendTaggedNotificationAsync(
        string tag,
        string title,
        string message,
        Dictionary<string, string>? data = null
    );
}
```

### Step 2.5: Implement Azure Notification Hub Service

**File**: `src/AICalendar.Infrastructure/Services/AzureNotificationHubService.cs`

```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Configuration;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AICalendar.Infrastructure.Services;

/// <summary>
/// Azure Notification Hub implementation for cross-platform push notifications
/// </summary>
public class AzureNotificationHubService : INotificationService
{
    private readonly ILogger<AzureNotificationHubService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly NotificationHubClient _hubClient;
    private readonly AzureNotificationHubSettings _settings;

    public AzureNotificationHubService(
        ILogger<AzureNotificationHubService> logger,
        IUserRepository userRepository,
        IOptions<AzureNotificationHubSettings> settings)
    {
        _logger = logger;
        _userRepository = userRepository;
        _settings = settings.Value;

        if (!_settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "Azure Notification Hub is not configured. " +
                "Please set ConnectionString and HubName in appsettings.json"
            );
        }

        _hubClient = NotificationHubClient.CreateClientFromConnectionString(
            _settings.ConnectionString,
            _settings.HubName
        );
    }

    public async Task<string> RegisterDeviceAsync(
        UserId userId,
        string deviceToken,
        string platform,
        List<string>? tags = null)
    {
        try
        {
            // Generate unique installation ID
            var installationId = $"{platform}_{userId.Value}_{Guid.NewGuid():N}";

            // Determine notification platform
            var notificationPlatform = platform.ToLower() switch
            {
                "android" => NotificationPlatform.Fcm,
                "ios" => NotificationPlatform.Apns,
                _ => throw new ArgumentException($"Unsupported platform: {platform}")
            };

            // Create installation object
            var installation = new Installation
            {
                InstallationId = installationId,
                Platform = notificationPlatform,
                PushChannel = deviceToken,
                Tags = new List<string>
                {
                    $"userId:{userId.Value}",      // Tag by user for targeted notifications
                    $"platform:{platform.ToLower()}" // Tag by platform for platform-specific broadcasts
                }
            };

            // Add custom tags if provided
            if (tags != null && tags.Any())
            {
                foreach (var tag in tags)
                {
                    installation.Tags.Add(tag);
                }
            }

            // Create or update installation in Azure Notification Hub
            await _hubClient.CreateOrUpdateInstallationAsync(installation);

            _logger.LogInformation(
                "Successfully registered device for user {UserId} on platform {Platform}. Installation ID: {InstallationId}",
                userId.Value,
                platform,
                installationId
            );

            return installationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to register device for user {UserId} on platform {Platform}",
                userId.Value,
                platform
            );
            throw;
        }
    }

    public async Task UnregisterDeviceAsync(string installationId)
    {
        try
        {
            await _hubClient.DeleteInstallationAsync(installationId);

            _logger.LogInformation(
                "Successfully unregistered device with installation ID: {InstallationId}",
                installationId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to unregister device with installation ID: {InstallationId}",
                installationId
            );
            throw;
        }
    }

    public async Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null)
    {
        try
        {
            // Get user to verify they exist
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning(
                    "User {UserId} not found. Cannot send notification.",
                    userId.Value
                );
                return;
            }

            if (string.IsNullOrEmpty(user.NotificationInstallationId))
            {
                _logger.LogWarning(
                    "User {UserId} has no registered device. Cannot send notification.",
                    userId.Value
                );
                return;
            }

            // Target specific user using tag expression
            var tagExpression = $"userId:{userId.Value}";

            // Build platform-specific payloads
            var androidPayload = BuildAndroidPayload(title, message, data);
            var iosPayload = BuildiOSPayload(title, message, data);

            // Send to Android devices
            NotificationOutcome? androidResult = null;
            try
            {
                androidResult = await _hubClient.SendFcmNativeNotificationAsync(
                    androidPayload,
                    tagExpression
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Android notification to user {UserId}", userId.Value);
            }

            // Send to iOS devices
            NotificationOutcome? iosResult = null;
            try
            {
                iosResult = await _hubClient.SendAppleNativeNotificationAsync(
                    iosPayload,
                    tagExpression
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send iOS notification to user {UserId}", userId.Value);
            }

            _logger.LogInformation(
                "Sent notification to user {UserId}. Android: {AndroidState}, iOS: {iOSState}",
                userId.Value,
                androidResult?.State ?? "Not Sent",
                iosResult?.State ?? "Not Sent"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending notification to user {UserId}",
                userId.Value
            );
        }
    }

    public async Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications)
    {
        _logger.LogInformation(
            "Sending {Count} bulk notifications",
            notifications.Count
        );

        // Send notifications in parallel for better performance
        var tasks = notifications.Select(n =>
            SendPushNotificationAsync(n.userId, n.title, n.message)
        );

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Completed sending {Count} bulk notifications",
            notifications.Count
        );
    }

    public async Task SendTaggedNotificationAsync(
        string tag,
        string title,
        string message,
        Dictionary<string, string>? data = null)
    {
        try
        {
            var androidPayload = BuildAndroidPayload(title, message, data);
            var iosPayload = BuildiOSPayload(title, message, data);

            // Send to Android devices with tag
            NotificationOutcome? androidResult = null;
            try
            {
                androidResult = await _hubClient.SendFcmNativeNotificationAsync(
                    androidPayload,
                    tag
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Android notification to tag '{Tag}'", tag);
            }

            // Send to iOS devices with tag
            NotificationOutcome? iosResult = null;
            try
            {
                iosResult = await _hubClient.SendAppleNativeNotificationAsync(
                    iosPayload,
                    tag
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send iOS notification to tag '{Tag}'", tag);
            }

            _logger.LogInformation(
                "Sent tagged notification to '{Tag}'. Android: {AndroidState}, iOS: {iOSState}",
                tag,
                androidResult?.State ?? "Not Sent",
                iosResult?.State ?? "Not Sent"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending tagged notification to '{Tag}'",
                tag
            );
        }
    }

    /// <summary>
    /// Builds Android (FCM) notification payload in JSON format
    /// </summary>
    private string BuildAndroidPayload(
        string title,
        string message,
        Dictionary<string, string>? data)
    {
        var payload = new
        {
            data = new Dictionary<string, string>
            {
                { "title", title },
                { "message", message },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            }.Concat(data ?? new Dictionary<string, string>())
             .ToDictionary(x => x.Key, x => x.Value),
            priority = "high"
        };

        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Builds iOS (APNs) notification payload in JSON format
    /// </summary>
    private string BuildiOSPayload(
        string title,
        string message,
        Dictionary<string, string>? data)
    {
        var payload = new Dictionary<string, object>
        {
            {
                "aps", new
                {
                    alert = new
                    {
                        title,
                        body = message
                    },
                    sound = "default",
                    badge = 1
                }
            }
        };

        // Add custom data at root level
        if (data != null)
        {
            foreach (var kvp in data)
            {
                payload[kvp.Key] = kvp.Value;
            }
        }

        return JsonSerializer.Serialize(payload);
    }
}
```

### Step 2.6: Create Null Notification Service (For Development)

**File**: `src/AICalendar.Infrastructure/Services/NullNotificationService.cs`

```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Services;

/// <summary>
/// Null implementation of notification service for development/testing
/// Logs notification operations without actually sending them
/// </summary>
public class NullNotificationService : INotificationService
{
    private readonly ILogger<NullNotificationService> _logger;

    public NullNotificationService(ILogger<NullNotificationService> logger)
    {
        _logger = logger;
    }

    public Task<string> RegisterDeviceAsync(
        UserId userId,
        string deviceToken,
        string platform,
        List<string>? tags = null)
    {
        var fakeInstallationId = $"null-{platform}-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "[NULL NOTIFICATION] Would register device for user {UserId} on platform {Platform}. " +
            "Generated installation ID: {InstallationId}",
            userId.Value,
            platform,
            fakeInstallationId
        );

        return Task.FromResult(fakeInstallationId);
    }

    public Task UnregisterDeviceAsync(string installationId)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would unregister device with installation ID: {InstallationId}",
            installationId
        );

        return Task.CompletedTask;
    }

    public Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would send notification to user {UserId}:\n" +
            "  Title: {Title}\n" +
            "  Message: {Message}\n" +
            "  Data: {Data}",
            userId.Value,
            title,
            message,
            data != null ? string.Join(", ", data.Select(x => $"{x.Key}={x.Value}")) : "None"
        );

        return Task.CompletedTask;
    }

    public Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would send {Count} bulk notifications",
            notifications.Count
        );

        foreach (var (userId, title, message) in notifications)
        {
            _logger.LogDebug(
                "[NULL NOTIFICATION]   - User {UserId}: {Title} - {Message}",
                userId.Value,
                title,
                message
            );
        }

        return Task.CompletedTask;
    }

    public Task SendTaggedNotificationAsync(
        string tag,
        string title,
        string message,
        Dictionary<string, string>? data = null)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would send tagged notification to '{Tag}':\n" +
            "  Title: {Title}\n" +
            "  Message: {Message}",
            tag,
            title,
            message
        );

        return Task.CompletedTask;
    }
}
```

### Step 2.7: Configure Services in Program.cs

**File**: `src/AICalendar.API/Program.cs`

Add these using statements:
```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Infrastructure.Configuration;
using AICalendar.Infrastructure.Services;
```

Add configuration before `builder.Build()`:

```csharp
// ═══════════════════════════════════════════════════════════
// Azure Notification Hub Configuration
// ═══════════════════════════════════════════════════════════

// Bind configuration
builder.Services.Configure<AzureNotificationHubSettings>(
    builder.Configuration.GetSection(AzureNotificationHubSettings.SectionName)
);

var azureNotificationSettings = builder.Configuration
    .GetSection(AzureNotificationHubSettings.SectionName)
    .Get<AzureNotificationHubSettings>();

if (azureNotificationSettings?.IsConfigured == true)
{
    // Register real Azure Notification Hub service
    builder.Services.AddScoped<INotificationService, AzureNotificationHubService>();

    Console.WriteLine("✅ Azure Notification Hub initialized");
    Console.WriteLine($"   Hub Name: {azureNotificationSettings.HubName}");
}
else
{
    // Register null service for development (logs without sending)
    builder.Services.AddScoped<INotificationService, NullNotificationService>();

    Console.WriteLine("⚠️  WARNING: Azure Notification Hub not configured.");
    Console.WriteLine("   Using NullNotificationService (notifications will be logged only).");
    Console.WriteLine("   To enable notifications, configure AzureNotificationHub in appsettings.json");
}
```

---

## Phase 3: Database Changes

### Step 3.1: Update User Entity

**File**: `src/AICalendar.Domain/Entities/User.cs`

Add these properties and methods:

```csharp
public class User : AggregateRoot<UserId>
{
    // ========================================
    // Existing Properties
    // ========================================
    public string Email { get; private set; }
    public string Name { get; private set; }
    // ... other existing properties ...

    // ========================================
    // Azure Notification Hub Properties
    // ========================================

    /// <summary>
    /// Installation ID from Azure Notification Hub
    /// </summary>
    public string? NotificationInstallationId { get; private set; }

    /// <summary>
    /// Platform-specific device token (FCM token for Android, APNs token for iOS)
    /// </summary>
    public string? DeviceToken { get; private set; }

    /// <summary>
    /// Device platform: "android" or "ios"
    /// </summary>
    public string? DevicePlatform { get; private set; }

    /// <summary>
    /// When the device was registered for notifications
    /// </summary>
    public DateTime? DeviceRegisteredAt { get; private set; }

    // ========================================
    // Device Registration Methods
    // ========================================

    /// <summary>
    /// Registers a device for push notifications
    /// </summary>
    public void RegisterDevice(string installationId, string deviceToken, string platform)
    {
        if (string.IsNullOrWhiteSpace(installationId))
            throw new ArgumentException("Installation ID cannot be empty", nameof(installationId));

        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ArgumentException("Device token cannot be empty", nameof(deviceToken));

        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException("Platform cannot be empty", nameof(platform));

        NotificationInstallationId = installationId;
        DeviceToken = deviceToken;
        DevicePlatform = platform.ToLower();
        DeviceRegisteredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unregisters the device from push notifications
    /// </summary>
    public void UnregisterDevice()
    {
        NotificationInstallationId = null;
        DeviceToken = null;
        DevicePlatform = null;
        DeviceRegisteredAt = null;
    }

    /// <summary>
    /// Checks if user has a registered device
    /// </summary>
    public bool HasRegisteredDevice => !string.IsNullOrEmpty(NotificationInstallationId);
}
```

### Step 3.2: Create Database Migration

```bash
cd src/AICalendar.Infrastructure
dotnet ef migrations add AddAzureNotificationHubFields --startup-project ../AICalendar.API
```

This will generate a migration file in `src/AICalendar.Infrastructure/Migrations/`.

### Step 3.3: Review Generated Migration

Open the generated migration file and verify it contains:

```csharp
public partial class AddAzureNotificationHubFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NotificationInstallationId",
            table: "Users",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeviceToken",
            table: "Users",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DevicePlatform",
            table: "Users",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeviceRegisteredAt",
            table: "Users",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "NotificationInstallationId", table: "Users");
        migrationBuilder.DropColumn(name: "DeviceToken", table: "Users");
        migrationBuilder.DropColumn(name: "DevicePlatform", table: "Users");
        migrationBuilder.DropColumn(name: "DeviceRegisteredAt", table: "Users");
    }
}
```

### Step 3.4: Apply Migration to Database

```bash
dotnet ef database update --startup-project ../AICalendar.API
```

Verify in SQL Server:
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
AND COLUMN_NAME IN ('NotificationInstallationId', 'DeviceToken', 'DevicePlatform', 'DeviceRegisteredAt');
```

### Step 3.5: Update User Repository Interface

**File**: `src/AICalendar.Domain/Interfaces/IUserRepository.cs`

```csharp
using AICalendar.Domain.Entities;
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId userId);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);

    /// <summary>
    /// Gets all users who have registered devices for notifications
    /// </summary>
    Task<List<User>> GetUsersWithRegisteredDevicesAsync();
}
```

### Step 3.6: Implement User Repository

**File**: `src/AICalendar.Infrastructure/Persistence/Repositories/UserRepository.cs`

```csharp
using AICalendar.Domain.Entities;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using AICalendar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(UserId userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
    }

    public async Task<List<User>> GetUsersWithRegisteredDevicesAsync()
    {
        return await _context.Users
            .Where(u => u.NotificationInstallationId != null)
            .ToListAsync();
    }
}
```

### Step 3.7: Register Repository in Program.cs

**File**: `src/AICalendar.API/Program.cs`

```csharp
// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

---

## Phase 4: Frontend Integration

### Option A: Android (Kotlin) - Using Google Play Services

**NO Firebase SDK needed!** We only use Google Play Services to get FCM tokens.

#### Step 4A.1: Add Dependencies

**File**: `app/build.gradle.kts`

```kotlin
dependencies {
    // Google Play Services for FCM tokens (NO Firebase SDK!)
    implementation("com.google.android.gms:play-services-base:18.3.0")

    // Retrofit for API calls
    implementation("com.squareup.retrofit2:retrofit:2.9.0")
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")

    // Coroutines
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-play-services:1.7.3")
}
```

**Important**: We're using `play-services-base`, **NOT** Firebase libraries!

#### Step 4A.2: Add FCM Configuration

Create **File**: `app/src/main/res/values/fcm_config.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <!-- Get this from Firebase Console → Project Settings → Cloud Messaging → Sender ID -->
    <string name="fcm_sender_id">YOUR_SENDER_ID_HERE</string>
</resources>
```

Replace `YOUR_SENDER_ID_HERE` with the numeric Sender ID from Firebase Console.

#### Step 4A.3: Create Notification Service

**File**: `app/src/main/java/com/aicalendar/app/services/AzureNotificationService.kt`

```kotlin
package com.aicalendar.app.services

import android.content.Context
import android.util.Log
import com.google.android.gms.tasks.Task
import com.google.android.gms.tasks.Tasks
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.POST
import java.io.IOException
import java.util.concurrent.Executor

/**
 * Service for registering devices with Azure Notification Hub via backend API
 */
class AzureNotificationService(private val context: Context) {

    private val api: BackendApi

    init {
        val retrofit = Retrofit.Builder()
            .baseUrl("https://your-api-url.com/api/") // TODO: Replace with your API URL
            .addConverterFactory(GsonConverterFactory.create())
            .build()

        api = retrofit.create(BackendApi::class.java)
    }

    /**
     * Gets FCM token from Google Play Services
     */
    suspend fun getFcmToken(senderId: String): Result<String> = withContext(Dispatchers.IO) {
        try {
            // Get FCM token using Google Play Services
            // This is the ONLY Firebase/FCM component we use - just to get the token!
            val token = Tasks.await(
                com.google.android.gms.tasks.TaskExecutors.MAIN_THREAD.execute {
                    // Use InstanceID to get token
                    com.google.firebase.iid.FirebaseInstanceId.getInstance()
                        .getToken(senderId, "FCM")
                }
            )

            if (token != null) {
                Log.d(TAG, "FCM Token obtained: ${token.take(20)}...")
                Result.success(token)
            } else {
                Result.failure(Exception("Failed to get FCM token"))
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error getting FCM token", e)
            Result.failure(e)
        }
    }

    /**
     * Registers device with backend (which registers with Azure Notification Hub)
     */
    suspend fun registerDevice(
        userId: String,
        deviceToken: String
    ): Result<String> = withContext(Dispatchers.IO) {
        try {
            val request = RegisterDeviceRequest(
                userId = userId,
                deviceToken = deviceToken,
                platform = "android"
            )

            val response = api.registerDevice(request)

            if (response.isSuccessful && response.body() != null) {
                val installationId = response.body()!!.installationId
                Log.d(TAG, "Device registered successfully: $installationId")
                Result.success(installationId)
            } else {
                val error = "Failed to register device: ${response.code()} ${response.message()}"
                Log.e(TAG, error)
                Result.failure(Exception(error))
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error registering device", e)
            Result.failure(e)
        }
    }

    /**
     * Complete registration flow: get token + register with backend
     */
    suspend fun completeRegistration(userId: String, senderId: String): Result<String> {
        // Step 1: Get FCM token
        val tokenResult = getFcmToken(senderId)
        if (tokenResult.isFailure) {
            return Result.failure(tokenResult.exceptionOrNull()!!)
        }

        val deviceToken = tokenResult.getOrNull()!!

        // Step 2: Register with backend
        return registerDevice(userId, deviceToken)
    }

    companion object {
        private const val TAG = "AzureNotificationSvc"
    }
}

// ========================================
// API Models
// ========================================

interface BackendApi {
    @POST("users/register-device")
    suspend fun registerDevice(@Body request: RegisterDeviceRequest): retrofit2.Response<RegisterDeviceResponse>
}

data class RegisterDeviceRequest(
    val userId: String,
    val deviceToken: String,
    val platform: String
)

data class RegisterDeviceResponse(
    val installationId: String,
    val message: String
)
```

#### Step 4A.4: Create Message Receiver Service

**File**: `app/src/main/java/com/aicalendar/app/services/PushNotificationReceiver.kt`

```kotlin
package com.aicalendar.app.services

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import com.aicalendar.app.MainActivity
import com.aicalendar.app.R
import org.json.JSONObject

/**
 * Receives push notifications from FCM (sent via Azure Notification Hub)
 */
class PushNotificationReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, intent: Intent) {
        Log.d(TAG, "Push notification received")

        // Extract notification data
        val title = intent.getStringExtra("title") ?: "AICalendar"
        val message = intent.getStringExtra("message") ?: ""
        val data = mutableMapOf<String, String>()

        // Extract all extras as data
        intent.extras?.keySet()?.forEach { key ->
            intent.getStringExtra(key)?.let { value ->
                data[key] = value
            }
        }

        Log.d(TAG, "Notification - Title: $title, Message: $message, Data: $data")

        // Show notification
        showNotification(context, title, message, data)
    }

    private fun showNotification(
        context: Context,
        title: String,
        message: String,
        data: Map<String, String>
    ) {
        val notificationManager =
            context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager

        val channelId = "payment_reminders"

        // Create notification channel (Android 8.0+)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                channelId,
                "Payment Reminders",
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                description = "Notifications for payment reminders"
                enableLights(true)
                enableVibration(true)
            }
            notificationManager.createNotificationChannel(channel)
        }

        // Create intent to open app when notification is tapped
        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            // Add data to intent
            data.forEach { (key, value) ->
                putExtra(key, value)
            }
        }

        val pendingIntent = PendingIntent.getActivity(
            context,
            System.currentTimeMillis().toInt(),
            intent,
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        // Build and show notification
        val notification = NotificationCompat.Builder(context, channelId)
            .setContentTitle(title)
            .setContentText(message)
            .setSmallIcon(R.drawable.ic_notification) // TODO: Add your notification icon
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setDefaults(NotificationCompat.DEFAULT_ALL)
            .build()

        notificationManager.notify(System.currentTimeMillis().toInt(), notification)
    }

    companion object {
        private const val TAG = "PushNotificationRcvr"
    }
}
```

#### Step 4A.5: Register Receiver in AndroidManifest.xml

**File**: `app/src/main/AndroidManifest.xml`

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.aicalendar.app">

    <!-- Permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
    <uses-permission android:name="com.google.android.c2dm.permission.RECEIVE" />

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

        <!-- Push Notification Receiver -->
        <receiver
            android:name=".services.PushNotificationReceiver"
            android:exported="true"
            android:permission="com.google.android.c2dm.permission.SEND">
            <intent-filter>
                <action android:name="com.google.android.c2dm.intent.RECEIVE" />
                <category android:name="com.aicalendar.app" />
            </intent-filter>
        </receiver>

    </application>

</manifest>
```

#### Step 4A.6: Initialize in MainActivity

**File**: `app/src/main/java/com/aicalendar/app/MainActivity.kt`

```kotlin
package com.aicalendar.app

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.util.Log
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import com.aicalendar.app.services.AzureNotificationService
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {

    private lateinit var notificationService: AzureNotificationService

    private val requestPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            Log.d(TAG, "Notification permission granted")
            registerDeviceForNotifications()
        } else {
            Log.w(TAG, "Notification permission denied")
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        notificationService = AzureNotificationService(this)

        // Request notification permission (Android 13+)
        requestNotificationPermission()
    }

    private fun requestNotificationPermission() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            when {
                ContextCompat.checkSelfPermission(
                    this,
                    Manifest.permission.POST_NOTIFICATIONS
                ) == PackageManager.PERMISSION_GRANTED -> {
                    Log.d(TAG, "Notification permission already granted")
                    registerDeviceForNotifications()
                }
                shouldShowRequestPermissionRationale(Manifest.permission.POST_NOTIFICATIONS) -> {
                    // Show explanation to user
                    Log.d(TAG, "Should show permission rationale")
                    requestPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
                }
                else -> {
                    requestPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
                }
            }
        } else {
            // No permission needed for Android 12 and below
            registerDeviceForNotifications()
        }
    }

    private fun registerDeviceForNotifications() {
        // Get user ID from your auth system
        val userId = getUserId() // TODO: Implement this
        if (userId == null) {
            Log.w(TAG, "No user ID available, skipping device registration")
            return
        }

        // Get FCM Sender ID from resources
        val senderId = getString(R.string.fcm_sender_id)

        lifecycleScope.launch {
            Log.d(TAG, "Starting device registration...")

            val result = notificationService.completeRegistration(userId, senderId)

            result.onSuccess { installationId ->
                Log.d(TAG, "Device registered successfully: $installationId")
                saveInstallationId(installationId)
            }.onFailure { error ->
                Log.e(TAG, "Failed to register device", error)
            }
        }
    }

    private fun getUserId(): String? {
        // TODO: Get from your authentication system (SharedPreferences, JWT, etc.)
        val prefs = getSharedPreferences("app_prefs", MODE_PRIVATE)
        return prefs.getString("user_id", null)
    }

    private fun saveInstallationId(installationId: String) {
        val prefs = getSharedPreferences("app_prefs", MODE_PRIVATE)
        prefs.edit().putString("installation_id", installationId).apply()
        Log.d(TAG, "Installation ID saved")
    }

    companion object {
        private const val TAG = "MainActivity"
    }
}
```

### Option B: iOS (Swift) - Using Native APNs

**NO Firebase SDK needed!** iOS uses native UserNotifications framework.

#### Step 4B.1: Enable Push Notifications in Xcode

1. Open your Xcode project
2. Select your app target
3. Click **"Signing & Capabilities"** tab
4. Click **"+ Capability"**
5. Add **"Push Notifications"**
6. Add **"Background Modes"** → Check **"Remote notifications"**

#### Step 4B.2: Create Notification Service

**File**: `AICalendar/Services/NotificationService.swift`

```swift
import Foundation
import UserNotifications

/// Service for managing push notifications with Azure Notification Hub
class NotificationService {
    static let shared = NotificationService()

    private let baseURL = "https://your-api-url.com/api" // TODO: Replace with your API URL

    private init() {}

    // MARK: - Permission Management

    /// Requests notification permission from user
    func requestPermission(completion: @escaping (Bool) -> Void) {
        UNUserNotificationCenter.current().requestAuthorization(
            options: [.alert, .badge, .sound]
        ) { granted, error in
            if let error = error {
                print("❌ Error requesting notification permission: \(error)")
            }

            DispatchQueue.main.async {
                completion(granted)
            }
        }
    }

    // MARK: - Device Registration

    /// Registers device with backend (which registers with Azure Notification Hub)
    func registerDevice(userId: String, deviceToken: Data) async throws -> String {
        // Convert device token to hex string
        let tokenString = deviceToken.map { String(format: "%02.2hhx", $0) }.joined()
        print("📱 Device Token (first 20 chars): \(String(tokenString.prefix(20)))...")

        // Prepare API request
        guard let url = URL(string: "\(baseURL)/users/register-device") else {
            throw NSError(
                domain: "NotificationService",
                code: -1,
                userInfo: [NSLocalizedDescriptionKey: "Invalid API URL"]
            )
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let body: [String: Any] = [
            "userId": userId,
            "deviceToken": tokenString,
            "platform": "ios"
        ]

        request.httpBody = try JSONSerialization.data(withJSONObject: body)

        // Send request
        let (data, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw NSError(
                domain: "NotificationService",
                code: -1,
                userInfo: [NSLocalizedDescriptionKey: "Invalid response"]
            )
        }

        guard (200...299).contains(httpResponse.statusCode) else {
            let errorMessage = String(data: data, encoding: .utf8) ?? "Unknown error"
            throw NSError(
                domain: "NotificationService",
                code: httpResponse.statusCode,
                userInfo: [NSLocalizedDescriptionKey: "Failed to register device: \(errorMessage)"]
            )
        }

        // Parse response
        let result = try JSONDecoder().decode(RegisterDeviceResponse.self, from: data)
        print("✅ Device registered successfully: \(result.installationId)")

        return result.installationId
    }

    /// Unregisters device from notifications
    func unregisterDevice(userId: String) async throws {
        guard let url = URL(string: "\(baseURL)/users/unregister-device/\(userId)") else {
            throw NSError(
                domain: "NotificationService",
                code: -1,
                userInfo: [NSLocalizedDescriptionKey: "Invalid API URL"]
            )
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"

        let (_, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse,
              (200...299).contains(httpResponse.statusCode) else {
            throw NSError(
                domain: "NotificationService",
                code: -1,
                userInfo: [NSLocalizedDescriptionKey: "Failed to unregister device"]
            )
        }

        print("✅ Device unregistered successfully")
    }
}

// MARK: - API Models

struct RegisterDeviceResponse: Codable {
    let installationId: String
    let message: String
}
```

#### Step 4B.3: Update AppDelegate

**File**: `AICalendar/AppDelegate.swift`

```swift
import UIKit
import UserNotifications

@main
class AppDelegate: UIResponder, UIApplicationDelegate {

    func application(
        _ application: UIApplication,
        didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
    ) -> Bool {

        // Set notification delegate
        UNUserNotificationCenter.current().delegate = self

        // Register for remote notifications
        application.registerForRemoteNotifications()

        print("✅ App launched, registered for remote notifications")

        return true
    }

    // MARK: - Remote Notifications

    /// Called when APNs successfully registers the device
    func application(
        _ application: UIApplication,
        didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data
    ) {
        print("✅ Successfully registered for remote notifications")

        // Get user ID from your auth system
        guard let userId = UserDefaults.standard.string(forKey: "userId") else {
            print("⚠️ No user ID found, skipping device registration")
            return
        }

        // Register with backend (Azure Notification Hub via API)
        Task {
            do {
                let installationId = try await NotificationService.shared.registerDevice(
                    userId: userId,
                    deviceToken: deviceToken
                )

                // Save installation ID
                UserDefaults.standard.set(installationId, forKey: "installationId")
                print("💾 Installation ID saved: \(installationId)")

            } catch {
                print("❌ Failed to register device: \(error.localizedDescription)")
            }
        }
    }

    /// Called when APNs fails to register the device
    func application(
        _ application: UIApplication,
        didFailToRegisterForRemoteNotificationsWithError error: Error
    ) {
        print("❌ Failed to register for remote notifications: \(error.localizedDescription)")
    }

    // MARK: - Configuration

    func application(
        _ application: UIApplication,
        configurationForConnecting connectingSceneSession: UISceneSession,
        options: UIScene.ConnectionOptions
    ) -> UISceneConfiguration {
        return UISceneConfiguration(
            name: "Default Configuration",
            sessionRole: connectingSceneSession.role
        )
    }
}

// MARK: - UNUserNotificationCenterDelegate

extension AppDelegate: UNUserNotificationCenterDelegate {

    /// Handle notification when app is in foreground
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification,
        withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void
    ) {
        print("📬 Notification received in foreground")

        let userInfo = notification.request.content.userInfo
        print("Notification data: \(userInfo)")

        // Show notification even when app is in foreground
        if #available(iOS 14.0, *) {
            completionHandler([.banner, .sound, .badge])
        } else {
            completionHandler([.alert, .sound, .badge])
        }
    }

    /// Handle notification tap
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        didReceive response: UNNotificationResponse,
        withCompletionHandler completionHandler: @escaping () -> Void
    ) {
        let userInfo = response.notification.request.content.userInfo
        print("👆 Notification tapped with data: \(userInfo)")

        // Handle notification tap (e.g., navigate to specific screen)
        handleNotificationTap(userInfo: userInfo)

        completionHandler()
    }

    /// Handles navigation when notification is tapped
    private func handleNotificationTap(userInfo: [AnyHashable: Any]) {
        // TODO: Navigate to relevant screen based on userInfo

        if let calendarItemId = userInfo["calendarItemId"] as? String {
            print("🗓️ Navigate to calendar item: \(calendarItemId)")
            // Implement navigation (e.g., post notification to SceneDelegate)
            NotificationCenter.default.post(
                name: NSNotification.Name("NavigateToCalendarItem"),
                object: nil,
                userInfo: ["calendarItemId": calendarItemId]
            )
        }
    }
}
```

#### Step 4B.4: Request Permission After Login

**File**: `AICalendar/Views/LoginView.swift` (or wherever you handle login)

```swift
import SwiftUI

struct LoginView: View {
    @State private var email = ""
    @State private var password = ""
    @State private var isLoading = false
    @State private var showAlert = false
    @State private var alertMessage = ""

    var body: some View {
        VStack(spacing: 20) {
            Text("AICalendar")
                .font(.largeTitle)
                .fontWeight(.bold)

            TextField("Email", text: $email)
                .textFieldStyle(RoundedBorderTextFieldStyle())
                .autocapitalization(.none)
                .keyboardType(.emailAddress)

            SecureField("Password", text: $password)
                .textFieldStyle(RoundedBorderTextFieldStyle())

            Button(action: performLogin) {
                if isLoading {
                    ProgressView()
                        .progressViewStyle(CircularProgressViewStyle(tint: .white))
                } else {
                    Text("Login")
                        .fontWeight(.semibold)
                }
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(Color.blue)
            .foregroundColor(.white)
            .cornerRadius(10)
            .disabled(isLoading)
        }
        .padding()
        .alert("Error", isPresented: $showAlert) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(alertMessage)
        }
    }

    private func performLogin() {
        isLoading = true

        // TODO: Implement your login logic
        // After successful login:

        // 1. Save user ID
        let userId = "user-123" // TODO: Get from login response
        UserDefaults.standard.set(userId, forKey: "userId")

        // 2. Request notification permission
        NotificationService.shared.requestPermission { granted in
            if granted {
                print("✅ Notification permission granted")

                // 3. Register for remote notifications
                DispatchQueue.main.async {
                    UIApplication.shared.registerForRemoteNotifications()
                }
            } else {
                print("⚠️ Notification permission denied")
                self.alertMessage = "Please enable notifications in Settings to receive payment reminders"
                self.showAlert = true
            }

            isLoading = false
        }
    }
}
```

---

## Phase 5: Testing

### Step 5.1: Create API Controller

**File**: `src/AICalendar.API/Controllers/UserNotificationsController.cs`

```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/user-notifications")]
[Produces("application/json")]
public class UserNotificationsController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserNotificationsController> _logger;

    public UserNotificationsController(
        IUserRepository userRepository,
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<UserNotificationsController> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Registers a device for push notifications with Azure Notification Hub
    /// </summary>
    /// <param name="request">Device registration details</param>
    /// <returns>Installation ID from Azure Notification Hub</returns>
    [HttpPost("register-device")]
    [ProducesResponseType(typeof(RegisterDeviceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        // Validate request
        if (request.UserId == Guid.Empty)
            return BadRequest("User ID is required");

        if (string.IsNullOrWhiteSpace(request.DeviceToken))
            return BadRequest("Device token is required");

        if (string.IsNullOrWhiteSpace(request.Platform))
            return BadRequest("Platform is required");

        if (request.Platform.ToLower() != "android" && request.Platform.ToLower() != "ios")
            return BadRequest("Platform must be 'android' or 'ios'");

        // Get user
        var user = await _userRepository.GetByIdAsync(UserId.Create(request.UserId));
        if (user == null)
            return NotFound($"User {request.UserId} not found");

        try
        {
            // Register with Azure Notification Hub
            var installationId = await _notificationService.RegisterDeviceAsync(
                UserId.Create(request.UserId),
                request.DeviceToken,
                request.Platform,
                request.Tags
            );

            // Update user record
            user.RegisterDevice(installationId, request.DeviceToken, request.Platform);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully registered device for user {UserId} on platform {Platform}. Installation ID: {InstallationId}",
                request.UserId,
                request.Platform,
                installationId
            );

            return Ok(new RegisterDeviceResponseDto
            {
                InstallationId = installationId,
                Message = "Device registered successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to register device for user {UserId} on platform {Platform}",
                request.UserId,
                request.Platform
            );

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Failed to register device. Please try again later."
            );
        }
    }

    /// <summary>
    /// Unregisters a device from push notifications
    /// </summary>
    /// <param name="userId">User ID</param>
    [HttpPost("unregister-device/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterDevice(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Create(userId));
        if (user == null)
            return NotFound($"User {userId} not found");

        try
        {
            // Unregister from Azure Notification Hub if installation ID exists
            if (!string.IsNullOrEmpty(user.NotificationInstallationId))
            {
                await _notificationService.UnregisterDeviceAsync(user.NotificationInstallationId);
            }

            // Update user record
            user.UnregisterDevice();
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully unregistered device for user {UserId}",
                userId
            );

            return Ok(new { message = "Device unregistered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to unregister device for user {UserId}",
                userId
            );

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Failed to unregister device. Please try again later."
            );
        }
    }

    /// <summary>
    /// Test endpoint to send a notification to a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    [HttpPost("test-notification/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestNotification(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Create(userId));
        if (user == null)
            return NotFound($"User {userId} not found");

        if (!user.HasRegisteredDevice)
            return BadRequest("User has no registered device");

        await _notificationService.SendPushNotificationAsync(
            UserId.Create(userId),
            "Test Notification",
            "This is a test notification from AICalendar via Azure Notification Hub! 🎉",
            new Dictionary<string, string>
            {
                { "type", "test" },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            }
        );

        return Ok(new { message = "Test notification sent successfully" });
    }

    /// <summary>
    /// Test endpoint to send tagged notification (e.g., to all Android users)
    /// </summary>
    [HttpPost("test-tagged-notification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestTaggedNotification(
        [FromQuery] string tag,
        [FromQuery] string title,
        [FromQuery] string message)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return BadRequest("Tag is required");

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("Title is required");

        if (string.IsNullOrWhiteSpace(message))
            return BadRequest("Message is required");

        await _notificationService.SendTaggedNotificationAsync(tag, title, message);

        return Ok(new { message = $"Tagged notification sent to '{tag}' successfully" });
    }

    /// <summary>
    /// Gets statistics about registered devices
    /// </summary>
    [HttpGet("notification-stats")]
    [ProducesResponseType(typeof(NotificationStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationStats()
    {
        var usersWithDevices = await _userRepository.GetUsersWithRegisteredDevicesAsync();

        var stats = new NotificationStatsDto
        {
            TotalRegisteredDevices = usersWithDevices.Count,
            AndroidDevices = usersWithDevices.Count(u => u.DevicePlatform == "android"),
            IosDevices = usersWithDevices.Count(u => u.DevicePlatform == "ios")
        };

        return Ok(stats);
    }
}

// ========================================
// DTOs
// ========================================

public record RegisterDeviceRequest(
    Guid UserId,
    string DeviceToken,
    string Platform,
    List<string>? Tags = null
);

public record RegisterDeviceResponseDto
{
    public string InstallationId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record NotificationStatsDto
{
    public int TotalRegisteredDevices { get; init; }
    public int AndroidDevices { get; init; }
    public int IosDevices { get; init; }
}
```

### Step 5.2: Start Backend and Test

1. **Start your backend**:
   ```bash
   cd src/AICalendar.API
   dotnet run
   ```

2. **Verify Azure Notification Hub is configured**:
   Look for console output:
   ```
   ✅ Azure Notification Hub initialized
      Hub Name: aicalendar-hub
   ```

3. **Run your mobile app** (Android or iOS) and login

4. **Verify device registration**:

   Check backend logs for:
   ```
   Successfully registered device for user {UserId} on platform {Platform}
   ```

   Check database:
   ```sql
   SELECT
       Id,
       Email,
       NotificationInstallationId,
       DevicePlatform,
       DeviceRegisteredAt
   FROM Users
   WHERE NotificationInstallationId IS NOT NULL;
   ```

   Check Azure Portal:
   - Go to your Notification Hub
   - Click **"Registrations"** (may take a few minutes to appear)

### Step 5.3: Send Test Notification

**Option A: Using curl**

```bash
# Replace YOUR_USER_GUID with actual user ID
curl -X POST "https://localhost:7000/api/users/test-notification/YOUR_USER_GUID"
```

**Option B: Using PowerShell**

```powershell
$userId = "YOUR_USER_GUID"
Invoke-RestMethod -Method POST -Uri "https://localhost:7000/api/users/test-notification/$userId"
```

**Option C: Using Postman**

1. Open Postman
2. Create new request:
   - Method: `POST`
   - URL: `https://localhost:7000/api/users/test-notification/{userId}`
3. Click **"Send"**

### Step 5.4: Verify Notification Received

✅ **Expected behavior**:
- **Android**: Notification appears in notification drawer
- **iOS**: Banner notification appears at top
- **Tap notification**: App opens (check console logs)

### Step 5.5: Test Tagged Notification

Send to all Android users:

```bash
curl -X POST "https://localhost:7000/api/users/test-tagged-notification?tag=platform:android&title=Android%20Test&message=Hello%20all%20Android%20users!"
```

Send to specific user:

```bash
curl -X POST "https://localhost:7000/api/users/test-tagged-notification?tag=userId:YOUR_USER_GUID&title=User%20Test&message=Hello%20specific%20user!"
```

### Step 5.6: Monitor in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to your Notification Hub: `aicalendar-hub`
3. Click **"Metrics"** in left menu
4. View:
   - Push notifications sent
   - Success/failure rates
   - Platform breakdown (Android vs iOS)

---

## Phase 6: Production Deployment

### Step 6.1: Secure Connection String

**⚠️ NEVER commit connection strings to Git!**

#### Option A: Azure Key Vault (Recommended for Production)

1. **Create Key Vault**:
   ```bash
   az keyvault create \
     --name aicalendar-keyvault \
     --resource-group AICalendar-RG \
     --location eastus
   ```

2. **Store connection string**:
   ```bash
   az keyvault secret set \
     --vault-name aicalendar-keyvault \
     --name AzureNotificationHubConnectionString \
     --value "Endpoint=sb://..."
   ```

3. **Install NuGet package**:
   ```bash
   dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
   dotnet add package Azure.Identity
   ```

4. **Update Program.cs**:
   ```csharp
   using Azure.Identity;

   var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
   if (!string.IsNullOrEmpty(keyVaultUrl))
   {
       builder.Configuration.AddAzureKeyVault(
           new Uri(keyVaultUrl),
           new DefaultAzureCredential()
       );
   }
   ```

5. **Set in App Service**:
   ```bash
   az webapp config appsettings set \
     --name aicalendar-api \
     --resource-group AICalendar-RG \
     --settings KeyVaultUrl="https://aicalendar-keyvault.vault.azure.net/"
   ```

#### Option B: Environment Variables

Set in your hosting environment:

```bash
export AZURE_NOTIFICATION_HUB_CONNECTION_STRING="Endpoint=sb://..."
export AZURE_NOTIFICATION_HUB_NAME="aicalendar-hub"
```

Update `Program.cs`:
```csharp
var connectionString =
    Environment.GetEnvironmentVariable("AZURE_NOTIFICATION_HUB_CONNECTION_STRING")
    ?? builder.Configuration["AzureNotificationHub:ConnectionString"];

var hubName =
    Environment.GetEnvironmentVariable("AZURE_NOTIFICATION_HUB_NAME")
    ?? builder.Configuration["AzureNotificationHub:HubName"];
```

#### Option C: User Secrets (Development Only)

```bash
cd src/AICalendar.API
dotnet user-secrets init
dotnet user-secrets set "AzureNotificationHub:ConnectionString" "Endpoint=sb://..."
dotnet user-secrets set "AzureNotificationHub:HubName" "aicalendar-hub"
```

### Step 6.2: Update Hangfire Job for Reminders

**File**: `src/AICalendar.Infrastructure/BackgroundJobs/SendRemindersJob.cs`

```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job to send payment reminder notifications
/// </summary>
public class SendRemindersJob
{
    private readonly ICalendarItemRepository _calendarItemRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendRemindersJob> _logger;

    public SendRemindersJob(
        ICalendarItemRepository calendarItemRepository,
        INotificationService notificationService,
        ILogger<SendRemindersJob> logger)
    {
        _calendarItemRepository = calendarItemRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Sends reminders for calendar items due in the next 24 hours
    /// </summary>
    public async Task SendDueReminders()
    {
        _logger.LogInformation("Starting payment reminder notification job");

        try
        {
            // Get unpaid calendar items due in next 24 hours
            var now = DateTime.UtcNow;
            var tomorrow = now.AddHours(24);

            var dueItems = await _calendarItemRepository.GetUnpaidItemsDueInRangeAsync(now, tomorrow);

            _logger.LogInformation(
                "Found {Count} calendar items due for reminders",
                dueItems.Count
            );

            if (!dueItems.Any())
            {
                _logger.LogInformation("No items due for reminders");
                return;
            }

            // Build notification list
            var notifications = dueItems.Select(item => (
                userId: item.UserId,
                title: "Payment Reminder 💰",
                message: $"{item.Title} is due soon! Amount: ${item.Amount:N2}"
            )).ToList();

            // Send bulk notifications via Azure Notification Hub
            await _notificationService.SendBulkNotificationsAsync(notifications);

            _logger.LogInformation(
                "Successfully sent {Count} reminder notifications",
                notifications.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in payment reminder notification job");
            throw; // Re-throw so Hangfire marks job as failed
        }
    }
}
```

### Step 6.3: Configure Hangfire Recurring Job

**File**: `src/AICalendar.Infrastructure/BackgroundJobs/HangfireConfiguration.cs`

```csharp
using Hangfire;

namespace AICalendar.Infrastructure.BackgroundJobs;

public static class HangfireConfiguration
{
    public static void ConfigureRecurringJobs(IRecurringJobManager recurringJobManager)
    {
        // Payment reminder notifications - runs every hour
        recurringJobManager.AddOrUpdate<SendRemindersJob>(
            "send-payment-reminders",
            job => job.SendDueReminders(),
            Cron.Hourly(), // Every hour
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            }
        );
    }
}
```

Register in `Program.cs`:
```csharp
// After app.UseHangfireDashboard()
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
HangfireConfiguration.ConfigureRecurringJobs(recurringJobManager);
```

### Step 6.4: Upgrade to Production Tier

When ready for production with > 500 devices:

1. Go to Azure Portal → Your Notification Hub
2. Click **"Pricing tier"** in left menu
3. Select **"Basic"** ($10/month) or **"Standard"** ($200/month)
4. Click **"Select"**

**Basic vs Standard**:
- **Basic**: Unlimited devices, 10M pushes, basic telemetry
- **Standard**: + Templates, scheduled push, multi-tenancy, advanced analytics

### Step 6.5: Monitoring and Logging

#### Application Insights

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

#### Custom Metrics

```csharp
// In AzureNotificationHubService
private readonly TelemetryClient _telemetryClient;

public async Task SendPushNotificationAsync(...)
{
    try
    {
        // ... send notification ...

        _telemetryClient.TrackMetric("NotificationsSent", 1);
        _telemetryClient.TrackMetric($"NotificationsSent_{platform}", 1);
    }
    catch
    {
        _telemetryClient.TrackMetric("NotificationsFailed", 1);
        throw;
    }
}
```

#### Azure Monitor Alerts

Set up alerts for:
- Notification failure rate > 10%
- No notifications sent in last hour (if expected)
- Hub throttling errors

### Step 6.6: Production Checklist

- [ ] Connection string stored in Key Vault (not in code)
- [ ] Platform credentials configured (APNs/FCM)
- [ ] Using **Production** APNs environment (not Sandbox)
- [ ] Notification Hub upgraded to Basic or Standard tier
- [ ] Application Insights configured
- [ ] Hangfire job scheduled correctly
- [ ] Error handling tested
- [ ] Load testing completed (if expecting high volume)
- [ ] Monitoring alerts configured

---

## Troubleshooting

### Issue 1: "Azure Notification Hub not configured"

**Console shows**:
```
⚠️  WARNING: Azure Notification Hub not configured.
   Using NullNotificationService
```

**Solution**:
1. Check `appsettings.Development.json` has correct connection string
2. Verify connection string format includes `Endpoint`, `SharedAccessKeyName`, and `SharedAccessKey`
3. Ensure `HubName` matches Azure portal

### Issue 2: "Installation not found" or "Installation expired"

**Cause**: Device registration expired or was deleted

**Solution**:
- User needs to re-open the app (triggers re-registration)
- Or manually call `registerDeviceForNotifications()` again
- Check Azure Portal → Notification Hub → Registrations (may take 5 minutes to appear)

### Issue 3: Android notifications not received

**Checklist**:
- [ ] FCM Server Key configured in Azure Portal
- [ ] Correct Sender ID in `fcm_config.xml`
- [ ] POST_NOTIFICATIONS permission granted (Android 13+)
- [ ] Device token obtained successfully (check logs)
- [ ] Installation registered in Azure (check database)
- [ ] BroadcastReceiver registered in AndroidManifest

**Debug**:
```kotlin
// Add logging in MainActivity
Log.d("DEBUG", "Sender ID: ${getString(R.string.fcm_sender_id)}")
```

### Issue 4: iOS notifications not received

**Checklist**:
- [ ] Push Notifications capability enabled in Xcode
- [ ] APNs certificate/token configured in Azure Portal
- [ ] Using correct environment (Sandbox vs Production)
- [ ] Device registered for remote notifications
- [ ] `didRegisterForRemoteNotificationsWithDeviceToken` called successfully

**Debug**:
```swift
// Check if registration succeeds
func application(
    _ application: UIApplication,
    didFailToRegisterForRemoteNotificationsWithError error: Error
) {
    print("❌ APNs registration failed: \(error)")
}
```

### Issue 5: "Permission denied" or "Authentication failed"

**Cause**: Incorrect connection string or insufficient permissions

**Solution**:
1. Verify using **DefaultFullSharedAccessSignature** (not Listen)
2. Check connection string includes `SharedAccessKey`
3. Regenerate access policy keys in Azure Portal if needed

### Issue 6: Notifications delayed or not sent

**Possible causes**:
1. **Free tier throttling**: Upgrade to Basic tier
2. **Platform issues**: Check Apple/Google service status
3. **Invalid tokens**: Old/expired device tokens

**Solutions**:
- Monitor Azure Portal → Metrics for throttling errors
- Implement token refresh on frontend
- Clean up expired installations

### Issue 7: "Could not load type 'Microsoft.Azure.NotificationHubs.NotificationHubClient'"

**Cause**: NuGet package not installed or wrong version

**Solution**:
```bash
cd src/AICalendar.API
dotnet add package Microsoft.Azure.NotificationHubs --version 4.2.0
dotnet restore
dotnet build
```

### Issue 8: Database migration fails

**Error**: "Column already exists" or similar

**Solution**:
```bash
# Rollback migration
dotnet ef database update PreviousMigrationName --startup-project ../AICalendar.API

# Delete migration file
rm Migrations/*_AddAzureNotificationHubFields.cs

# Recreate migration
dotnet ef migrations add AddAzureNotificationHubFields --startup-project ../AICalendar.API

# Apply
dotnet ef database update --startup-project ../AICalendar.API
```

---

## Additional Resources

### Official Documentation

- **Azure Notification Hubs**: https://learn.microsoft.com/en-us/azure/notification-hubs/
- **.NET SDK Reference**: https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.notificationhubs
- **REST API**: https://learn.microsoft.com/en-us/rest/api/notificationhubs/
- **APNs (Apple)**: https://developer.apple.com/documentation/usernotifications
- **FCM (Google)**: https://firebase.google.com/docs/cloud-messaging

### Useful Tools

- **Azure Portal**: https://portal.azure.com/
- **Hangfire Dashboard**: `https://your-api-url/hangfire`
- **Azure Notification Hub Test Send**: In Azure Portal → Notification Hub → Test Send
- **Postman Collection**: (Create for testing your APIs)

### Community

- **Stack Overflow**: Tag `azure-notificationhub`
- **Azure Support**: https://azure.microsoft.com/support/
- **GitHub Issues**: (Your repo)

---

## Summary Checklist

### Azure Setup
- [ ] Notification Hub namespace created
- [ ] Notification Hub created
- [ ] FCM Server Key configured (for Android)
- [ ] APNs certificate/token configured (for iOS)
- [ ] Connection string saved securely

### Backend
- [ ] NuGet package installed (`Microsoft.Azure.NotificationHubs`)
- [ ] Configuration added (`AzureNotificationHubSettings`)
- [ ] `INotificationService` interface created
- [ ] `AzureNotificationHubService` implemented
- [ ] `NullNotificationService` created (for dev)
- [ ] Services registered in `Program.cs`
- [ ] User entity updated with notification fields
- [ ] Database migration created and applied
- [ ] `UserRepository` methods implemented
- [ ] API endpoints created (`UsersController`)
- [ ] Hangfire job updated (`SendRemindersJob`)

### Frontend - Android
- [ ] Dependencies added (Google Play Services)
- [ ] FCM Sender ID configured
- [ ] `AzureNotificationService` implemented
- [ ] `PushNotificationReceiver` created
- [ ] BroadcastReceiver registered in `AndroidManifest.xml`
- [ ] Permissions added (`POST_NOTIFICATIONS`, `RECEIVE`)
- [ ] Device registration in `MainActivity`
- [ ] Notification permission requested

### Frontend - iOS
- [ ] Push Notifications capability enabled
- [ ] Background Modes enabled (Remote notifications)
- [ ] `NotificationService` created
- [ ] `AppDelegate` updated with notification handling
- [ ] Permission request implemented
- [ ] Device registration implemented

### Testing
- [ ] Backend starts without errors
- [ ] Azure Notification Hub initialized message shown
- [ ] Device registration endpoint works
- [ ] Device registered in database
- [ ] Installation visible in Azure Portal (may take 5 min)
- [ ] Test notification endpoint works
- [ ] Notification received on device
- [ ] Notification tap opens app
- [ ] Hangfire job runs successfully

### Production
- [ ] Connection string in Key Vault (not code)
- [ ] Using **Production** APNs environment
- [ ] Notification Hub tier upgraded (Basic/Standard) if needed
- [ ] Application Insights configured
- [ ] Monitoring alerts set up
- [ ] Error handling tested
- [ ] Load testing completed
- [ ] Documentation updated

---

**🎉 Congratulations!** You now have a complete Azure Notification Hub implementation for your AICalendar project using platform-native approaches (no Firebase SDK on frontend)!

---

## Key Takeaways

✅ **Azure handles all platform routing** - You send once, Azure delivers to APNs/FCM

✅ **No Firebase SDK needed** - Android uses Google Play Services only for tokens

✅ **Tag-based targeting** - Send to specific users, platforms, or groups

✅ **Production-ready** - Scales to millions of devices

✅ **Cost-effective** - Start free, upgrade as you grow ($10/month for unlimited devices)
