# User Notifications and Reminders API

This document describes the API surface related to user notifications and reminders.

> NOTE: This is a starting point based on generic assumptions. Update the endpoint paths, request/response schemas, and auth details to match your actual implementation.

## Overview

The Notifications & Reminders APIs let clients:
- Create reminders tied to calendar events or standalone.
- List upcoming reminders for a user.
- Mark notifications as read / dismissed.
- Subscribe to notification channels (e.g. email, push, in-app).

All endpoints are assumed to be JSON-over-HTTP.

## Authentication

All endpoints require an authenticated user.

- Auth method: `Bearer <token>` (adjust if you use a different scheme).
- Include the header: `Authorization: Bearer <access_token>`.

## Base URL

Adjust this section to your environment setup.

- Production: `https://api.example.com`
- Staging: `https://staging-api.example.com`

All routes in this document are relative to the base URL.

---

## Reminders

### Create Reminder

- **Endpoint**: `POST /v1/reminders`
- **Description**: Create a new reminder for the authenticated user.

#### Request body

```json path=null start=null
{
  "title": "string",
  "description": "string(optional)",
  "scheduledAt": "ISO-8601 timestamp",
  "eventId": "string(optional)",
  "timezone": "IANA timezone string (e.g. 'America/Los_Angeles')",
  "channel": "in_app | email | push",
  "metadata": { "key": "value" }
}
```

#### Response

```json path=null start=null
{
  "id": "string",
  "title": "string",
  "description": "string|null",
  "scheduledAt": "ISO-8601 timestamp",
  "eventId": "string|null",
  "timezone": "string",
  "channel": "string",
  "status": "pending | sent | canceled",
  "createdAt": "ISO-8601 timestamp",
  "updatedAt": "ISO-8601 timestamp"
}
```

### List Reminders

- **Endpoint**: `GET /v1/reminders`
- **Description**: List reminders for the authenticated user.

#### Query parameters

- `from` (optional, ISO-8601): Start of time range.
- `to` (optional, ISO-8601): End of time range.
- `status` (optional): `pending | sent | canceled`.
- `limit` (optional, default 50): Max items to return.
- `cursor` (optional): For pagination.

#### Response

```json path=null start=null
{
  "items": [
    {
      "id": "string",
      "title": "string",
      "scheduledAt": "ISO-8601 timestamp",
      "status": "pending | sent | canceled"
    }
  ],
  "nextCursor": "string|null"
}
```

### Update Reminder

- **Endpoint**: `PATCH /v1/reminders/{id}`
- **Description**: Update fields for an existing reminder.

#### Request body

All fields optional; only provided fields are updated.

```json path=null start=null
{
  "title": "string",
  "description": "string|null",
  "scheduledAt": "ISO-8601 timestamp",
  "timezone": "string",
  "channel": "in_app | email | push",
  "status": "pending | canceled"
}
```

#### Response

Same as **Create Reminder** response.

### Delete Reminder

- **Endpoint**: `DELETE /v1/reminders/{id}`
- **Description**: Permanently delete a reminder.

#### Response

```json path=null start=null
{
  "success": true
}
```

---

## Notifications

Notifications represent messages delivered to the user (e.g. reminder fired, event changed, invite received).

### List Notifications

- **Endpoint**: `GET /v1/notifications`
- **Description**: Fetch notifications for the authenticated user.

#### Query parameters

- `status` (optional): `unread | read | dismissed`.
- `type` (optional): E.g. `event_update`, `reminder_fired`.
- `limit` (optional, default 50): Max items.
- `cursor` (optional): Pagination cursor.

#### Response

```json path=null start=null
{
  "items": [
    {
      "id": "string",
      "type": "string",
      "title": "string",
      "body": "string",
      "createdAt": "ISO-8601 timestamp",
      "status": "unread | read | dismissed",
      "data": {
        "eventId": "string(optional)",
        "reminderId": "string(optional)"
      }
    }
  ],
  "nextCursor": "string|null"
}
```

### Mark Notification as Read

- **Endpoint**: `POST /v1/notifications/{id}/read`
- **Description**: Mark the notification as `read`.

#### Response

```json path=null start=null
{
  "id": "string",
  "status": "read"
}
```

### Dismiss Notification

- **Endpoint**: `POST /v1/notifications/{id}/dismiss`
- **Description**: Mark the notification as `dismissed`.

#### Response

```json path=null start=null
{
  "id": "string",
  "status": "dismissed"
}
```

---

## Notification Channels

This section documents how users manage their notification preferences.

### Get Notification Settings

- **Endpoint**: `GET /v1/users/me/notification-settings`

#### Response

```json path=null start=null
{
  "channels": {
    "email": {
      "enabled": true,
      "reminders": true,
      "invites": true
    },
    "push": {
      "enabled": true,
      "reminders": true,
      "invites": true
    },
    "in_app": {
      "enabled": true,
      "reminders": true,
      "invites": true
    }
  },
  "quietHours": {
    "enabled": false,
    "start": "22:00",
    "end": "07:00",
    "timezone": "America/Los_Angeles"
  }
}
```

### Update Notification Settings

- **Endpoint**: `PUT /v1/users/me/notification-settings`

#### Request body

```json path=null start=null
{
  "channels": {
    "email": {
      "enabled": true,
      "reminders": true,
      "invites": false
    }
  },
  "quietHours": {
    "enabled": true,
    "start": "22:00",
    "end": "07:00",
    "timezone": "America/Los_Angeles"
  }
}
```

#### Response

Returns the full, updated settings object (same shape as **Get Notification Settings**).

---

## Webhooks (Optional)

If your system sends webhooks when reminders fire or notifications are created, document them here.

### Reminder Fired Webhook

- **Event**: `reminder.fired`

#### Payload

```json path=null start=null
{
  "id": "string",
  "type": "reminder.fired",
  "occurredAt": "ISO-8601 timestamp",
  "data": {
    "reminderId": "string",
    "userId": "string",
    "scheduledAt": "ISO-8601 timestamp",
    "channel": "in_app | email | push"
  }
}
```

---

## Errors

All endpoints use a common error envelope.

```json path=null start=null
{
  "error": {
    "code": "string",
    "message": "Human-readable message",
    "details": {}
  }
}
```

Common error codes (customize as needed):
- `UNAUTHENTICATED` – Missing or invalid auth token.
- `FORBIDDEN` – User not allowed to access this resource.
- `NOT_FOUND` – Resource not found.
- `INVALID_ARGUMENT` – Validation error.
- `INTERNAL` – Unexpected server-side error.
