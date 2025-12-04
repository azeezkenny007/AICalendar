# Push Notification User Guide

## Overview

This guide provides comprehensive documentation on how to use the AICalendar push notification system. The system uses Firebase Cloud Messaging (FCM) to send real-time notifications to users about upcoming payment reminders and calendar events.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [User Endpoints](#user-endpoints)
3. [Integration Flow](#integration-flow)
4. [Implementation Guide](#implementation-guide)
5. [Testing](#testing)
6. [Troubleshooting](#troubleshooting)
7. [Best Practices](#best-practices)

---

## Architecture Overview

### How It Works

The push notification system consists of several key components:

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│  Mobile Client  │────────▶│  AICalendar API  │────────▶│ Firebase Cloud  │
│   (iOS/Android) │         │                  │         │    Messaging    │
└─────────────────┘         └──────────────────┘         └─────────────────┘
        │                            │                            │
        │ 1. Get FCM Token          │                            │
        │◀───────────────────────────┘                            │
        │                                                         │
        │ 2. Register Token                                       │
        ├────────────────────────────▶│                           │
        │                             │ 3. Store Token           │
        │                             ├──────────▶┌─────────┐    │
        │                             │           │Database │    │
        │                             │           └─────────┘    │
        │                             │                          │
        │                             │ 4. Send Notification     │
        │                             ├─────────────────────────▶│
        │                             │                          │
        │ 5. Receive Notification     │                          │
        │◀─────────────────────────────────────────────────────────┘
```

### Components

1. **Mobile Client**: iOS or Android app that receives notifications
2. **Firebase SDK**: Client-side library that generates FCM tokens
3. **AICalendar API**: Backend service that manages tokens and sends notifications
4. **Firebase Cloud Messaging**: Google's notification delivery service
5. **Hangfire Background Job**: Scheduled job that sends reminder notifications

---

## User Endpoints

The AICalendar API provides three endpoints for managing push notifications:

### 1. Register Device Token

**Endpoint**: `POST /api/users/register-device`

Registers or updates a user's FCM device token for receiving push notifications.

#### Request Body

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fcmToken": "fGcI7X8kRZuQ9H7rL4kP5mN2jC6dV8wX..."
}
```

#### Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `userId` | GUID | Yes | The unique identifier of the user in the system |
| `fcmToken` | string | Yes | The Firebase Cloud Messaging token obtained from the Firebase SDK |

#### Response Codes

| Code | Description |
|------|-------------|
| 200 | Device token successfully registered |
| 400 | Invalid request data or malformed FCM token |
| 404 | User with the specified ID was not found |
| 500 | An unexpected error occurred during registration |

#### Success Response

```json
{
  "message": "Device registered successfully"
}
```

#### Error Response

```json
{
  "message": "User {userId} not found"
}
```

#### When to Use

- When the app starts and the user is logged in
- When the FCM token is refreshed by Firebase
- After user login/authentication
- When reinstalling the app

#### Code Example (JavaScript/TypeScript)

```typescript
async function registerDeviceToken(userId: string, fcmToken: string) {
  const response = await fetch('https://api.aicalendar.com/api/users/register-device', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      userId: userId,
      fcmToken: fcmToken
    })
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }

  const result = await response.json();
  console.log(result.message);
}
```

---

### 2. Send Test Notification

**Endpoint**: `POST /api/users/test-notification/{userId}`

Sends a test push notification to verify that the notification system is working correctly.

#### URL Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `userId` | GUID | Yes | The unique identifier of the user to send the test notification to |

#### Request Example

```
POST /api/users/test-notification/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

#### Response Codes

| Code | Description |
|------|-------------|
| 200 | Test notification sent successfully |
| 400 | User has no registered device token |
| 404 | User with the specified ID was not found |
| 500 | An error occurred while sending the notification |

#### Success Response

```json
{
  "message": "Test notification sent successfully"
}
```

#### Error Response (No Token)

```json
{
  "message": "User has no registered device token"
}
```

#### When to Use

- After registering a device token for the first time
- During development and testing
- When troubleshooting notification issues
- To verify Firebase configuration

#### Code Example (JavaScript/TypeScript)

```typescript
async function sendTestNotification(userId: string) {
  const response = await fetch(
    `https://api.aicalendar.com/api/users/test-notification/${userId}`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      }
    }
  );

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }

  const result = await response.json();
  console.log(result.message);
}
```

#### Test Notification Content

The test notification contains:
- **Title**: "Test Notification"
- **Body**: "This is a test notification from AICalendar"
- **Data**:
  - `type`: "test"
  - `timestamp`: Current UTC timestamp

---

### 3. Unregister Device Token

**Endpoint**: `POST /api/users/unregister-device/{userId}`

Removes a user's FCM device token to stop receiving push notifications.

#### URL Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `userId` | GUID | Yes | The unique identifier of the user to unregister |

#### Request Example

```
POST /api/users/unregister-device/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

#### Response Codes

| Code | Description |
|------|-------------|
| 200 | Device token successfully removed |
| 404 | User with the specified ID was not found |
| 500 | An error occurred while unregistering the device |

#### Success Response

```json
{
  "message": "Device unregistered successfully"
}
```

#### When to Use

- When a user logs out
- When a user disables notifications in settings
- When uninstalling the app (if possible to detect)
- When switching accounts
- For privacy compliance (GDPR, etc.)

#### Code Example (JavaScript/TypeScript)

```typescript
async function unregisterDevice(userId: string) {
  const response = await fetch(
    `https://api.aicalendar.com/api/users/unregister-device/${userId}`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      }
    }
  );

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }

  const result = await response.json();
  console.log(result.message);
}
```

---

## Integration Flow

### Complete Client Integration

Here's a step-by-step guide to integrate push notifications in your mobile app:

### Step 1: Setup Firebase SDK

#### iOS (Swift)

```swift
import Firebase
import FirebaseMessaging

// In AppDelegate.swift
func application(_ application: UIApplication,
                 didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
    FirebaseApp.configure()
    Messaging.messaging().delegate = self

    UNUserNotificationCenter.current().delegate = self
    UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .badge, .sound]) { granted, error in
        if granted {
            DispatchQueue.main.async {
                application.registerForRemoteNotifications()
            }
        }
    }

    return true
}
```

#### Android (Kotlin)

```kotlin
import com.google.firebase.messaging.FirebaseMessaging

class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Request notification permission (Android 13+)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            requestNotificationPermission()
        }

        // Get FCM token
        FirebaseMessaging.getInstance().token.addOnCompleteListener { task ->
            if (task.isSuccessful) {
                val token = task.result
                registerDeviceWithBackend(token)
            }
        }
    }
}
```

### Step 2: Obtain FCM Token

#### iOS (Swift)

```swift
extension AppDelegate: MessagingDelegate {
    func messaging(_ messaging: Messaging, didReceiveRegistrationToken fcmToken: String?) {
        guard let token = fcmToken else { return }

        // Store token locally
        UserDefaults.standard.set(token, forKey: "fcmToken")

        // Register with backend
        registerDeviceWithBackend(token: token)
    }
}
```

#### Android (Kotlin)

```kotlin
class MyFirebaseMessagingService : FirebaseMessagingService() {
    override fun onNewToken(token: String) {
        super.onNewToken(token)

        // Store token locally
        getSharedPreferences("app_prefs", MODE_PRIVATE)
            .edit()
            .putString("fcm_token", token)
            .apply()

        // Register with backend
        registerDeviceWithBackend(token)
    }
}
```

### Step 3: Register Token with AICalendar API

#### iOS (Swift)

```swift
func registerDeviceWithBackend(token: String) {
    guard let userId = getUserId() else { return }

    let url = URL(string: "https://api.aicalendar.com/api/users/register-device")!
    var request = URLRequest(url: url)
    request.httpMethod = "POST"
    request.setValue("application/json", forHTTPHeaderField: "Content-Type")

    let body: [String: Any] = [
        "userId": userId,
        "fcmToken": token
    ]

    request.httpBody = try? JSONSerialization.data(withJSONObject: body)

    URLSession.shared.dataTask(with: request) { data, response, error in
        if let error = error {
            print("Error registering device: \(error)")
            return
        }

        if let httpResponse = response as? HTTPURLResponse,
           httpResponse.statusCode == 200 {
            print("Device registered successfully")
        }
    }.resume()
}
```

#### Android (Kotlin)

```kotlin
fun registerDeviceWithBackend(token: String) {
    val userId = getUserId() ?: return

    val client = OkHttpClient()
    val json = JSONObject().apply {
        put("userId", userId)
        put("fcmToken", token)
    }

    val body = json.toString().toRequestBody("application/json".toMediaType())
    val request = Request.Builder()
        .url("https://api.aicalendar.com/api/users/register-device")
        .post(body)
        .build()

    client.newCall(request).enqueue(object : Callback {
        override fun onResponse(call: Call, response: Response) {
            if (response.isSuccessful) {
                Log.d("FCM", "Device registered successfully")
            }
        }

        override fun onFailure(call: Call, e: IOException) {
            Log.e("FCM", "Error registering device", e)
        }
    })
}
```

### Step 4: Handle Incoming Notifications

#### iOS (Swift)

```swift
extension AppDelegate: UNUserNotificationCenterDelegate {
    // Handle notification when app is in foreground
    func userNotificationCenter(_ center: UNUserNotificationCenter,
                              willPresent notification: UNNotification,
                              withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        let userInfo = notification.request.content.userInfo

        // Handle the notification data
        if let type = userInfo["type"] as? String {
            switch type {
            case "payment_reminder":
                handlePaymentReminder(userInfo)
            case "test":
                print("Test notification received")
            default:
                break
            }
        }

        // Show notification even when app is in foreground
        completionHandler([.banner, .sound, .badge])
    }

    // Handle notification tap
    func userNotificationCenter(_ center: UNUserNotificationCenter,
                              didReceive response: UNNotificationResponse,
                              withCompletionHandler completionHandler: @escaping () -> Void) {
        let userInfo = response.notification.request.content.userInfo

        // Navigate to appropriate screen based on notification data
        if let calendarItemId = userInfo["calendarItemId"] as? String {
            navigateToCalendarItem(id: calendarItemId)
        }

        completionHandler()
    }
}
```

#### Android (Kotlin)

```kotlin
class MyFirebaseMessagingService : FirebaseMessagingService() {
    override fun onMessageReceived(remoteMessage: RemoteMessage) {
        super.onMessageReceived(remoteMessage)

        // Handle notification data
        remoteMessage.data.let { data ->
            when (data["type"]) {
                "payment_reminder" -> handlePaymentReminder(data)
                "test" -> Log.d("FCM", "Test notification received")
            }
        }

        // Show notification
        remoteMessage.notification?.let {
            showNotification(it.title, it.body, remoteMessage.data)
        }
    }

    private fun showNotification(title: String?, body: String?, data: Map<String, String>) {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            putExtra("calendarItemId", data["calendarItemId"])
        }

        val pendingIntent = PendingIntent.getActivity(
            this, 0, intent, PendingIntent.FLAG_IMMUTABLE
        )

        val notification = NotificationCompat.Builder(this, "default_channel")
            .setSmallIcon(R.drawable.ic_notification)
            .setContentTitle(title)
            .setContentText(body)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(pendingIntent)
            .setAutoCancel(true)
            .build()

        NotificationManagerCompat.from(this).notify(1, notification)
    }
}
```

### Step 5: Handle User Logout

#### iOS (Swift)

```swift
func logout() {
    guard let userId = getUserId() else { return }

    // Unregister device token
    let url = URL(string: "https://api.aicalendar.com/api/users/unregister-device/\(userId)")!
    var request = URLRequest(url: url)
    request.httpMethod = "POST"

    URLSession.shared.dataTask(with: request) { _, _, _ in
        // Clear local data
        UserDefaults.standard.removeObject(forKey: "fcmToken")
        UserDefaults.standard.removeObject(forKey: "userId")

        // Navigate to login screen
        DispatchQueue.main.async {
            self.navigateToLogin()
        }
    }.resume()
}
```

#### Android (Kotlin)

```kotlin
fun logout() {
    val userId = getUserId() ?: return

    val client = OkHttpClient()
    val request = Request.Builder()
        .url("https://api.aicalendar.com/api/users/unregister-device/$userId")
        .post("".toRequestBody())
        .build()

    client.newCall(request).enqueue(object : Callback {
        override fun onResponse(call: Call, response: Response) {
            // Clear local data
            getSharedPreferences("app_prefs", MODE_PRIVATE)
                .edit()
                .clear()
                .apply()

            // Navigate to login screen
            navigateToLogin()
        }

        override fun onFailure(call: Call, e: IOException) {
            Log.e("Logout", "Error unregistering device", e)
        }
    })
}
```

---

## Testing

### Manual Testing Steps

1. **Register Device Token**
   ```bash
   curl -X POST https://api.aicalendar.com/api/users/register-device \
     -H "Content-Type: application/json" \
     -d '{
       "userId": "your-user-id",
       "fcmToken": "your-fcm-token"
     }'
   ```

2. **Send Test Notification**
   ```bash
   curl -X POST https://api.aicalendar.com/api/users/test-notification/your-user-id
   ```

3. **Verify Notification Received**
   - Check device notification tray
   - Verify notification content matches expected format

4. **Unregister Device**
   ```bash
   curl -X POST https://api.aicalendar.com/api/users/unregister-device/your-user-id
   ```

5. **Verify No Notifications**
   - Send another test notification
   - Confirm no notification is received

### Testing Checklist

- [ ] FCM token is successfully obtained from Firebase SDK
- [ ] Device registration endpoint returns 200 status
- [ ] Test notification is received on device
- [ ] Notification appears with correct title and body
- [ ] Tapping notification opens the app
- [ ] Token refresh is handled correctly
- [ ] Unregister removes token from database
- [ ] No notifications received after unregistering
- [ ] Multiple devices per user work correctly
- [ ] Error cases return appropriate status codes

---

## Troubleshooting

### Common Issues and Solutions

#### 1. Notification Not Received

**Problem**: Device registered but no notification received

**Solutions**:
- Verify FCM token is valid and not expired
- Check Firebase Console for delivery reports
- Ensure `firebase-credentials.json` is correctly configured on server
- Verify device has internet connection
- Check notification permissions are granted
- Review Firebase project settings and API keys

**Debug Steps**:
```bash
# Check if user has registered token
curl -X GET https://api.aicalendar.com/api/users/{userId}/device-status

# Check server logs for FCM errors
tail -f /var/log/aicalendar/notification-service.log
```

#### 2. Invalid FCM Token Error

**Problem**: 400 Bad Request when registering device

**Solutions**:
- Ensure FCM token is properly formatted string
- Token should not be empty or null
- Verify Firebase SDK is initialized correctly
- Check for special characters or encoding issues

**Valid Token Format**:
```
fGcI7X8kRZuQ9H7rL4kP5mN2jC6dV8wX1yZ3aB4cD5eF6gH7iJ8kL9mN0oP1qR2sT3uV4wX5yZ6...
```

#### 3. User Not Found Error

**Problem**: 404 Not Found when calling any endpoint

**Solutions**:
- Verify user ID is correct GUID format
- Ensure user exists in database
- Check user was created successfully during registration
- Confirm user ID matches authenticated user

#### 4. Token Refresh Not Working

**Problem**: Old token still stored after refresh

**Solutions**:
- Implement `onNewToken` callback correctly
- Call register-device endpoint when token changes
- Don't cache tokens indefinitely on client side
- Update token immediately when Firebase SDK provides new one

#### 5. Notifications Work in Development but Not Production

**Solutions**:
- Verify production Firebase credentials are loaded
- Check APNs certificates for iOS production
- Ensure production API keys are configured
- Review Firebase project production settings

---

## Best Practices

### Security

1. **Never Expose FCM Tokens**
   - Don't log tokens in production
   - Don't include tokens in URLs
   - Store tokens securely on backend

2. **Validate User Ownership**
   - Always verify user ID matches authenticated user
   - Don't allow users to register tokens for other users
   - Implement proper authentication/authorization

3. **Rotate Tokens Regularly**
   - Handle token refresh from Firebase
   - Update backend when token changes
   - Remove old/invalid tokens

### Performance

1. **Batch Operations**
   - Register token once per app launch
   - Don't call registration endpoint repeatedly
   - Cache token locally to avoid unnecessary calls

2. **Background Processing**
   - Send notifications asynchronously
   - Use Hangfire for scheduled notifications
   - Don't block API requests waiting for FCM

3. **Error Handling**
   - Implement exponential backoff for retries
   - Handle FCM errors gracefully
   - Log failures for monitoring

### User Experience

1. **Clear Notification Content**
   - Use descriptive titles and messages
   - Include relevant data for deep linking
   - Test notification appearance on both platforms

2. **Respect User Preferences**
   - Allow users to disable notifications
   - Provide granular notification settings
   - Unregister on logout

3. **Handle All States**
   - App in foreground
   - App in background
   - App terminated
   - First app launch
   - Token refresh

### Code Organization

1. **Centralize FCM Logic**
   ```typescript
   // Create a notification service
   class NotificationService {
     async registerDevice(userId: string, token: string) { }
     async sendTestNotification(userId: string) { }
     async unregisterDevice(userId: string) { }
   }
   ```

2. **Use Environment Variables**
   ```
   API_BASE_URL=https://api.aicalendar.com
   FIREBASE_PROJECT_ID=your-project-id
   ```

3. **Implement Retry Logic**
   ```typescript
   async function registerWithRetry(userId: string, token: string, maxRetries = 3) {
     for (let i = 0; i < maxRetries; i++) {
       try {
         await registerDeviceToken(userId, token);
         return;
       } catch (error) {
         if (i === maxRetries - 1) throw error;
         await sleep(Math.pow(2, i) * 1000);
       }
     }
   }
   ```

---

## Automated Reminder Notifications

### Background Job Overview

AICalendar uses **Hangfire** to automatically send push notifications for upcoming and overdue payments. The system runs a scheduled background job that checks all unpaid calendar items and sends timely reminders to users.

### Background Job Configuration

The reminder system is configured in the `HangfireConfiguration.cs` file:

**File**: `src/AICalendar.Infrastructure/BackgroundJobs/HangfireConfiguration.cs`

```csharp
// JOB 4: Send Reminders (Every Hour)
var remindersCron = configuration["BackgroundJobs:SendReminders:CronExpression"]
    ?? Cron.Hourly(); // Default: Every hour

recurringJobManager.AddOrUpdate<SendRemindersJob>(
    "send-reminders",
    job => job.SendDueReminders(),
    remindersCron,
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });
```

### Job Schedule

- **Frequency**: Every hour (by default)
- **Job Name**: `send-reminders`
- **Job Class**: `SendRemindersJob`
- **Method**: `SendDueReminders()`
- **Timezone**: UTC

### How It Works

The `SendRemindersJob` performs the following steps every hour:

1. **Fetch Unpaid Items**: Queries the database for all unpaid calendar items with due dates
2. **Check Time Windows**: Evaluates each item against multiple notification time windows
3. **Send Notifications**: Sends FCM push notifications to users whose items match a time window
4. **Log Results**: Records how many notifications were sent

### Notification Time Windows

The job sends notifications at these intervals:

#### Before Due Date
- **24 hours before**: "Payment Due Soon: {merchant} - ${amount} due in 24 hours"
- **6 hours before**: "Payment Due Soon: {merchant} - ${amount} due in 6 hours"
- **1 hour before**: "Payment Due Soon: {merchant} - ${amount} due in 1 hour"

#### After Due Date (Overdue)
- **12 hours overdue**: "Payment Overdue: {merchant} - ${amount} was due 12 hours ago"
- **24 hours overdue**: "Payment Overdue: {merchant} - ${amount} was due 24 hours ago"
- **48 hours overdue**: "Payment Overdue: {merchant} - ${amount} was due 48 hours ago"

### Code Implementation

**File**: `src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs`

```csharp
public async Task SendDueReminders()
{
    _logger.LogInformation("Starting reminder processing at {Time}", DateTime.UtcNow);

    // Get all unpaid calendar items with payment due dates
    var unpaidItems = await _calendarRepository.GetUnpaidItemsWithDueDatesAsync();

    if (!unpaidItems.Any())
    {
        _logger.LogInformation("No unpaid items found. Skipping reminder processing.");
        return;
    }

    var notificationsSent = 0;
    var now = DateTime.UtcNow;

    foreach (var (item, userId) in unpaidItems)
    {
        var paymentDue = item.DueDate;
        string? notificationMessage = null;

        // Check each time window
        if (now >= paymentDue.AddHours(-24) && now < paymentDue.AddHours(-23))
        {
            notificationMessage = $"Payment Due Soon: {item.Merchant} - ${item.Amount:F2} due in 24 hours";
        }
        else if (now >= paymentDue.AddHours(-6) && now < paymentDue.AddHours(-5))
        {
            notificationMessage = $"Payment Due Soon: {item.Merchant} - ${item.Amount:F2} due in 6 hours";
        }
        // ... more time windows ...

        // Send notification if a time window matched
        if (!string.IsNullOrEmpty(notificationMessage))
        {
            await _notificationService.SendPushNotificationAsync(
                userId: userId,
                title: "AICalendar Payment Reminder",
                message: notificationMessage,
                data: new Dictionary<string, string>
                {
                    { "calendarItemId", item.Id.Value.ToString() },
                    { "type", "payment_reminder" },
                    { "merchant", item.Merchant },
                    { "amount", item.Amount.ToString("F2") },
                    { "dueDate", paymentDue.ToString("O") }
                }
            );

            notificationsSent++;
        }
    }

    _logger.LogInformation(
        "Reminder processing completed. Sent {SentCount} notifications out of {TotalCount} unpaid items",
        notificationsSent,
        unpaidItems.Count
    );
}
```

### Reminder Notification Format

When a notification is sent, it has the following structure:

```json
{
  "title": "AICalendar Payment Reminder",
  "body": "Payment Due Soon: Netflix - $15.99 due in 24 hours (Jan 15, 11:00 PM UTC)",
  "data": {
    "type": "payment_reminder",
    "calendarItemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "merchant": "Netflix",
    "amount": "15.99",
    "dueDate": "2025-01-15T23:00:00.0000000Z"
  }
}
```

### How to Use Reminder Data in Your App

When your mobile app receives a reminder notification, use the `data` payload to handle it appropriately:

#### iOS (Swift)

```swift
extension AppDelegate: UNUserNotificationCenterDelegate {
    func userNotificationCenter(_ center: UNUserNotificationCenter,
                              didReceive response: UNNotificationResponse,
                              withCompletionHandler completionHandler: @escaping () -> Void) {
        let userInfo = response.notification.request.content.userInfo

        // Check if it's a payment reminder
        if let type = userInfo["type"] as? String, type == "payment_reminder" {
            if let calendarItemId = userInfo["calendarItemId"] as? String,
               let merchant = userInfo["merchant"] as? String,
               let amount = userInfo["amount"] as? String {

                // Navigate to the calendar item details
                navigateToCalendarItem(id: calendarItemId)

                // Or show a quick action to mark as paid
                showQuickPaymentAction(
                    itemId: calendarItemId,
                    merchant: merchant,
                    amount: amount
                )
            }
        }

        completionHandler()
    }
}
```

#### Android (Kotlin)

```kotlin
class MyFirebaseMessagingService : FirebaseMessagingService() {
    override fun onMessageReceived(remoteMessage: RemoteMessage) {
        super.onMessageReceived(remoteMessage)

        remoteMessage.data.let { data ->
            if (data["type"] == "payment_reminder") {
                val calendarItemId = data["calendarItemId"]
                val merchant = data["merchant"]
                val amount = data["amount"]
                val dueDate = data["dueDate"]

                // Create notification with action buttons
                showReminderNotification(
                    title = remoteMessage.notification?.title ?: "Payment Reminder",
                    body = remoteMessage.notification?.body ?: "",
                    calendarItemId = calendarItemId,
                    merchant = merchant,
                    amount = amount
                )
            }
        }
    }

    private fun showReminderNotification(
        title: String,
        body: String,
        calendarItemId: String?,
        merchant: String?,
        amount: String?
    ) {
        // Create intent to open calendar item
        val viewIntent = Intent(this, CalendarDetailActivity::class.java).apply {
            putExtra("calendarItemId", calendarItemId)
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        val viewPendingIntent = PendingIntent.getActivity(
            this, 0, viewIntent, PendingIntent.FLAG_IMMUTABLE
        )

        // Create intent to mark as paid
        val markPaidIntent = Intent(this, MarkAsPaidService::class.java).apply {
            putExtra("calendarItemId", calendarItemId)
        }
        val markPaidPendingIntent = PendingIntent.getService(
            this, 1, markPaidIntent, PendingIntent.FLAG_IMMUTABLE
        )

        val notification = NotificationCompat.Builder(this, "reminders_channel")
            .setSmallIcon(R.drawable.ic_notification)
            .setContentTitle(title)
            .setContentText(body)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(viewPendingIntent)
            .addAction(R.drawable.ic_check, "Mark as Paid", markPaidPendingIntent)
            .addAction(R.drawable.ic_view, "View Details", viewPendingIntent)
            .setAutoCancel(true)
            .build()

        NotificationManagerCompat.from(this).notify(2, notification)
    }
}
```

### Customization Options

#### 1. Change Notification Frequency

Modify the cron expression in `appsettings.json`:

```json
{
  "BackgroundJobs": {
    "SendReminders": {
      "CronExpression": "0 */30 * * * *"  // Every 30 minutes
    }
  }
}
```

Common cron expressions:
- Every 30 minutes: `"0 */30 * * * *"`
- Every 15 minutes: `"0 */15 * * * *"`
- Every 2 hours: `"0 0 */2 * * *"`
- Every day at 8 AM: `"0 0 8 * * *"`

#### 2. Modify Notification Time Windows

Edit the time windows in `SendRemindersJob.cs`:

```csharp
// Example: Add a 3-hour warning
else if (now >= paymentDue.AddHours(-3) && now < paymentDue.AddHours(-2))
{
    notificationMessage = $"Payment Due Soon: {item.Merchant} - ${item.Amount:F2} due in 3 hours";
}
```

#### 3. Disable Specific Time Windows

Comment out unwanted time windows:

```csharp
// Disable overdue notifications
// else if (now >= paymentDue.AddHours(12) && now < paymentDue.AddHours(13))
// {
//     notificationMessage = $"Payment Overdue: {item.Merchant} - ${amount} was due 12 hours ago";
// }
```

#### 4. Change Notification Messages

Customize the notification text:

```csharp
notificationMessage = $"⏰ Hey! Your {item.Merchant} payment of ${item.Amount:F2} is due in 24 hours!";
```

### Monitoring Background Jobs

#### View Job Status in Hangfire Dashboard

1. Navigate to: `https://your-api-url/hangfire`
2. Click on "Recurring Jobs"
3. Find "send-reminders" job
4. View:
   - Last execution time
   - Next execution time
   - Success/failure history

#### Check Logs

The job logs important information:

```
[INFO] Starting reminder processing at 2025-01-04T10:00:00Z
[INFO] Found 15 unpaid items to check
[INFO] Sent reminder for item abc123 to user xyz789: Payment Due Soon: Netflix - $15.99 due in 24 hours
[INFO] Reminder processing completed. Sent 5 notifications out of 15 unpaid items
```

#### Trigger Job Manually

You can manually trigger the reminder job for testing:

```bash
# Using curl to trigger Hangfire job
curl -X POST https://your-api-url/hangfire/jobs/enqueue \
  -H "Content-Type: application/json" \
  -d '{"job": "send-reminders"}'
```

Or via Hangfire Dashboard:
1. Go to "Recurring Jobs"
2. Find "send-reminders"
3. Click "Trigger Now"

### Testing the Reminder System

#### Step 1: Create a Test Calendar Item

Create a calendar item with a due date in the near future:

```bash
curl -X POST https://api.aicalendar.com/api/calendar/items \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "your-user-id",
    "merchant": "Test Payment",
    "amount": 10.00,
    "dueDate": "2025-01-05T12:00:00Z"  // Set to ~24 hours from now
  }'
```

#### Step 2: Register Device Token

Ensure your device token is registered:

```bash
curl -X POST https://api.aicalendar.com/api/users/register-device \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "your-user-id",
    "fcmToken": "your-fcm-token"
  }'
```

#### Step 3: Wait or Trigger Job

- **Wait**: Let the hourly job run automatically
- **Trigger**: Manually trigger the job via Hangfire Dashboard

#### Step 4: Verify Notification

- Check your mobile device for the notification
- Check Hangfire logs for confirmation
- Verify the notification contains correct data

### Troubleshooting Reminders

#### Notifications Not Sending

**Check 1: Job is Running**
```bash
# Check Hangfire dashboard
# Verify "send-reminders" is enabled and executing
```

**Check 2: User Has Device Token**
```sql
SELECT Id, Email, FcmDeviceToken
FROM Users
WHERE Id = 'your-user-id';
```

**Check 3: Items Are Unpaid**
```sql
SELECT * FROM CalendarItems
WHERE IsPaid = 0
AND DueDate IS NOT NULL;
```

**Check 4: Time Window Matches**
- Ensure current time matches one of the time windows
- Check job logs for processing details

**Check 5: Firebase Credentials**
- Verify `firebase-credentials.json` exists
- Check Firebase Console for delivery reports

#### Duplicate Notifications

**Problem**: User receives the same notification multiple times

**Solution**: The time windows are designed with 1-hour gaps to prevent duplicates. If you reduce the job frequency below 1 hour, you may get duplicates.

**Fix**:
```csharp
// Add tracking to prevent duplicate sends
private static HashSet<string> _sentNotifications = new();

var notificationKey = $"{item.Id}_{timeWindow}";
if (_sentNotifications.Contains(notificationKey))
{
    continue; // Skip already sent
}

// Send notification...
_sentNotifications.Add(notificationKey);
```

#### Performance Issues

**Problem**: Job takes too long to process many items

**Solution 1**: Add pagination
```csharp
var batchSize = 100;
var unpaidItems = await _calendarRepository
    .GetUnpaidItemsWithDueDatesAsync(limit: batchSize);
```

**Solution 2**: Parallel processing
```csharp
await Parallel.ForEachAsync(unpaidItems, async (item, cancellationToken) =>
{
    // Send notification...
});
```

---

## Database Schema

### User Table

The FCM device token is stored in the `Users` table:

```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL,
    FullName NVARCHAR(256) NOT NULL,
    FcmDeviceToken NVARCHAR(500) NULL, -- FCM token stored here
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
```

### Key Methods in User Entity

```csharp
public class User : AggregateRoot<UserId>
{
    public string? FcmDeviceToken { get; private set; }

    public void UpdateDeviceToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Device token cannot be empty");

        FcmDeviceToken = token;
    }

    public void ClearDeviceToken()
    {
        FcmDeviceToken = null;
    }
}
```

---

## API Response Examples

### Successful Registration

**Request**:
```http
POST /api/users/register-device HTTP/1.1
Content-Type: application/json

{
  "userId": "a7b8c9d0-1234-5678-90ab-cdef12345678",
  "fcmToken": "fGcI7X8kRZuQ9H7rL4kP5mN2jC6dV8wX..."
}
```

**Response**:
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "message": "Device registered successfully"
}
```

### User Not Found

**Request**:
```http
POST /api/users/register-device HTTP/1.1
Content-Type: application/json

{
  "userId": "00000000-0000-0000-0000-000000000000",
  "fcmToken": "fGcI7X8kRZuQ9H7rL4kP5mN2jC6dV8wX..."
}
```

**Response**:
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "message": "User 00000000-0000-0000-0000-000000000000 not found"
}
```

### Invalid Token

**Request**:
```http
POST /api/users/register-device HTTP/1.1
Content-Type: application/json

{
  "userId": "a7b8c9d0-1234-5678-90ab-cdef12345678",
  "fcmToken": ""
}
```

**Response**:
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "message": "Device token cannot be empty"
}
```

---

## Related Documentation

- [Firebase Notification Implementation Guide](./FIREBASE_NOTIFICATION_IMPLEMENTATION.md)
- [SendRemindersJob Documentation](../src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs)
- [Notification Service Interface](../src/AICalendar.Application/Common/Interfaces/INotificationService.cs)
- [Firebase Console Documentation](https://firebase.google.com/docs/cloud-messaging)
- [APNs Documentation (iOS)](https://developer.apple.com/documentation/usernotifications)

---

## Support

For issues or questions:
1. Check the [Troubleshooting](#troubleshooting) section
2. Review Firebase Console logs
3. Check server logs for detailed error messages
4. Contact the development team

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-04 | Initial documentation |

---

**Last Updated**: January 4, 2025
