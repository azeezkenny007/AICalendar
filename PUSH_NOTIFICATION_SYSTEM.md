# Push Notification System - Complete Guide

## Overview

This document provides a comprehensive explanation of how the push notification system works in the AICalendar project. The system uses **Firebase Cloud Messaging (FCM)** to deliver real-time notifications to users across Android, iOS, and Web platforms.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [System Components](#system-components)
3. [How It Works - Flow Diagram](#how-it-works---flow-diagram)
4. [Device Registration Process](#device-registration-process)
5. [Sending Notifications](#sending-notifications)
6. [Background Job Integration](#background-job-integration)
7. [API Endpoints](#api-endpoints)
8. [Configuration](#configuration)
9. [Error Handling](#error-handling)
10. [Platform-Specific Features](#platform-specific-features)
11. [Testing](#testing)
12. [Deployment Considerations](#deployment-considerations)

---

## Architecture Overview

The push notification system follows a clean architecture pattern with clear separation of concerns:

```
┌─────────────────┐
│   Mobile App    │
│  (iOS/Android)  │
│   or Web App    │
└────────┬────────┘
         │ 1. Register FCM Token
         ↓
┌─────────────────────────────────────────────┐
│         PushNotificationsController         │
│       (API Layer - Presentation)            │
└────────┬────────────────────────────────────┘
         │ 2. Send Command via MediatR
         ↓
┌─────────────────────────────────────────────┐
│      RegisterDeviceCommandHandler           │
│       (Application Layer - CQRS)            │
└────────┬────────────────────────────────────┘
         │ 3. Save to Database
         ↓
┌─────────────────────────────────────────────┐
│           User Entity (Domain)              │
│      FcmDeviceToken: string?                │
└─────────────────────────────────────────────┘

         When notification is triggered:

┌─────────────────────────────────────────────┐
│       SendRemindersJob (Background)         │
│         or Manual Trigger                   │
└────────┬────────────────────────────────────┘
         │ 4. Call Notification Service
         ↓
┌─────────────────────────────────────────────┐
│      FirebaseNotificationService            │
│      (Infrastructure Layer)                 │
└────────┬────────────────────────────────────┘
         │ 5. Send to FCM
         ↓
┌─────────────────────────────────────────────┐
│     Firebase Cloud Messaging (FCM)          │
└────────┬────────────────────────────────────┘
         │ 6. Deliver Notification
         ↓
┌─────────────────┐
│   User Device   │
│  (Notification) │
└─────────────────┘
```

---

## System Components

### 1. **Domain Layer** (`AICalendar.Domain`)
- **User Entity** - Stores the FCM device token
  - Location: `src/AICalendar.Domain/Entities/User.cs`
  - Properties:
    - `FcmDeviceToken`: Stores the Firebase device token
  - Methods:
    - `RegisterDeviceToken(string token)`: Registers a new device
    - `ClearDeviceToken()`: Removes the device token

### 2. **Application Layer** (`AICalendar.Application`)
- **INotificationService Interface** - Defines notification operations
  - Location: `src/AICalendar.Application/Common/Interfaces/INotificationService.cs`

- **Commands (CQRS Pattern)**:
  - `RegisterDeviceCommand` - Register a device token
  - `UnregisterDeviceCommand` - Remove a device token
  - `TestNotificationCommand` - Send a test notification

- **Command Handlers**:
  - `RegisterDeviceCommandHandler`
  - `UnregisterDeviceCommandHandler`
  - `TestNotificationCommandHandler`

### 3. **Infrastructure Layer** (`AICalendar.Infrastructure`)
- **FirebaseNotificationService** - Concrete implementation
  - Location: `src/AICalendar.Infrastructure/Services/FirebaseNotificationService.cs`
  - Responsible for:
    - Sending notifications via FCM
    - Handling platform-specific configurations (Android, iOS, Web)
    - Managing invalid tokens
    - Logging notification events

- **NullNotificationService** - Fallback when Firebase is not configured
  - Used in development when `firebase-credentials.json` is missing

### 4. **API Layer** (`AICalendar.API`)
- **PushNotificationsController** - REST API endpoints
  - Location: `src/AICalendar.API/Controllers/PushNotificationsController.cs`
  - Endpoints for device management and testing

- **Program.cs** - Firebase initialization
  - Location: `src/AICalendar.API/Program.cs`
  - Lines 175-209: Firebase setup and service registration

### 5. **Background Jobs**
- **SendRemindersJob** - Automated notification scheduler
  - Location: `src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs`
  - Runs hourly to send payment reminders

---

## How It Works - Flow Diagram

### Complete User Journey

```
┌─────────────────────────────────────────────────────────┐
│                    STEP 1: APP STARTUP                  │
└─────────────────────────────────────────────────────────┘
                          │
                          ↓
        User opens mobile app for the first time
                          │
                          ↓
        App initializes Firebase SDK on the device
                          │
                          ↓
        Firebase SDK generates unique FCM Token
        (e.g., "fGcI7X8kRZuQ9...")
                          │
                          ↓
┌─────────────────────────────────────────────────────────┐
│              STEP 2: TOKEN REGISTRATION                 │
└─────────────────────────────────────────────────────────┘
                          │
                          ↓
        App sends POST request:
        POST /api/push-notifications/register-device
        {
          "userId": "user-guid",
          "fcmToken": "fGcI7X8kRZuQ9..."
        }
                          │
                          ↓
        Backend validates and stores token in database
        (User.FcmDeviceToken column)
                          │
                          ↓
        Returns success response
                          │
                          ↓
┌─────────────────────────────────────────────────────────┐
│           STEP 3: NOTIFICATION TRIGGER                  │
└─────────────────────────────────────────────────────────┘
                          │
                          ↓
        Two possible triggers:

        A) Scheduled Job (Automatic)
           - SendRemindersJob runs every hour
           - Finds payments due soon
           - Sends notifications to users

        B) Manual Trigger (On-demand)
           - Admin/System triggers notification
           - Test notification via API
                          │
                          ↓
┌─────────────────────────────────────────────────────────┐
│         STEP 4: FIREBASE NOTIFICATION SERVICE           │
└─────────────────────────────────────────────────────────┘
                          │
                          ↓
        1. Fetch user from database by UserId
                          │
                          ↓
        2. Retrieve user's FcmDeviceToken
                          │
                          ↓
        3. Build FCM Message with:
           - Notification title and body
           - Platform-specific config (Android/iOS/Web)
           - Custom data payload
                          │
                          ↓
        4. Call FirebaseMessaging.DefaultInstance.SendAsync()
                          │
                          ↓
┌─────────────────────────────────────────────────────────┐
│              STEP 5: FCM CLOUD DELIVERY                 │
└─────────────────────────────────────────────────────────┘
                          │
                          ↓
        Firebase Cloud Messaging (Google's servers)
        routes notification to target device
                          │
                          ↓
        Device receives notification via:
        - APNS (Apple Push Notification Service) for iOS
        - FCM for Android
        - Web Push for browsers
                          │
                          ↓
┌─────────────────────────────────────────────────────────┐
│            STEP 6: USER RECEIVES NOTIFICATION           │
└─────────────────────────────────────────────────────────┘
                          │
                          ↓
        Notification appears on device:

        ┌─────────────────────────────┐
        │  📱 Payment Reminder         │
        │  Your rent payment is due   │
        │  in 2 days ($1,200.00)      │
        └─────────────────────────────┘
                          │
                          ↓
        User taps notification → App opens to payment details
```

---

## Device Registration Process

### Client-Side Implementation (Mobile/Web App)

```javascript
// Example: React Native or Web App
import messaging from '@react-native-firebase/messaging';

// 1. Request permission (iOS)
async function requestUserPermission() {
  const authStatus = await messaging().requestPermission();
  const enabled =
    authStatus === messaging.AuthorizationStatus.AUTHORIZED ||
    authStatus === messaging.AuthorizationStatus.PROVISIONAL;

  if (enabled) {
    console.log('Authorization status:', authStatus);
    return true;
  }
  return false;
}

// 2. Get FCM Token
async function getFCMToken() {
  const fcmToken = await messaging().getToken();
  return fcmToken;
}

// 3. Register with Backend
async function registerDevice(userId) {
  const hasPermission = await requestUserPermission();

  if (!hasPermission) {
    console.log('Push notification permission denied');
    return;
  }

  const fcmToken = await getFCMToken();

  // Send to your backend
  const response = await fetch('https://your-api.com/api/push-notifications/register-device', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      userId: userId,
      fcmToken: fcmToken,
    }),
  });

  const result = await response.json();
  console.log('Device registered:', result);
}
```

### Backend Processing

When the API receives the registration request:

1. **Validation** - `RegisterDeviceCommandValidator` checks:
   - UserId is valid GUID
   - FcmToken is not empty

2. **Command Handler** - `RegisterDeviceCommandHandler`:
   ```csharp
   // Pseudo-code flow
   var user = await _userRepository.GetByIdAsync(command.UserId);

   if (user == null)
       return Error("User not found", 404);

   if (!string.IsNullOrEmpty(user.FcmDeviceToken))
       return Error("Device already registered", 409);

   user.RegisterDeviceToken(command.FcmToken);
   await _userRepository.UpdateAsync(user);

   return Success("Device registered successfully");
   ```

3. **Database Update** - Token stored in `Users` table:
   ```sql
   UPDATE Users
   SET FcmDeviceToken = 'fGcI7X8kRZuQ9...'
   WHERE Id = 'user-guid'
   ```

---

## Sending Notifications

### Method 1: Automatic (Background Job)

The `SendRemindersJob` runs hourly and sends notifications for upcoming payments:

```csharp
// Simplified flow from SendRemindersJob.cs
public async Task Execute(IJobCancellationToken token)
{
    // 1. Find calendars with payments due soon
    var upcomingCalendars = await _calendarRepository
        .GetUpcomingCalendarsAsync(DateTime.UtcNow.AddDays(2));

    foreach (var calendar in upcomingCalendars)
    {
        // 2. Check if reminder already sent
        if (!calendar.ReminderSent)
        {
            // 3. Send push notification
            await _notificationService.SendPushNotificationAsync(
                calendar.UserId,
                title: "Payment Reminder",
                message: $"Your {calendar.ItemName} payment is due in 2 days",
                data: new Dictionary<string, string>
                {
                    { "calendarId", calendar.Id.ToString() },
                    { "dueDate", calendar.DueDate.ToString("O") }
                }
            );

            // 4. Mark reminder as sent
            calendar.MarkReminderSent();
            await _calendarRepository.UpdateAsync(calendar);
        }
    }
}
```

**Schedule**: Runs every hour (configured in `appsettings.json`)

### Method 2: Manual/On-Demand

Any part of the application can inject `INotificationService` and send notifications:

```csharp
public class PaymentController : ControllerBase
{
    private readonly INotificationService _notificationService;

    [HttpPost("notify-payment-received")]
    public async Task<IActionResult> NotifyPaymentReceived(Guid userId)
    {
        await _notificationService.SendPushNotificationAsync(
            UserId.Create(userId),
            title: "Payment Received",
            message: "Your payment has been successfully processed",
            data: new Dictionary<string, string>
            {
                { "type", "payment_confirmation" },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            }
        );

        return Ok();
    }
}
```

### Method 3: Bulk Notifications

For sending to multiple users at once:

```csharp
var notifications = new List<(UserId, string, string)>
{
    (userId1, "Title 1", "Message 1"),
    (userId2, "Title 2", "Message 2"),
    (userId3, "Title 3", "Message 3")
};

await _notificationService.SendBulkNotificationsAsync(notifications);
```

---

## Background Job Integration

### Hangfire Configuration

The system uses **Hangfire** to schedule recurring notification jobs:

**Configuration** (`appsettings.json`):
```json
{
  "BackgroundJobs": {
    "SendReminders": {
      "CronExpression": "0 * * * *",
      "Description": "Send push notifications for upcoming payments every hour"
    }
  }
}
```

**Cron Expression Breakdown**:
- `0 * * * *` = Every hour at minute 0
- Example: 12:00, 1:00, 2:00, 3:00, etc.

**Job Registration** (`HangfireConfiguration.cs`):
```csharp
recurringJobManager.AddOrUpdate<SendRemindersJob>(
    "send-reminders",
    job => job.Execute(JobCancellationToken.Null),
    cronExpression,
    TimeZoneInfo.Utc
);
```

---

## API Endpoints

### 1. Register Device

**Endpoint**: `POST /api/push-notifications/register-device`

**Purpose**: Register a user's FCM device token

**Request**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fcmToken": "fGcI7X8kRZuQ9yNxP7..."
}
```

**Response (Success - 200)**:
```json
{
  "message": "Device registered successfully"
}
```

**Response (Error - 404)**:
```json
{
  "message": "User not found"
}
```

**Response (Error - 409)**:
```json
{
  "message": "User already has a registered device. Unregister first."
}
```

### 2. Test Notification

**Endpoint**: `POST /api/push-notifications/test-notification/{userId}`

**Purpose**: Send a test notification to verify FCM is working

**Request**: No body required

**Response (Success - 200)**:
```json
{
  "message": "Test notification sent successfully"
}
```

**Response (Error - 400)**:
```json
{
  "message": "User has no registered device token"
}
```

### 3. Unregister Device

**Endpoint**: `POST /api/push-notifications/unregister-device/{userId}`

**Purpose**: Remove a user's FCM token (logout, opt-out)

**Request**: No body required

**Response (Success - 200)**:
```json
{
  "message": "Device unregistered successfully"
}
```

**Response (Error - 400)**:
```json
{
  "message": "User does not have a registered device"
}
```

---

## Configuration

### Firebase Setup

**1. Firebase Credentials File**

The system requires `firebase-credentials.json` containing your Firebase Admin SDK credentials:

**Local Development**:
- Path: `src/AICalendar.API/firebase-credentials.json`

**Docker/Render Deployment**:
- Path: `/etc/secrets/firebase-credentials.json`

**2. Firebase Initialization** (`Program.cs`):

```csharp
var isDocker = Environment.GetEnvironmentVariable("RENDER") == "true" &&
               Directory.Exists("/etc/secrets");

var firebaseCredentialsPath = isDocker
    ? Path.Combine("/etc/secrets", "firebase-credentials.json")
    : Path.Combine(builder.Environment.ContentRootPath, "firebase-credentials.json");

if (File.Exists(firebaseCredentialsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
    });

    builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();
    Log.Information("✅ Firebase Cloud Messaging initialized");
}
else
{
    Log.Warning("⚠️  WARNING: firebase-credentials.json not found. Notifications disabled.");
    builder.Services.AddScoped<INotificationService, NullNotificationService>();
}
```

**3. Get Firebase Credentials**:

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project
3. Go to **Project Settings** → **Service Accounts**
4. Click **Generate New Private Key**
5. Save the JSON file as `firebase-credentials.json`

---

## Error Handling

### Invalid/Expired Tokens

The `FirebaseNotificationService` automatically handles invalid tokens:

```csharp
catch (FirebaseMessagingException ex)
{
    if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
        ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
    {
        _logger.LogWarning(
            "Invalid FCM token for user {UserId}. Clearing token from database.",
            userId.Value
        );

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            user.ClearDeviceToken();
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}
```

**Scenarios**:
- User uninstalled the app
- Token expired (FCM tokens can expire)
- User revoked notification permissions
- Device was reset

**Action**: Token is automatically removed from database, preventing future failed attempts.

### Missing Token

If a user has no registered device token:

```csharp
if (string.IsNullOrEmpty(user.FcmDeviceToken))
{
    _logger.LogWarning(
        "User {UserId} has no FCM device token. Cannot send notification.",
        userId.Value
    );
    return; // Silently fail - not an error condition
}
```

**This is expected behavior** - not all users will have notifications enabled.

---

## Platform-Specific Features

### Android Configuration

```csharp
Android = new AndroidConfig
{
    Priority = Priority.High,
    Notification = new AndroidNotification
    {
        Icon = "notification_icon",      // App icon
        Color = "#FF5722",                // Orange badge color
        Sound = "default",                // Notification sound
        ChannelId = "payment_reminders"   // Android notification channel
    }
}
```

**Android Notification Channel** (Client-side setup required):
```kotlin
// Android app must create notification channel
val channel = NotificationChannel(
    "payment_reminders",
    "Payment Reminders",
    NotificationManager.IMPORTANCE_HIGH
)
notificationManager.createNotificationChannel(channel)
```

### iOS Configuration

```csharp
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
        Badge = 1                    // Badge count on app icon
    }
}
```

**iOS Requirements**:
- Apple Push Notification Service (APNS) certificate configured in Firebase
- User must grant notification permission
- App must be signed with proper entitlements

### Web Push Configuration

```csharp
Webpush = new WebpushConfig
{
    Notification = new WebpushNotification
    {
        Title = title,
        Body = message,
        Icon = "/icon-192x192.png"   // PWA icon
    }
}
```

**Web Requirements**:
- HTTPS (required for Web Push)
- Service Worker registered
- Firebase Web SDK initialized
- User grants browser notification permission

---

## Testing

### Step 1: Register a Test Device

```bash
curl -X POST "https://your-api.com/api/push-notifications/register-device" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "your-user-guid",
    "fcmToken": "your-fcm-token-from-device"
  }'
```

### Step 2: Send Test Notification

```bash
curl -X POST "https://your-api.com/api/push-notifications/test-notification/your-user-guid"
```

**Expected Result**:
- Device should receive notification with title "🔔 Test Notification"
- Message: "If you see this, notifications are working!"

### Step 3: Verify Logs

Check application logs for:
```
✅ Firebase Cloud Messaging initialized
Successfully sent notification to user {UserId}. FCM Response: {Response}
```

### Step 4: Test Automatic Reminders

1. Create a calendar entry with due date in 2 days
2. Wait for the next hour (SendRemindersJob runs)
3. Verify notification is received
4. Check that `ReminderSent` flag is set to `true` in database

---

## Deployment Considerations

### Environment Variables

For Render or Docker deployments:

```bash
# Set in Render Dashboard or docker-compose.yml
RENDER=true
```

### Secrets Management

**Never commit `firebase-credentials.json` to Git!**

**Local Development**:
- Add to `.gitignore`
- Store locally in `src/AICalendar.API/`

**Production (Render)**:
1. Go to Render Dashboard → Service → Environment
2. Add Secret File: `firebase-credentials.json`
3. Content: Paste your Firebase credentials JSON
4. Mount path: `/etc/secrets/firebase-credentials.json`

**Production (Docker)**:
```yaml
# docker-compose.yml
services:
  api:
    volumes:
      - ./firebase-credentials.json:/etc/secrets/firebase-credentials.json:ro
```

### Monitoring

**Key Metrics to Monitor**:
- Notification send success rate
- Invalid token rate
- Notification delivery latency
- Background job execution time

**Logging**:
All notification events are logged via Serilog:
- Successful sends
- Failed sends with error codes
- Invalid tokens detected
- Token cleanup operations

---

## Security Considerations

### 1. Token Protection
- FCM tokens should be treated as sensitive data
- Never expose tokens in API responses
- Store securely in database (consider encryption at rest)

### 2. User Consent
- Always request user permission before registering device
- Provide opt-out mechanism (unregister endpoint)
- Respect user notification preferences

### 3. Rate Limiting
- Implement rate limits on registration endpoints
- Prevent abuse of test notification endpoint
- Monitor for unusual notification patterns

### 4. Data Privacy
- Don't send sensitive information in notification body
- Use notification data payload for IDs only
- Fetch full details in app after notification tap

---

## Troubleshooting

### Notifications Not Received

**Check**:
1. ✅ Firebase credentials file exists and is valid
2. ✅ User has registered FCM token in database
3. ✅ Device has granted notification permissions
4. ✅ App is properly configured with Firebase SDK
5. ✅ Check logs for FCM errors

### "firebase-credentials.json not found"

**Solution**:
- Verify file exists at correct path
- Check file permissions (readable)
- Ensure file is valid JSON

### Invalid Token Errors

**Solution**:
- User should unregister and re-register device
- Old tokens expire - implement token refresh logic
- Check that client is using latest FCM SDK

### Notifications Delayed

**Possible Causes**:
- Background job schedule (runs hourly)
- Device in low-power mode
- Network connectivity issues
- FCM service latency

---

## Best Practices

### 1. Token Management
- Refresh tokens when they expire
- Remove tokens on user logout
- Handle token cleanup automatically

### 2. Notification Content
- Keep titles short and clear
- Use actionable language
- Include relevant context
- Test on multiple devices

### 3. Timing
- Respect user time zones
- Avoid notifications at night
- Space out bulk notifications

### 4. Testing
- Always test on real devices
- Test all platforms (Android, iOS, Web)
- Verify notification appearance
- Test tap actions

---

## Summary

The AICalendar push notification system provides:

✅ **Multi-platform support** - Android, iOS, Web
✅ **Automated reminders** - Hourly background job
✅ **Manual triggers** - Send notifications on-demand
✅ **Robust error handling** - Automatic token cleanup
✅ **Clean architecture** - Testable and maintainable
✅ **Production-ready** - Docker and cloud deployment support

**Key Files**:
- Service: [FirebaseNotificationService.cs](src/AICalendar.Infrastructure/Services/FirebaseNotificationService.cs)
- Controller: [PushNotificationsController.cs](src/AICalendar.API/Controllers/PushNotificationsController.cs)
- Background Job: [SendRemindersJob.cs](src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs)
- Configuration: [Program.cs](src/AICalendar.API/Program.cs) (lines 175-209)

For more details, see:
- [FIREBASE_NOTIFICATION_IMPLEMENTATION.md](docs/FIREBASE_NOTIFICATION_IMPLEMENTATION.md)
- [TESTING_FIREBASE_NOTIFICATIONS.md](docs/TESTING_FIREBASE_NOTIFICATIONS.md)
- [PUSH_NOTIFICATION_USER_GUIDE.md](docs/PUSH_NOTIFICATION_USER_GUIDE.md)

---

**Last Updated**: 2025-12-09
**Version**: 1.0
