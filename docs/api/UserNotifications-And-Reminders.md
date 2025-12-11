# User Notifications and Reminders API

This document describes how AICalendar handles **user device notifications** and **automated payment reminders**.

It is based on the actual implementation in:
- `src/AICalendar.API/Controllers/UserNotificationsController.cs`
- `src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs`

---

## User Notifications API

These endpoints manage FCM device registration and test/unregister flows for push notifications.

### Base URL

```text
/api/user-notifications
```

### Endpoints Overview

| Method | Endpoint                                             | Description                               |
|--------|------------------------------------------------------|-------------------------------------------|
| POST   | `/api/user-notifications/register-device`            | Register or update FCM device token       |
| POST   | `/api/user-notifications/test-notification/{userId}` | Send a test push notification             |
| POST   | `/api/user-notifications/unregister-device/{userId}` | Remove a user's FCM device token          |

> The tables and examples below are aligned with `Users-API-Guide.md` and the controller implementation.

---

### 1. Register Device

Registers or updates a user's Firebase Cloud Messaging (FCM) device token for push notifications.

#### Endpoint

```text
POST /api/user-notifications/register-device
```

#### Request Body

```json path=null start=null
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fcmToken": "fGcI7X8kRZuQ9..."
}
```

#### Request Parameters

| Parameter | Type | Required | Description                                   |
|----------|------|----------|-----------------------------------------------|
| userId   | Guid | Yes      | The unique identifier of the user            |
| fcmToken | string | Yes    | FCM device token from the Firebase SDK      |

#### Response Codes

| Status Code | Description                                      |
|------------|--------------------------------------------------|
| 200        | Device token successfully registered              |
| 400        | Invalid request data or malformed FCM token       |
| 404        | User with the specified ID was not found          |
| 409        | User already has a registered device token (if enforced) |
| 500        | An unexpected error occurred during registration  |

#### Success Response (200)

```json path=null start=null
{
  "message": "Device registered successfully"
}
```

#### Error Response (404)

```json path=null start=null
{
  "message": "User {userId} not found"
}
```

#### Usage Notes

- The FCM token **must** be obtained from Firebase SDK on the client device.
- If a user already has a token registered, the command layer may update it with the new token depending on configuration.
- Call this endpoint when:
  - The app starts for the first time.
  - The FCM token is first generated or refreshed.
- A registered device is required before the user can receive push notifications or automated reminders.

#### Example Request (cURL)

```bash path=null start=null
curl -X POST "https://your-api-url.com/api/user-notifications/register-device" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fcmToken": "fGcI7X8kRZuQ9..."
  }'
```

---

### 2. Test Notification

Sends a test push notification to a user's registered device to verify Firebase Cloud Messaging is properly configured.

#### Endpoint

```text
POST /api/user-notifications/test-notification/{userId}
```

#### URL Parameters

| Parameter | Type | Required | Description                        |
|----------|------|----------|------------------------------------|
| userId   | Guid | Yes      | The unique identifier of the user |

#### Response Codes

| Status Code | Description                                      |
|------------|--------------------------------------------------|
| 200        | Test notification sent successfully              |
| 400        | User has no registered device token               |
| 404        | User with the specified ID was not found          |
| 500        | An error occurred while sending the notification  |

#### Success Response (200)

```json path=null start=null
{
  "message": "Test notification sent successfully"
}
```

#### Error Response (400)

```json path=null start=null
{
  "message": "User has no registered device token"
}
```

#### Notification Content

The test notification typically contains:

- **Title**: `"Test Notification"`
- **Body**: `"This is a test notification from AICalendar"`
- **Data**:
  - `type`: `"test"`
  - `timestamp`: Current UTC timestamp in ISO 8601 format

#### Usage Notes

- The user **must** have a registered FCM device token before calling this endpoint.
- Use this endpoint to validate that:
  - Firebase Cloud Messaging is correctly configured.
  - The user's device can receive notifications from AICalendar.

#### Example Request (cURL)

```bash path=null start=null
curl -X POST "https://your-api-url.com/api/user-notifications/test-notification/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json"
```

---

### 3. Unregister Device

Removes a user's FCM device token so they stop receiving push notifications.

#### Endpoint

```text
POST /api/user-notifications/unregister-device/{userId}
```

#### URL Parameters

| Parameter | Type | Required | Description                        |
|----------|------|----------|------------------------------------|
| userId   | Guid | Yes      | The unique identifier of the user |

#### Response Codes

| Status Code | Description                                      |
|------------|--------------------------------------------------|
| 200        | Device token successfully removed                 |
| 400        | User does not have a registered device token      |
| 404        | User with the specified ID was not found          |
| 500        | An error occurred while unregistering the device  |

#### Success Response (200)

```json path=null start=null
{
  "message": "Device unregistered successfully"
}
```

#### Error Response (404)

```json path=null start=null
{
  "message": "User {userId} not found"
}
```

#### Usage Notes

- Call this endpoint when a user logs out or explicitly opts out of push notifications.
- After unregistering, the user will not receive any push notifications until they register a new device token.
- This helps maintain user privacy and reduces unnecessary notification attempts.

#### Example Request (cURL)

```bash path=null start=null
curl -X POST "https://your-api-url.com/api/user-notifications/unregister-device/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json"
```

---

## Automated Payment Reminders

Payment reminders are not exposed as a public REST API. Instead, they are executed by a scheduled background job using Hangfire.

### Background Job: `SendRemindersJob`

- **Location**: `src/AICalendar.Application/BackgroundJobs/SendRemindersJob.cs`
- **Purpose**: Sends push notifications for upcoming and overdue payment-related calendar items.
- **Schedule**: Runs every hour (configured via Hangfire in infrastructure layer).

### Processing Flow

1. **Fetch unpaid items**
   - Calls `ICalendarRepository.GetUnpaidItemsWithDueDatesAsync()`.
   - Returns a collection of tuples `(item, userId)` where:
     - `item` has a `DueDate`, `Merchant`, `Amount`, and `Id`.
2. **Check time windows** relative to `item.DueDate` (UTC):
   - **24 hours before due**: `now >= dueDate - 24h` and `< dueDate - 23h`
   - **6 hours before due**: `now >= dueDate - 6h` and `< dueDate - 5h`
   - **1 hour before due**: `now >= dueDate - 1h` and `< dueDate`
   - **12 hours overdue**: `now >= dueDate + 12h` and `< dueDate + 13h`
   - **24 hours overdue**: `now >= dueDate + 24h` and `< dueDate + 25h`
   - **48 hours overdue**: `now >= dueDate + 48h` and `< dueDate + 49h`
3. **Build notification message** depending on the window:
   - Upcoming examples:
     - `"Payment Due Soon: {Merchant} - ${Amount} due in 24 hours ({DueDate} UTC)"`
     - `"Payment Due Soon: {Merchant} - ${Amount} due in 6 hours ({DueDate} UTC)"`
     - `"Payment Due Soon: {Merchant} - ${Amount} due in 1 hour ({DueDate} UTC)"`
   - Overdue examples:
     - `"Payment Overdue: {Merchant} - ${Amount} was due 12 hours ago"`
     - `"Payment Overdue: {Merchant} - ${Amount} was due 24 hours ago"`
     - `"Payment Overdue: {Merchant} - ${Amount} was due 48 hours ago"`
4. **Send push notification** via `INotificationService.SendPushNotificationAsync`:
   - **Title**: `"AICalendar Payment Reminder"`
   - **Body**: The computed `notificationMessage`.
   - **Data payload**:
     - `calendarItemId`: the calendar item ID
     - `type`: `"payment_reminder"`
     - `merchant`: merchant name
     - `amount`: amount formatted as string
     - `dueDate`: ISO 8601 (`"O"`) representation of due date
5. **Logging**
   - Logs start/end of processing.
   - Logs each sent notification with item and user IDs.

### Integration with User Notifications

- The reminder job relies on the **same FCM infrastructure** as other push notifications.
- To receive payment reminders, a user must:
  1. Have a valid FCM token registered via `POST /api/user-notifications/register-device`.
  2. Keep notifications enabled on their device.

There is currently **no public REST endpoint** to create or manage reminder schedules directly; they are derived from calendar items with unpaid status and due dates.

---

## Error Handling

User notification endpoints follow a simple, consistent error model:

```json path=null start=null
{
  "message": "Description of the error"
}
```

Typical scenarios:

1. **Validation Errors (400)**
   - Invalid `userId` format.
   - Missing or malformed `fcmToken`.
2. **Not Found (404)**
   - User does not exist in the database.
3. **Conflict (409)**
   - User already has a registered device token (if the command enforces single-device rules).
4. **Server Errors (500)**
   - Unexpected exceptions during processing or when calling notification providers.

---

## Authentication & Authorization

Currently, these endpoints do **not** require authentication headers.
If authentication is added in the future, clients will likely need to include:

```text
Authorization: Bearer {your-jwt-token}
```

Check with your system administrator or API gateway configuration for the most up-to-date requirements.

---

## Related Documentation

- `docs/api/Users-API-Guide.md`
- `docs/FIREBASE_NOTIFICATION_IMPLEMENTATION.md`
- `docs/TESTING_FIREBASE_NOTIFICATIONS.md`
- `docs/PUSH_NOTIFICATION_USER_GUIDE.md`
- `docs/BackgroundJobs.md` (for Hangfire and scheduled jobs)
