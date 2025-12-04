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

### Background Job Schedule

AICalendar automatically sends reminder notifications for upcoming payments using Hangfire:

- **Schedule**: Every 30 minutes
- **Job**: `SendRemindersJob`
- **Logic**: Sends notifications for items due within the next 24 hours

### Reminder Notification Format

```json
{
  "title": "Payment Reminder",
  "body": "{merchant} payment of ${amount} is due on {dueDate}",
  "data": {
    "type": "payment_reminder",
    "calendarItemId": "item-guid",
    "merchant": "Netflix",
    "amount": "15.99",
    "dueDate": "2025-01-15T00:00:00Z"
  }
}
```

### Customization

To modify reminder timing or frequency, update the Hangfire configuration:

```csharp
// In Program.cs or HangfireServiceExtensions.cs
RecurringJob.AddOrUpdate<ISendRemindersJob>(
    "send-reminders",
    job => job.ExecuteAsync(),
    Cron.Minutely(30) // Change this to adjust frequency
);
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
