# Firebase Cloud Messaging (FCM) Implementation Guide

Complete step-by-step guide to implement push notifications using Firebase Cloud Messaging in the AICalendar project.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Phase 1: Firebase Project Setup](#phase-1-firebase-project-setup)
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

User logs in → Gets FCM device token → Sends to backend
    ↓
Backend stores token in User table
    ↓
Hangfire SendRemindersJob runs every hour
    ↓
Checks unpaid calendar items
    ↓
Sends notification via Firebase Admin SDK
    ↓
Firebase Cloud Messaging
    ↓
User's device receives push notification
```

### Technologies Used

- **Backend**: .NET 8 + Firebase Admin SDK
- **Android Frontend**: Kotlin + Firebase Cloud Messaging SDK
- **iOS Frontend**: Swift + Firebase Cloud Messaging SDK
- **Database**: SQL Server (add FCM token column)
- **Job Scheduler**: Hangfire (already configured)

### Notification Schedule

The system sends payment reminders at strategic intervals before and after the payment due date:

**Before Payment Due:**
- 24 hours before
- 6 hours before
- 1 hour before

**After Payment Due (Overdue):**
- 12 hours after
- 24 hours after
- 48 hours after

**Total: 6 notifications per unpaid item**

#### Example Timeline

For a payment due on **January 15, 2025 at 2:00 PM**:

```
Before Due:
├─ Jan 14, 2:00 PM  → "Payment due in 24 hours: $100.00"
├─ Jan 15, 8:00 AM  → "Payment due in 6 hours: $100.00"
└─ Jan 15, 1:00 PM  → "Payment due in 1 hour: $100.00"

Due Time:
    Jan 15, 2:00 PM  → PAYMENT DUE

After Due (Overdue):
├─ Jan 16, 2:00 AM  → "Payment overdue by 12 hours: $100.00"
├─ Jan 16, 2:00 PM  → "Payment overdue by 24 hours: $100.00"
└─ Jan 17, 2:00 PM  → "Payment overdue by 48 hours: $100.00"
```

**Why This Schedule?**

This is a streamlined approach that:
- Provides early warning (24 hours), mid-day reminder (6 hours), and final alert (1 hour)
- Balances user awareness without overwhelming them with notifications
- Gives users time to make payment without feeling harassed
- Escalates gradually for overdue payments (12h, 24h, 48h)

**Technical Implementation:**

The `SendRemindersJob` runs hourly via Hangfire and checks for calendar items that fall within these time windows. The job queries unpaid items where:

```csharp
// Before due notifications
DateTime.UtcNow >= item.PaymentDue.AddHours(-24) && DateTime.UtcNow < item.PaymentDue.AddHours(-23)
DateTime.UtcNow >= item.PaymentDue.AddHours(-6) && DateTime.UtcNow < item.PaymentDue.AddHours(-5)
DateTime.UtcNow >= item.PaymentDue.AddHours(-1) && DateTime.UtcNow < item.PaymentDue

// After due notifications
DateTime.UtcNow >= item.PaymentDue.AddHours(12) && DateTime.UtcNow < item.PaymentDue.AddHours(13)
DateTime.UtcNow >= item.PaymentDue.AddHours(24) && DateTime.UtcNow < item.PaymentDue.AddHours(25)
DateTime.UtcNow >= item.PaymentDue.AddHours(48) && DateTime.UtcNow < item.PaymentDue.AddHours(49)
```

---

## Prerequisites

### Requirements

- [ ] Google account
- [ ] .NET 8 SDK installed
- [ ] Access to Firebase Console
- [ ] SQL Server running
- [ ] Android Studio (for Kotlin app)
- [ ] Xcode (for Swift app)

### Estimated Time

- Firebase setup: 30 minutes
- Backend implementation: 2 hours
- Frontend integration: 1 hour
- Testing: 1 hour
- **Total**: ~4-5 hours

---

## Phase 1: Firebase Project Setup

### Step 1.1: Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click **"Add project"**
3. Enter project name: `AICalendar`
4. Disable Google Analytics (optional for now)
5. Click **"Create project"**

### Step 1.2: Add Your Apps to Firebase

#### For Android (Kotlin) App

1. In Firebase Console, click **"Add app"** → Select **Android** icon
2. **Android package name**: `com.aicalendar.app` (must match your `applicationId` in `build.gradle`)
3. **App nickname**: `AICalendar Android`
4. Download `google-services.json`
5. Save to: `app/google-services.json` (in your Android project root)

#### For iOS (Swift) App

1. Click **"Add app"** → Select **iOS** icon
2. **iOS bundle ID**: `com.aicalendar.app` (must match your Xcode bundle identifier)
3. **App nickname**: `AICalendar iOS`
4. Download `GoogleService-Info.plist`
5. In Xcode, drag `GoogleService-Info.plist` into your project root (make sure "Copy items if needed" is checked)

### Step 1.3: Enable Cloud Messaging

1. In Firebase Console, go to **Project Settings** (gear icon)
2. Click **"Cloud Messaging"** tab
3. Scroll to **"Cloud Messaging API (Legacy)"**
4. If disabled, click **"Enable"**
5. Note down the **Server Key** (you won't use this, but good to know)


### Step 1.4: Generate Service Account Key

**CRITICAL STEP** - This allows your backend to send notifications

1. In Firebase Console, go to **Project Settings** → **Service accounts**
2. Click **"Generate new private key"**
3. Click **"Generate key"** (JSON file downloads)


4. **Rename** the file to: `firebase-credentials.json`
5. **Move** to: `src/AICalendar.API/firebase-credentials.json`

⚠️ **SECURITY WARNING**: Never commit this file to Git!

6. Add to `.gitignore`:
   ```
   # Firebase credentials
   **/firebase-credentials.json
   ```

---

## Phase 2: Backend Implementation

### Step 2.1: Install NuGet Package

```bash
cd src/AICalendar.API
dotnet add package FirebaseAdmin --version 3.0.0
```

### Step 2.2: Create Interfaces

**File**: `src/AICalendar.Application/Common/Interfaces/INotificationService.cs`

```csharp
using AICalendar.Domain.ValueObjects;

namespace AICalendar.Application.Common.Interfaces;

/// <summary>
/// Service for sending push notifications to users
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a push notification to a specific user
    /// </summary>
    /// <param name="userId">The user to notify</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message body</param>
    /// <param name="data">Optional additional data</param>
    Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null
    );

    /// <summary>
    /// Sends push notifications to multiple users
    /// </summary>
    Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications
    );
}
```

### Step 2.3: Create Firebase Service Implementation

**File**: `src/AICalendar.Infrastructure/Services/FirebaseNotificationService.cs`

```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Services;

public class FirebaseNotificationService : INotificationService
{
    private readonly ILogger<FirebaseNotificationService> _logger;
    private readonly IUserRepository _userRepository;

    public FirebaseNotificationService(
        ILogger<FirebaseNotificationService> logger,
        IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null)
    {
        try
        {
            // Get user's FCM device token
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning(
                    "User {UserId} not found. Cannot send notification.",
                    userId.Value
                );
                return;
            }

            if (string.IsNullOrEmpty(user.FcmDeviceToken))
            {
                _logger.LogWarning(
                    "User {UserId} has no FCM device token. Cannot send notification.",
                    userId.Value
                );
                return;
            }

            // Build notification message
            var fcmMessage = new Message
            {
                Token = user.FcmDeviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = message
                },
                Data = data ?? new Dictionary<string, string>
                {
                    { "userId", userId.Value.ToString() },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                },
                // Android specific settings
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Icon = "notification_icon",
                        Color = "#FF5722", // Orange color
                        Sound = "default",
                        ChannelId = "payment_reminders"
                    }
                },
                // iOS specific settings
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = title,
                            Body = message
                        },
                        Sound = "default",
                        Badge = 1
                    }
                },
                // Web push settings
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = title,
                        Body = message,
                        Icon = "/icon-192x192.png"
                    }
                }
            };

            // Send notification
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);

            _logger.LogInformation(
                "Successfully sent notification to user {UserId}. FCM Response: {Response}",
                userId.Value,
                response
            );
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex,
                "Firebase error sending notification to user {UserId}. Error code: {ErrorCode}",
                userId.Value,
                ex.MessagingErrorCode
            );

            // Handle invalid token (user uninstalled app or token expired)
            if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                _logger.LogWarning(
                    "Invalid FCM token for user {UserId}. Clearing token from database.",
                    userId.Value
                );

                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.ClearDeviceToken();
                    await _userRepository.UpdateAsync(user);
                }
            }
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
        var tasks = notifications.Select(n =>
            SendPushNotificationAsync(n.userId, n.title, n.message)
        );

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Sent {Count} bulk notifications",
            notifications.Count
        );
    }
}
```

### Step 2.4: Initialize Firebase in Program.cs

**File**: `src/AICalendar.API/Program.cs`

Add these using statements at the top:
```csharp
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
```

Add Firebase initialization before `builder.Build()`:
```csharp
// ═══════════════════════════════════════════════════════════
// Firebase Cloud Messaging Configuration
// ═══════════════════════════════════════════════════════════
var firebaseCredentialsPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "firebase-credentials.json"
);

if (File.Exists(firebaseCredentialsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
    });

    builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();

    Console.WriteLine("✅ Firebase Cloud Messaging initialized");
}
else
{
    Console.WriteLine("⚠️  WARNING: firebase-credentials.json not found. Notifications disabled.");

    // Register a null/dummy service for development
    builder.Services.AddScoped<INotificationService, NullNotificationService>();
}
```

### Step 2.5: Implement SendRemindersJob

**File**: `src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs`

The job is already registered with Hangfire but needs implementation. Here's what it should do:

```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.BackgroundJobs;

/// <summary>
/// Hangfire job that sends reminders for upcoming calendar items
/// Runs every hour
/// </summary>
public class SendRemindersJob
{
    private readonly ILogger<SendRemindersJob> _logger;
    private readonly ICalendarRepository _calendarRepository;
    private readonly INotificationService _notificationService;

    public SendRemindersJob(
        ILogger<SendRemindersJob> logger,
        ICalendarRepository calendarRepository,
        INotificationService notificationService)
    {
        _logger = logger;
        _calendarRepository = calendarRepository;
        _notificationService = notificationService;
    }

    public async Task SendDueReminders()
    {
        _logger.LogInformation("Starting reminder processing at {Time}", DateTime.UtcNow);

        try
        {
            // Get all unpaid calendar items with payment due dates
            var unpaidItems = await _calendarRepository.GetUnpaidItemsWithDueDatesAsync();

            if (!unpaidItems.Any())
            {
                _logger.LogInformation("No unpaid items found. Skipping reminder processing.");
                return;
            }

            _logger.LogInformation("Found {Count} unpaid items to check", unpaidItems.Count);

            var notificationsSent = 0;
            var now = DateTime.UtcNow;

            foreach (var item in unpaidItems)
            {
                var paymentDue = item.PaymentDue!.Value; // Already filtered to non-null

                // Check each time window and send notification if matched
                string? notificationMessage = null;

                // BEFORE DUE NOTIFICATIONS
                if (now >= paymentDue.AddHours(-24) && now < paymentDue.AddHours(-23))
                {
                    notificationMessage = $"Payment Due Soon: {item.Title} - ${item.Amount:F2} due in 24 hours ({paymentDue:MMM dd, h:mm tt})";
                }
                else if (now >= paymentDue.AddHours(-6) && now < paymentDue.AddHours(-5))
                {
                    notificationMessage = $"Payment Due Soon: {item.Title} - ${item.Amount:F2} due in 6 hours ({paymentDue:h:mm tt})";
                }
                else if (now >= paymentDue.AddHours(-1) && now < paymentDue)
                {
                    notificationMessage = $"Payment Due Soon: {item.Title} - ${item.Amount:F2} due in 1 hour ({paymentDue:h:mm tt})";
                }
                // AFTER DUE NOTIFICATIONS (OVERDUE)
                else if (now >= paymentDue.AddHours(12) && now < paymentDue.AddHours(13))
                {
                    notificationMessage = $"Payment Overdue: {item.Title} - ${item.Amount:F2} was due 12 hours ago";
                }
                else if (now >= paymentDue.AddHours(24) && now < paymentDue.AddHours(25))
                {
                    notificationMessage = $"Payment Overdue: {item.Title} - ${item.Amount:F2} was due 24 hours ago";
                }
                else if (now >= paymentDue.AddHours(48) && now < paymentDue.AddHours(49))
                {
                    notificationMessage = $"Payment Overdue: {item.Title} - ${item.Amount:F2} was due 48 hours ago";
                }

                // Send notification if a time window matched
                if (!string.IsNullOrEmpty(notificationMessage))
                {
                    await _notificationService.SendPushNotificationAsync(
                        userId: item.UserId,
                        title: "AICalendar Reminder",
                        message: notificationMessage,
                        data: new Dictionary<string, string>
                        {
                            { "calendarItemId", item.Id.Value.ToString() },
                            { "type", "payment_reminder" }
                        }
                    );

                    notificationsSent++;

                    _logger.LogInformation(
                        "Sent reminder for item {ItemId} to user {UserId}: {Message}",
                        item.Id.Value,
                        item.UserId.Value,
                        notificationMessage
                    );
                }
            }

            _logger.LogInformation(
                "Reminder processing completed. Sent {SentCount} notifications out of {TotalCount} unpaid items",
                notificationsSent,
                unpaidItems.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reminder processing");
            throw;
        }
    }
}
```

**Key Implementation Details:**

1. **Queries unpaid items**: Uses `GetUnpaidItemsWithDueDatesAsync()` to get all calendar items where `IsPaid = false` and `PaymentDue IS NOT NULL`

2. **Checks 6 time windows**: For each item, checks if current time falls within any of the notification windows:
   - 24 hours before (window: -24h to -23h)
   - 6 hours before (window: -6h to -5h)
   - 1 hour before (window: -1h to due time)
   - 12 hours after (window: +12h to +13h)
   - 24 hours after (window: +24h to +25h)
   - 48 hours after (window: +48h to +49h)

3. **Sends targeted notifications**: Creates appropriate message based on which window matched

4. **Includes metadata**: Passes calendar item ID in notification data for deep linking

5. **Logs everything**: Tracks how many notifications sent vs total unpaid items

**Why 1-hour windows?**
- Job runs every hour
- Each window is 1 hour wide
- Guarantees we won't miss notifications
- Prevents duplicate notifications (same item won't match same window twice)

**What happens if item is paid?**
- Next job run won't include it in the query
- No notification sent
- Immediately stops reminder cycle

### Step 2.6: Create Null Notification Service (For Development)

**File**: `src/AICalendar.Infrastructure/Services/NullNotificationService.cs`

```csharp
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Services;

/// <summary>
/// Null implementation of notification service for development/testing
/// </summary>
public class NullNotificationService : INotificationService
{
    private readonly ILogger<NullNotificationService> _logger;

    public NullNotificationService(ILogger<NullNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendPushNotificationAsync(
        UserId userId,
        string title,
        string message,
        Dictionary<string, string>? data = null)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would send to user {UserId}: {Title} - {Message}",
            userId.Value,
            title,
            message
        );

        return Task.CompletedTask;
    }

    public Task SendBulkNotificationsAsync(
        List<(UserId userId, string title, string message)> notifications)
    {
        _logger.LogInformation(
            "[NULL NOTIFICATION] Would send {Count} notifications",
            notifications.Count
        );

        return Task.CompletedTask;
    }
}
```

---

## Phase 3: Database Changes

### Step 3.1: Add FCM Token to User Entity

**File**: `src/AICalendar.Domain/Entities/User.cs` (or wherever your User entity is)

```csharp
public class User : AggregateRoot<UserId>
{
    // Existing properties...
    public string Email { get; private set; }
    public string Name { get; private set; }

    // NEW: FCM device token
    public string? FcmDeviceToken { get; private set; }
    public DateTime? FcmTokenUpdatedAt { get; private set; }

    // NEW: Methods
    public void UpdateDeviceToken(string fcmToken)
    {
        FcmDeviceToken = fcmToken;
        FcmTokenUpdatedAt = DateTime.UtcNow;
    }

    public void ClearDeviceToken()
    {
        FcmDeviceToken = null;
        FcmTokenUpdatedAt = null;
    }
}
```

### Step 3.2: Create Migration

```bash
cd src/AICalendar.Infrastructure
dotnet ef migrations add AddFcmTokenToUser --startup-project ../AICalendar.API
```

### Step 3.3: Review and Apply Migration

```bash
# Review the migration file in Migrations folder

# Apply to database
dotnet ef database update --startup-project ../AICalendar.API
```

The migration should add:
```sql
ALTER TABLE Users
ADD FcmDeviceToken NVARCHAR(500) NULL,
    FcmTokenUpdatedAt DATETIME2 NULL;
```

### Step 3.4: Create User Repository Interface

**File**: Update `src/AICalendar.Domain/Interfaces/IUserRepository.cs`

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
}
```

### Step 3.5: Implement User Repository

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
}
```

### Step 3.6: Register Repository in Program.cs

**File**: `src/AICalendar.API/Program.cs`

```csharp
// Register User Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

---

## Phase 4: Frontend Integration

### Option A: Android (Kotlin) App

#### Step 4A.1: Add Firebase Dependencies

**File**: `build.gradle` (Project level)
```gradle
buildscript {
    dependencies {
        classpath 'com.google.gms:google-services:4.4.0'
    }
}
```

**File**: `build.gradle` (App level)
```gradle
plugins {
    id 'com.android.application'
    id 'org.jetbrains.kotlin.android'
    id 'com.google.gms.google-services' // ADD THIS
}

dependencies {
    // Firebase BOM
    implementation platform('com.google.firebase:firebase-bom:32.7.0')

    // Firebase Cloud Messaging
    implementation 'com.google.firebase:firebase-messaging-ktx'

    // Coroutines for async operations
    implementation 'org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3'

    // Retrofit for API calls (if not already added)
    implementation 'com.squareup.retrofit2:retrofit:2.9.0'
    implementation 'com.squareup.retrofit2:converter-gson:2.9.0'
}
```

#### Step 4A.2: Update AndroidManifest.xml

**File**: `app/src/main/AndroidManifest.xml`
```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" /> <!-- Android 13+ -->

    <application
        android:name=".AICalendarApplication"
        ...>

        <!-- Default notification channel -->
        <meta-data
            android:name="com.google.firebase.messaging.default_notification_channel_id"
            android:value="payment_reminders" />

        <!-- Firebase Messaging Service -->
        <service
            android:name=".notifications.FCMService"
            android:exported="false">
            <intent-filter>
                <action android:name="com.google.firebase.MESSAGING_EVENT" />
            </intent-filter>
        </service>

    </application>
</manifest>
```

#### Step 4A.3: Create Firebase Messaging Service

**File**: `app/src/main/java/com/aicalendar/app/notifications/FCMService.kt`
```kotlin
package com.aicalendar.app.notifications

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import com.aicalendar.app.MainActivity
import com.aicalendar.app.R
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage

class FCMService : FirebaseMessagingService() {

    companion object {
        private const val TAG = "FCMService"
        private const val CHANNEL_ID = "payment_reminders"
        private const val CHANNEL_NAME = "Payment Reminders"
    }

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        Log.d(TAG, "New FCM token: $token")

        // Send token to your backend
        sendTokenToServer(token)
    }

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)

        Log.d(TAG, "Message received from: ${message.from}")

        // Check if message contains notification payload
        message.notification?.let { notification ->
            showNotification(
                title = notification.title ?: "AICalendar",
                body = notification.body ?: "",
                data = message.data
            )
        }

        // Check if message contains data payload
        if (message.data.isNotEmpty()) {
            Log.d(TAG, "Message data: ${message.data}")
            handleDataPayload(message.data)
        }
    }

    private fun showNotification(title: String, body: String, data: Map<String, String>) {
        createNotificationChannel()

        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            // Add data to intent
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

        val notificationBuilder = NotificationCompat.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_notification) // Add your icon
            .setContentTitle(title)
            .setContentText(body)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)

        val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        notificationManager.notify(System.currentTimeMillis().toInt(), notificationBuilder.build())
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                CHANNEL_NAME,
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                description = "Channel for payment reminder notifications"
            }

            val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            notificationManager.createNotificationChannel(channel)
        }
    }

    private fun sendTokenToServer(token: String) {
        // Get userId from SharedPreferences or your auth manager
        val userId = getUserId() ?: return

        // Use your API service to send token
        NotificationRepository.registerDeviceToken(userId, token)
    }

    private fun handleDataPayload(data: Map<String, String>) {
        // Handle custom data payload
        val userId = data["userId"]
        val timestamp = data["timestamp"]

        Log.d(TAG, "Data - userId: $userId, timestamp: $timestamp")
    }

    private fun getUserId(): String? {
        val sharedPrefs = getSharedPreferences("AICalendar", Context.MODE_PRIVATE)
        return sharedPrefs.getString("user_id", null)
    }
}
```

#### Step 4A.4: Create Notification Manager

**File**: `app/src/main/java/com/aicalendar/app/notifications/NotificationManager.kt`
```kotlin
package com.aicalendar.app.notifications

import android.Manifest
import android.app.Activity
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.util.Log
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.google.android.gms.tasks.OnCompleteListener
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class NotificationManager(private val context: Context) {

    companion object {
        private const val TAG = "NotificationManager"
        const val NOTIFICATION_PERMISSION_REQUEST_CODE = 1001
    }

    fun initialize(userId: String) {
        // Request notification permission for Android 13+
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            requestNotificationPermission()
        }

        // Get FCM token
        getFCMToken(userId)
    }

    fun requestNotificationPermission() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (ContextCompat.checkSelfPermission(
                    context,
                    Manifest.permission.POST_NOTIFICATIONS
                ) != PackageManager.PERMISSION_GRANTED
            ) {
                ActivityCompat.requestPermissions(
                    context as Activity,
                    arrayOf(Manifest.permission.POST_NOTIFICATIONS),
                    NOTIFICATION_PERMISSION_REQUEST_CODE
                )
            }
        }
    }

    private fun getFCMToken(userId: String) {
        FirebaseMessaging.getInstance().token.addOnCompleteListener(OnCompleteListener { task ->
            if (!task.isSuccessful) {
                Log.w(TAG, "Fetching FCM token failed", task.exception)
                return@OnCompleteListener
            }

            // Get FCM token
            val token = task.result
            Log.d(TAG, "FCM Token: $token")

            // Send token to backend
            registerDeviceToken(userId, token)
        })
    }

    private fun registerDeviceToken(userId: String, fcmToken: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                NotificationRepository.registerDeviceToken(userId, fcmToken)
                Log.d(TAG, "Device token registered successfully")
            } catch (e: Exception) {
                Log.e(TAG, "Error registering device token", e)
            }
        }
    }
}
```

#### Step 4A.5: Create API Repository

**File**: `app/src/main/java/com/aicalendar/app/notifications/NotificationRepository.kt`
```kotlin
package com.aicalendar.app.notifications

import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.POST

data class RegisterDeviceRequest(
    val userId: String,
    val fcmToken: String
)

interface NotificationApiService {
    @POST("api/users/register-device")
    suspend fun registerDevice(@Body request: RegisterDeviceRequest)
}

object NotificationRepository {

    private const val BASE_URL = "https://your-api-url.com/"

    private val retrofit = Retrofit.Builder()
        .baseUrl(BASE_URL)
        .addConverterFactory(GsonConverterFactory.create())
        .build()

    private val apiService = retrofit.create(NotificationApiService::class.java)

    suspend fun registerDeviceToken(userId: String, fcmToken: String) {
        apiService.registerDevice(RegisterDeviceRequest(userId, fcmToken))
    }
}
```

#### Step 4A.6: Initialize in Application Class

**File**: `app/src/main/java/com/aicalendar/app/AICalendarApplication.kt`
```kotlin
package com.aicalendar.app

import android.app.Application
import com.google.firebase.FirebaseApp

class AICalendarApplication : Application() {

    override fun onCreate() {
        super.onCreate()

        // Initialize Firebase
        FirebaseApp.initializeApp(this)
    }
}
```

#### Step 4A.7: Initialize After Login

**File**: `app/src/main/java/com/aicalendar/app/LoginActivity.kt` (or wherever you handle login)
```kotlin
// After successful login
val userId = loginResponse.userId
val notificationManager = NotificationManager(this)
notificationManager.initialize(userId)

// Save userId to SharedPreferences
getSharedPreferences("AICalendar", Context.MODE_PRIVATE)
    .edit()
    .putString("user_id", userId)
    .apply()
```

---

### Option B: iOS (Swift) App

#### Step 4B.1: Install Firebase SDK

**File**: Add to your Podfile

```ruby
platform :ios, '13.0'
use_frameworks!

target 'AICalendar' do
  # Firebase pods
  pod 'Firebase/Core'
  pod 'Firebase/Messaging'
end
```

Then run:

```bash
pod install
```

**OR** using Swift Package Manager in Xcode:

1. File → Add Packages
2. Enter: `https://github.com/firebase/firebase-ios-sdk`
3. Select: FirebaseMessaging

#### Step 4B.2: Enable Push Notifications in Xcode

1. Open your project in Xcode
2. Select your project target
3. Go to **Signing & Capabilities**
4. Click **+ Capability**
5. Add **Push Notifications**
6. Add **Background Modes** → Check **Remote notifications**

#### Step 4B.3: Configure AppDelegate

**File**: `AppDelegate.swift`
```swift
import UIKit
import Firebase
import FirebaseMessaging
import UserNotifications

@main
class AppDelegate: UIResponder, UIApplicationDelegate, UNUserNotificationCenterDelegate, MessagingDelegate {

    func application(_ application: UIApplication,
                     didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {

        // Configure Firebase
        FirebaseApp.configure()

        // Set messaging delegate
        Messaging.messaging().delegate = self

        // Set notification delegate
        UNUserNotificationCenter.current().delegate = self

        // Request notification permissions
        requestNotificationPermissions()

        // Register for remote notifications
        application.registerForRemoteNotifications()

        return true
    }

    func requestNotificationPermissions() {
        let authOptions: UNAuthorizationOptions = [.alert, .badge, .sound]
        UNUserNotificationCenter.current().requestAuthorization(options: authOptions) { granted, error in
            if let error = error {
                print("Error requesting notification permissions: \(error)")
                return
            }

            if granted {
                print("Notification permission granted")
            } else {
                print("Notification permission denied")
            }
        }
    }

    // MARK: - FCM Token Management

    func messaging(_ messaging: Messaging, didReceiveRegistrationToken fcmToken: String?) {
        print("FCM Token: \(fcmToken ?? "")")

        // Send token to backend
        if let token = fcmToken {
            registerDeviceToken(token)
        }
    }

    func application(_ application: UIApplication,
                     didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data) {
        // Pass device token to Firebase
        Messaging.messaging().apnsToken = deviceToken
    }

    func application(_ application: UIApplication,
                     didFailToRegisterForRemoteNotificationsWithError error: Error) {
        print("Failed to register for remote notifications: \(error)")
    }

    // MARK: - Handle Notifications

    // Handle notification when app is in foreground
    func userNotificationCenter(_ center: UNUserNotificationCenter,
                                willPresent notification: UNNotification,
                                withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        let userInfo = notification.request.content.userInfo
        print("Foreground notification: \(userInfo)")

        // Show notification even when app is in foreground
        completionHandler([[.banner, .badge, .sound]])
    }

    // Handle notification tap
    func userNotificationCenter(_ center: UNUserNotificationCenter,
                                didReceive response: UNNotificationResponse,
                                withCompletionHandler completionHandler: @escaping () -> Void) {
        let userInfo = response.notification.request.content.userInfo
        print("Notification tapped: \(userInfo)")

        // Handle navigation based on notification data
        handleNotificationTap(userInfo)

        completionHandler()
    }

    // MARK: - Backend Communication

    func registerDeviceToken(_ fcmToken: String) {
        guard let userId = UserDefaults.standard.string(forKey: "user_id") else {
            print("No user ID found")
            return
        }

        NotificationService.shared.registerDeviceToken(userId: userId, fcmToken: fcmToken)
    }

    func handleNotificationTap(_ userInfo: [AnyHashable: Any]) {
        // Extract data from notification
        if let userId = userInfo["userId"] as? String {
            print("Navigate to user: \(userId)")
            // Implement your navigation logic here
        }
    }
}
```

#### Step 4B.4: Create Notification Service

**File**: `Services/NotificationService.swift`

```swift
import Foundation
import FirebaseMessaging

class NotificationService {

    static let shared = NotificationService()

    private let baseURL = "https://your-api-url.com"

    private init() {}

    func initialize(userId: String) {
        // Save user ID
        UserDefaults.standard.set(userId, forKey: "user_id")

        // Get FCM token
        Messaging.messaging().token { token, error in
            if let error = error {
                print("Error fetching FCM token: \(error)")
                return
            }

            if let token = token {
                print("FCM Token: \(token)")
                self.registerDeviceToken(userId: userId, fcmToken: token)
            }
        }
    }

    func registerDeviceToken(userId: String, fcmToken: String) {
        guard let url = URL(string: "\(baseURL)/api/users/register-device") else {
            print("Invalid URL")
            return
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let body: [String: Any] = [
            "userId": userId,
            "fcmToken": fcmToken
        ]

        do {
            request.httpBody = try JSONSerialization.data(withJSONObject: body)
        } catch {
            print("Error serializing JSON: \(error)")
            return
        }

        URLSession.shared.dataTask(with: request) { data, response, error in
            if let error = error {
                print("Error registering device token: \(error)")
                return
            }

            if let httpResponse = response as? HTTPURLResponse {
                if httpResponse.statusCode == 200 {
                    print("Device token registered successfully")
                } else {
                    print("Failed to register device token: \(httpResponse.statusCode)")
                }
            }
        }.resume()
    }
}
```

#### Step 4B.5: Initialize After Login

**File**: `ViewControllers/LoginViewController.swift` (or wherever you handle login)

```swift
// After successful login
let userId = loginResponse.userId
NotificationService.shared.initialize(userId: userId)
```

#### Step 4B.6: Handle Notification Permissions

**File**: `Helpers/NotificationPermissionHelper.swift`

```swift
import UserNotifications

class NotificationPermissionHelper {

    static func checkPermissionStatus(completion: @escaping (Bool) -> Void) {
        UNUserNotificationCenter.current().getNotificationSettings { settings in
            DispatchQueue.main.async {
                completion(settings.authorizationStatus == .authorized)
            }
        }
    }

    static func requestPermission(completion: @escaping (Bool) -> Void) {
        let authOptions: UNAuthorizationOptions = [.alert, .badge, .sound]
        UNUserNotificationCenter.current().requestAuthorization(options: authOptions) { granted, error in
            DispatchQueue.main.async {
                completion(granted)
            }
        }
    }
}
```

#### Step 4B.7: Upload APNs Certificate to Firebase

1. Go to [Apple Developer Portal](https://developer.apple.com/account)
2. Navigate to **Certificates, Identifiers & Profiles**
3. Create an **APNs Authentication Key** (recommended) or **APNs Certificate**
4. Download the key/certificate
5. In Firebase Console:
   - Go to **Project Settings** → **Cloud Messaging** → **iOS app configuration**
   - Upload your APNs key or certificate

---

## Phase 5: Testing

### Step 5.1: Create API Endpoint for Device Registration

**File**: `src/AICalendar.API/Controllers/UsersController.cs`

```csharp
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Registers or updates a user's FCM device token for push notifications
    /// </summary>
    [HttpPost("register-device")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterDeviceRequest request)
    {
        var user = await _userRepository.GetByIdAsync(
            UserId.Create(request.UserId)
        );

        if (user == null)
        {
            return NotFound($"User {request.UserId} not found");
        }

        user.UpdateDeviceToken(request.FcmToken);
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Registered FCM token for user {UserId}",
            request.UserId
        );

        return Ok(new { message = "Device registered successfully" });
    }

    /// <summary>
    /// Test endpoint to send a notification to a user
    /// </summary>
    [HttpPost("test-notification/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestNotification(
        Guid userId,
        [FromServices] INotificationService notificationService)
    {
        await notificationService.SendPushNotificationAsync(
            UserId.Create(userId),
            "Test Notification",
            "This is a test notification from AICalendar"
        );

        return Ok(new { message = "Test notification sent" });
    }
}

public record RegisterDeviceRequest(Guid UserId, string FcmToken);
```

### Step 5.2: Test Notification Flow

1. **Start your backend**:
   ```bash
   cd src/AICalendar.API
   dotnet run
   ```

2. **Run your frontend app** and login

3. **Verify token registration**:
   - Check logs for "FCM Token: ..."
   - Check database: `SELECT FcmDeviceToken FROM Users WHERE Id = '...'`

4. **Send test notification**:
   ```bash
   curl -X POST https://localhost:7000/api/users/test-notification/YOUR_USER_GUID
   ```

5. **Verify notification received**:
   - Check your mobile device or browser
   - Should see "Test Notification"

### Step 5.3: Test Reminder Notification

1. **Create a calendar item with due date 24 hours from now**

2. **Manually trigger SendRemindersJob**:
   - Go to Hangfire Dashboard: `https://localhost:7000/hangfire`
   - Navigate to "Recurring jobs"
   - Find "send-reminders"
   - Click "Trigger now"

3. **Check logs**:
   ```
   Successfully sent notification to user {UserId}. FCM Response: ...
   ```

4. **Verify notification received** on your device

---

## Phase 6: Production Deployment

### Step 6.1: Environment Configuration

**File**: `src/AICalendar.API/appsettings.Production.json`

```json
{
  "Firebase": {
    "CredentialsPath": "/app/secrets/firebase-credentials.json"
  }
}
```

### Step 6.2: Secure Firebase Credentials

**DO NOT** commit `firebase-credentials.json` to Git!

**Options for production**:

#### Option A: Environment Variable
```bash
export GOOGLE_APPLICATION_CREDENTIALS=/path/to/firebase-credentials.json
```

#### Option B: Azure Key Vault
```csharp
var credential = builder.Configuration["Firebase:Credentials"];
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromJson(credential)
});
```

#### Option C: Docker Secrets
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .

# Copy credentials at build time (not recommended)
# OR mount as volume at runtime (better)
VOLUME /app/secrets

ENTRYPOINT ["dotnet", "AICalendar.API.dll"]
```

```bash
docker run -v /secure/path/firebase-credentials.json:/app/secrets/firebase-credentials.json ...
```

### Step 6.3: Update Hangfire Job Schedule

**File**: Update `src/AICalendar.Infrastructure/BackgroundJobs/HangfireConfiguration.cs`

Verify reminder job is configured:
```csharp
recurringJobManager.AddOrUpdate<SendRemindersJob>(
    "send-reminders",
    job => job.SendDueReminders(),
    Cron.Hourly(), // Runs every hour
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);
```

### Step 6.4: Monitoring and Logging

Add Application Insights or similar:

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

Track notification metrics:
- Total notifications sent
- Failed notifications
- Invalid tokens
- Response times

---

## Troubleshooting

### Issue 1: "Firebase credentials not found"

**Solution**:
```bash
# Verify file exists
ls -la src/AICalendar.API/firebase-credentials.json

# Check file permissions
chmod 644 src/AICalendar.API/firebase-credentials.json
```

### Issue 2: "User has no FCM token"

**Solution**:
- Frontend not sending token to backend
- Check browser console / mobile logs
- Verify API endpoint is being called
- Check database for token

### Issue 3: "Notifications not received"

**Checklist**:
- [ ] Firebase project created
- [ ] `google-services.json` / `GoogleService-Info.plist` added
- [ ] Permissions granted on device
- [ ] Token registered in database
- [ ] SendRemindersJob is running (check Hangfire)
- [ ] Check Firebase Console → Cloud Messaging → Usage

### Issue 4: "Invalid FCM token"

**Solution**:
- Token expired or user uninstalled app
- Service automatically clears invalid tokens
- User needs to re-login to get new token

### Issue 5: "Android notifications not showing"

**Solution**:

- Verify notification channel is created (already in FCMService)
- Check notification permission granted (Android 13+)
- Verify `google-services.json` is in correct location
- Check logcat for FCM errors: `adb logcat | grep FCM`
- Ensure app is not in battery optimization/doze mode

### Issue 6: "iOS notifications not showing"

**Solution**:

- Enable Push Notifications capability in Xcode
- Verify APNs certificate/key uploaded to Firebase Console
- Check device is not in Do Not Disturb mode
- Test on physical device (push notifications don't work on simulator)
- Check Xcode console for registration errors

### Issue 7: "Kotlin app crashes on FCM initialization"

**Solution**:

- Verify `google-services.json` exists in `app/` folder
- Check Gradle plugin is applied: `apply plugin: 'com.google.gms.google-services'`
- Sync Gradle files
- Clean and rebuild project

### Issue 8: "Swift app not receiving APNs token"

**Solution**:

- Verify app is signed with correct provisioning profile
- Check APNs entitlements are enabled
- Test on physical device (not simulator)
- Check for errors in `didFailToRegisterForRemoteNotificationsWithError`

---

## Additional Resources

### Documentation

- [Firebase Cloud Messaging Docs](https://firebase.google.com/docs/cloud-messaging)
- [Firebase Admin .NET SDK](https://firebase.google.com/docs/admin/setup#dotnet)
- [Firebase Cloud Messaging for Android](https://firebase.google.com/docs/cloud-messaging/android/client)
- [Firebase Cloud Messaging for iOS](https://firebase.google.com/docs/cloud-messaging/ios/client)
- [Kotlin Coroutines Guide](https://kotlinlang.org/docs/coroutines-guide.html)
- [Swift URLSession Documentation](https://developer.apple.com/documentation/foundation/urlsession)
- [APNs Overview](https://developer.apple.com/documentation/usernotifications)

### Testing Tools

- [Firebase Console](https://console.firebase.google.com/)
- [Hangfire Dashboard](http://localhost:5000/hangfire)
- [Postman Collection](#) (create for testing)

### Support

- GitHub Issues: [Your repo]
- Firebase Support: [Firebase Console]
- Stack Overflow: Tag with `firebase-cloud-messaging`

---

## Next Steps

After successful implementation:

1. ✅ Test with real calendar items
2. ✅ Monitor notification delivery rates
3. ✅ Set up analytics to track user engagement
4. ✅ Implement notification preferences (let users control frequency)
5. ✅ Add deep linking (notification tap → specific calendar item)
6. ✅ Implement notification history (show past notifications in app)

---

## Summary Checklist

### Backend
- [ ] Firebase project created
- [ ] Service account key downloaded
- [ ] FirebaseAdmin package installed
- [ ] Firebase initialized in Program.cs
- [ ] INotificationService interface created
- [ ] FirebaseNotificationService implemented
- [ ] User entity updated with FCM token
- [ ] Database migration applied
- [ ] UserRepository created
- [ ] API endpoints for device registration created

### Android (Kotlin) Frontend

- [ ] Firebase SDK added to Gradle
- [ ] `google-services.json` added to app folder
- [ ] Google Services plugin applied
- [ ] FCMService created and registered in manifest
- [ ] Notification channel created
- [ ] Permissions requested (Android 13+)
- [ ] FCM token retrieved and sent to backend
- [ ] NotificationManager initialized after login

### iOS (Swift) Frontend

- [ ] Firebase SDK installed via CocoaPods or SPM
- [ ] `GoogleService-Info.plist` added to Xcode project
- [ ] Push Notifications capability enabled
- [ ] Background Modes (Remote notifications) enabled
- [ ] AppDelegate configured with FCM
- [ ] Permissions requested
- [ ] APNs certificate/key uploaded to Firebase
- [ ] FCM token retrieved and sent to backend
- [ ] NotificationService initialized after login

### Testing

- [ ] Device token registered successfully
- [ ] Test notification received
- [ ] Reminder notification triggered and received
- [ ] Background notifications working
- [ ] Foreground notifications working
- [ ] Notification tap navigation working

### Production

- [ ] Firebase credentials secured
- [ ] Environment configuration set
- [ ] Monitoring enabled
- [ ] Logging configured
- [ ] Error handling tested
- [ ] Scaling considerations addressed

---

**🎉 Congratulations!** You now have a complete Firebase Cloud Messaging implementation for your AICalendar project!
