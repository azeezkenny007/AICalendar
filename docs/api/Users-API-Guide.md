# Users API Guide

This guide covers all user-related endpoints in the AICalendar API for managing device registration and push notifications.

## Base URL

```
/api/users
```

## Endpoints Overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/users/register-device` | Register or update FCM device token |
| POST | `/api/users/test-notification/{userId}` | Send test push notification |
| POST | `/api/users/unregister-device/{userId}` | Remove FCM device token |

---

## 1. Register Device

Registers or updates a user's Firebase Cloud Messaging (FCM) device token for push notifications.

### Endpoint
```
POST /api/users/register-device
```

### Request Body
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fcmToken": "fGcI7X8kRZuQ9..."
}
```

### Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | Guid | Yes | The unique identifier of the user |
| fcmToken | string | Yes | FCM device token from Firebase SDK |

### Response Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Device token successfully registered |
| 400 | Invalid request data or malformed FCM token |
| 404 | User with the specified ID was not found |
| 500 | An unexpected error occurred during registration |

### Success Response (200)
```json
{
  "message": "Device registered successfully"
}
```

### Error Response (404)
```json
{
  "message": "User {userId} not found"
}
```

### Usage Notes
- The FCM token should be obtained from Firebase SDK on the client device
- If a user already has a token registered, this will update it with the new token
- Use this endpoint when the app starts or when the FCM token is refreshed
- This is required before the user can receive push notifications

### Example Request (cURL)
```bash
curl -X POST "https://your-api-url.com/api/users/register-device" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fcmToken": "fGcI7X8kRZuQ9..."
  }'
```

---

## 2. Test Notification

Sends a test push notification to a user's registered device to verify Firebase Cloud Messaging is properly configured.

### Endpoint
```
POST /api/users/test-notification/{userId}
```

### URL Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | Guid | Yes | The unique identifier of the user |

### Response Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Test notification sent successfully |
| 400 | User has no registered device token |
| 404 | User with the specified ID was not found |
| 500 | An error occurred while sending the notification |

### Success Response (200)
```json
{
  "message": "Test notification sent successfully"
}
```

### Error Response (400)
```json
{
  "message": "User has no registered device token"
}
```

### Notification Content

The test notification will contain:
- **Title**: "Test Notification"
- **Body**: "This is a test notification from AICalendar"
- **Data**:
  - `type`: "test"
  - `timestamp`: Current UTC timestamp in ISO 8601 format

### Usage Notes
- The user must have a registered FCM device token before calling this endpoint
- This endpoint is useful for testing that Firebase Cloud Messaging is properly configured
- Use this to verify that the user's device is correctly receiving notifications

### Example Request (cURL)
```bash
curl -X POST "https://your-api-url.com/api/users/test-notification/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json"
```

---

## 3. Unregister Device

Removes a user's FCM device token to stop receiving push notifications.

### Endpoint
```
POST /api/users/unregister-device/{userId}
```

### URL Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | Guid | Yes | The unique identifier of the user |

### Response Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Device token successfully removed |
| 404 | User with the specified ID was not found |
| 500 | An error occurred while unregistering the device |

### Success Response (200)
```json
{
  "message": "Device unregistered successfully"
}
```

### Error Response (404)
```json
{
  "message": "User {userId} not found"
}
```

### Usage Notes
- Call this endpoint when a user logs out
- Use when a user wants to stop receiving push notifications
- After unregistering, the user will not receive any push notifications until they register a new device token
- This helps maintain user privacy and reduces unnecessary notification attempts

### Example Request (cURL)
```bash
curl -X POST "https://your-api-url.com/api/users/unregister-device/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json"
```

---

## Common Use Cases

### 1. First Time App Setup
```
User opens app → Get FCM token from Firebase SDK → POST /api/users/register-device
```

### 2. Testing Notifications
```
Register device → POST /api/users/test-notification/{userId} → Verify notification received
```

### 3. User Logout
```
User logs out → POST /api/users/unregister-device/{userId}
```

### 4. Token Refresh
```
Firebase SDK refreshes token → POST /api/users/register-device with new token
```

---

## Implementation Details

### Controller Location
[UsersController.cs](../../src/AICalendar.API/Controllers/UsersController.cs)

### Dependencies
- **IUserRepository**: User data access (lines 16, 22)
- **IUnitOfWork**: Transaction management (lines 17, 23)
- **INotificationService**: Push notification handling (lines 18, 24)
- **ILogger**: Logging (lines 19, 25)

### Key Methods
- `RegisterDevice()`: [UsersController.cs:60-95](../../src/AICalendar.API/Controllers/UsersController.cs#L60-L95)
- `TestNotification()`: [UsersController.cs:120-155](../../src/AICalendar.API/Controllers/UsersController.cs#L120-L155)
- `UnregisterDevice()`: [UsersController.cs:178-201](../../src/AICalendar.API/Controllers/UsersController.cs#L178-L201)

---

## Related Documentation

- [Firebase Notification Implementation](../FIREBASE_NOTIFICATION_IMPLEMENTATION.md)
- [Testing Firebase Notifications](../TESTING_FIREBASE_NOTIFICATIONS.md)
- [Push Notification User Guide](../PUSH_NOTIFICATION_USER_GUIDE.md)
- [REST API Documentation](./REST-API-Documentation.md)

---

## Error Handling

All endpoints follow consistent error handling patterns:

1. **Validation Errors (400)**: Invalid input data, malformed tokens
2. **Not Found (404)**: User doesn't exist in the database
3. **Server Errors (500)**: Unexpected errors during processing

All errors return a JSON response with a descriptive message:
```json
{
  "message": "Description of the error"
}
```

---

## Authentication & Authorization

Currently, these endpoints do not require authentication headers. If authentication is added in the future, you will need to include:

```
Authorization: Bearer {your-jwt-token}
```

Check with your system administrator for current authentication requirements.
