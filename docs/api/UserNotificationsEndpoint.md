# User Notifications Endpoint

This document explains how the User Notifications endpoints work in the AICalendar API. These endpoints manage Firebase Cloud Messaging (FCM) device registration for push notifications.

## Overview

The User Notifications API is handled by the `UserNotificationsController` in `src/AICalendar.API/Controllers/UserNotificationsController.cs`. It uses MediatR for command handling and supports three main operations: registering a device, sending a test notification, and unregistering a device.

## Endpoints

### 1. Register Device

**Endpoint:** `POST /api/user-notifications/register-device`

**Purpose:** Registers a user's FCM device token for receiving push notifications.

**Request Body:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fcmToken": "fGcI7X8kRZuQ9..."
}
```

**Response Codes:**
- 200: Device registered successfully
- 400: Invalid request or malformed token
- 404: User not found
- 409: User already has a registered device
- 500: Internal server error

**How it works:**
- The controller receives the request and creates a `RegisterDeviceCommand`.
- The command is sent via MediatR to the application layer.
- The application layer validates the user and stores/updates the FCM token.
- Success or error is returned based on the result.

### 2. Test Notification

**Endpoint:** `POST /api/user-notifications/test-notification/{userId}`

**Purpose:** Sends a test push notification to verify FCM setup.

**Parameters:**
- `userId` (GUID): The user's ID

**Response Codes:**
- 200: Test notification sent
- 400: No registered device token
- 404: User not found
- 500: Error sending notification

**How it works:**
- Creates a `TestNotificationCommand` with the user ID.
- The command handler retrieves the user's FCM token.
- Sends a test notification via Firebase Cloud Messaging.
- Returns success or error status.

### 3. Unregister Device

**Endpoint:** `POST /api/user-notifications/unregister-device/{userId}`

**Purpose:** Removes the user's FCM token to stop notifications.

**Parameters:**
- `userId` (GUID): The user's ID

**Response Codes:**
- 200: Device unregistered successfully
- 400: No registered device token
- 404: User not found
- 500: Internal server error

**How it works:**
- Creates an `UnregisterDeviceCommand`.
- The handler removes the FCM token from the user's record.
- The user will no longer receive push notifications.

## Architecture

- **Controller Layer:** Handles HTTP requests, validation, and response formatting.
- **Application Layer:** Contains command handlers that implement business logic using MediatR.
- **Infrastructure Layer:** Manages FCM integration and data persistence.

## Usage Flow

1. User registers device on app start or token refresh.
2. System can send test notifications to verify setup.
3. Background jobs (like payment reminders) use the registered tokens to send notifications.
4. User can unregister to stop receiving notifications.

## Background Job: Payment Reminders

The `SendRemindersJob` is a Hangfire background job that automatically sends payment reminders to users based on their calendar items.

### How It Works

- **Schedule:** Runs every hour via Hangfire.
- **Purpose:** Sends push notifications for upcoming and overdue payments.

### Processing Steps

1. **Fetch Unpaid Items:** Retrieves all unpaid calendar items with due dates from the database using `ICalendarRepository.GetUnpaidItemsWithDueDatesAsync()`.

2. **Check Time Windows:** For each item, checks if the current time falls within specific reminder windows relative to the due date:
   - 24 hours before due
   - 6 hours before due
   - 1 hour before due
   - 12 hours overdue
   - 24 hours overdue
   - 48 hours overdue

3. **Generate Message:** Creates appropriate notification messages based on the time window:
   - Upcoming: "Payment Due Soon: {Merchant} - ${Amount} due in X hours ({DueDate} UTC)"
   - Overdue: "Payment Overdue: {Merchant} - ${Amount} was due X hours ago"

4. **Send Notification:** Uses `INotificationService.SendPushNotificationAsync()` to send FCM push notifications with:
   - Title: "AICalendar Payment Reminder"
   - Body: The generated message
   - Data payload containing calendar item details (ID, type, merchant, amount, due date)

5. **Logging:** Logs the start/end of processing and each notification sent.

### Integration with User Notifications

- Relies on users having registered FCM tokens via the register-device endpoint.
- Notifications are sent only to users with valid registered devices.
- The same FCM infrastructure is used for both manual test notifications and automated reminders.

## Related Files

- Controller: `src/AICalendar.API/Controllers/UserNotificationsController.cs`
- Commands: `src/AICalendar.Application/Users/Commands/`
- Detailed API Docs: `docs/api/UserNotifications-And-Reminders.md`
