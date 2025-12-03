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
- **Frontend**: KOTLIN/SWIFT + Firebase SDK
- **Database**: SQL Server (add FCM token column)
- **Job Scheduler**: Hangfire (already configured)

---

## Prerequisites

### Requirements

- [ ] Google account
- [ ] .NET 8 SDK installed
- [ ] Access to Firebase Console
- [ ] SQL Server running
- [ ] Frontend app (Flutter or React)

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

#### For Flutter Mobile App

1. In Firebase Console, click **"Add app"** → Select **Android** icon
2. **Android package name**: `com.aicalendar.app` (or your package name)
3. Download `google-services.json`
4. Save to: `your-flutter-app/android/app/google-services.json`

5. Click **"Add app"** → Select **iOS** icon
6. **iOS bundle ID**: `com.aicalendar.app` (or your bundle ID)
7. Download `GoogleService-Info.plist`
8. Save to: `your-flutter-app/ios/Runner/GoogleService-Info.plist`

#### For React Web App

1. Click **"Add app"** → Select **Web** icon
2. App nickname: `AICalendar Web`
3. Copy the Firebase config object (you'll need this later)

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

### Step 2.5: Create Null Notification Service (For Development)

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

### Option A: Flutter Mobile App

#### Step 4A.1: Install Flutter Package

```yaml
# pubspec.yaml
dependencies:
  firebase_core: ^2.24.0
  firebase_messaging: ^14.7.0
  flutter_local_notifications: ^16.3.0
```

```bash
flutter pub get
```

#### Step 4A.2: Configure Android

**File**: `android/app/build.gradle`
```gradle
plugins {
    id "com.android.application"
    id "kotlin-android"
    id "dev.flutter.flutter-gradle-plugin"
    id "com.google.gms.google-services" // ADD THIS
}

dependencies {
    // ADD THIS
    implementation platform('com.google.firebase:firebase-bom:32.7.0')
    implementation 'com.google.firebase:firebase-messaging'
}
```

**File**: `android/build.gradle`
```gradle
buildscript {
    dependencies {
        // ADD THIS
        classpath 'com.google.gms:google-services:4.4.0'
    }
}
```

**File**: `android/app/src/main/AndroidManifest.xml`
```xml
<manifest>
    <application>
        <!-- ADD THIS -->
        <meta-data
            android:name="com.google.firebase.messaging.default_notification_channel_id"
            android:value="payment_reminders" />
    </application>
</manifest>
```

#### Step 4A.3: Configure iOS

**File**: `ios/Runner/AppDelegate.swift`
```swift
import UIKit
import Flutter
import Firebase // ADD THIS

@UIApplicationMain
@objc class AppDelegate: FlutterAppDelegate {
  override func application(
    _ application: UIApplication,
    didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
  ) -> Bool {
    FirebaseApp.configure() // ADD THIS
    GeneratedPluginRegistrant.register(with: self)
    return super.application(application, didFinishLaunchingWithOptions: launchOptions)
  }
}
```

#### Step 4A.4: Initialize Firebase in Flutter

**File**: `lib/main.dart`
```dart
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'firebase_options.dart';

// Background message handler
Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
  print("Background message: ${message.notification?.title}");
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize Firebase
  await Firebase.initializeApp(
    options: DefaultFirebaseOptions.currentPlatform,
  );

  // Register background handler
  FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);

  runApp(MyApp());
}
```

#### Step 4A.5: Request Permissions and Get Token

**File**: `lib/services/notification_service.dart`
```dart
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class NotificationService {
  final FirebaseMessaging _firebaseMessaging = FirebaseMessaging.instance;

  Future<void> initialize(String userId) async {
    // Request permission
    NotificationSettings settings = await _firebaseMessaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
    );

    if (settings.authorizationStatus == AuthorizationStatus.authorized) {
      print('User granted permission');

      // Get FCM token
      String? token = await _firebaseMessaging.getToken();
      print('FCM Token: $token');

      // Send token to backend
      if (token != null) {
        await _registerDeviceToken(userId, token);
      }

      // Listen for token refresh
      _firebaseMessaging.onTokenRefresh.listen((newToken) {
        _registerDeviceToken(userId, newToken);
      });

      // Handle foreground messages
      FirebaseMessaging.onMessage.listen((RemoteMessage message) {
        print('Foreground message: ${message.notification?.title}');
        _showLocalNotification(message);
      });

      // Handle notification tap
      FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
        print('Notification tapped: ${message.data}');
        _handleNotificationTap(message);
      });
    }
  }

  Future<void> _registerDeviceToken(String userId, String fcmToken) async {
    try {
      final response = await http.post(
        Uri.parse('https://your-api.com/api/users/register-device'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'userId': userId,
          'fcmToken': fcmToken,
        }),
      );

      if (response.statusCode == 200) {
        print('Device token registered successfully');
      }
    } catch (e) {
      print('Error registering device token: $e');
    }
  }

  void _showLocalNotification(RemoteMessage message) {
    // Implement local notification display
  }

  void _handleNotificationTap(RemoteMessage message) {
    // Navigate to relevant screen based on message data
  }
}
```

### Option B: React Web App

#### Step 4B.1: Install Dependencies

```bash
npm install firebase
```

#### Step 4B.2: Create Firebase Config

**File**: `src/firebase-config.js`
```javascript
import { initializeApp } from "firebase/app";
import { getMessaging, getToken, onMessage } from "firebase/messaging";

const firebaseConfig = {
  apiKey: "YOUR_API_KEY",
  authDomain: "aicalendar.firebaseapp.com",
  projectId: "aicalendar",
  storageBucket: "aicalendar.appspot.com",
  messagingSenderId: "YOUR_SENDER_ID",
  appId: "YOUR_APP_ID"
};

const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

export { messaging, getToken, onMessage };
```

#### Step 4B.3: Create Service Worker

**File**: `public/firebase-messaging-sw.js`
```javascript
importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-messaging-compat.js');

firebase.initializeApp({
  apiKey: "YOUR_API_KEY",
  authDomain: "aicalendar.firebaseapp.com",
  projectId: "aicalendar",
  storageBucket: "aicalendar.appspot.com",
  messagingSenderId: "YOUR_SENDER_ID",
  appId: "YOUR_APP_ID"
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
  console.log('Background message:', payload);

  const notificationTitle = payload.notification.title;
  const notificationOptions = {
    body: payload.notification.body,
    icon: '/icon-192x192.png'
  };

  self.registration.showNotification(notificationTitle, notificationOptions);
});
```

#### Step 4B.4: Request Permission and Get Token

**File**: `src/services/notificationService.js`
```javascript
import { messaging, getToken, onMessage } from '../firebase-config';

export const initializeNotifications = async (userId) => {
  try {
    // Request permission
    const permission = await Notification.requestPermission();

    if (permission === 'granted') {
      // Get FCM token
      const token = await getToken(messaging, {
        vapidKey: 'YOUR_VAPID_KEY' // Get from Firebase Console
      });

      console.log('FCM Token:', token);

      // Send to backend
      await registerDeviceToken(userId, token);

      // Listen for foreground messages
      onMessage(messaging, (payload) => {
        console.log('Foreground message:', payload);
        showNotification(payload);
      });
    }
  } catch (error) {
    console.error('Error initializing notifications:', error);
  }
};

const registerDeviceToken = async (userId, fcmToken) => {
  await fetch('/api/users/register-device', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId, fcmToken })
  });
};

const showNotification = (payload) => {
  new Notification(payload.notification.title, {
    body: payload.notification.body,
    icon: '/icon-192x192.png'
  });
};
```

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
- Create notification channel:
  ```dart
  const AndroidNotificationChannel channel = AndroidNotificationChannel(
    'payment_reminders',
    'Payment Reminders',
    importance: Importance.high,
  );
  ```

### Issue 6: "iOS notifications not showing"

**Solution**:
- Enable Push Notifications in Xcode
- Add Notification Service Extension
- Verify APNs certificate in Firebase Console

---

## Additional Resources

### Documentation

- [Firebase Cloud Messaging Docs](https://firebase.google.com/docs/cloud-messaging)
- [Firebase Admin .NET SDK](https://firebase.google.com/docs/admin/setup#dotnet)
- [Flutter Firebase Messaging](https://firebase.flutter.dev/docs/messaging/overview)
- [Firebase JS SDK](https://firebase.google.com/docs/web/setup)

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

### Frontend
- [ ] Firebase SDK installed
- [ ] Firebase initialized
- [ ] Permissions requested
- [ ] FCM token retrieved
- [ ] Token sent to backend
- [ ] Notification handlers implemented
- [ ] Background message handler configured

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
