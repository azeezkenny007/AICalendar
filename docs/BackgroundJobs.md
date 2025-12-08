# Background Jobs Configuration

This document explains the Hangfire background jobs configured in the AICalendar system.

## Job Schedule Overview

All jobs run in UTC timezone. You can customize the schedule by modifying the `BackgroundJobs` section in `appsettings.json`.

### 1. Process Outbox Messages
**Default Schedule:** Every 10 seconds
**Cron Expression:** `*/10 * * * * *`
**Queue:** `critical`
**Purpose:** Processes domain events from the Outbox table and publishes them to event handlers (Calendar, UserFeedback).

**Why it's critical:** This is the backbone of the event-driven architecture. Without this job, accepted predictions won't be added to calendars and feedback won't be recorded.

---

### 2. Batch Prediction Generation
**Default Schedule:** Daily at 2:00 AM UTC
**Cron Expression:** `0 2 * * *`
**Queue:** `default`
**Purpose:** Generates predictions for all active users by calling the AI service with their transaction history.

**Customization Examples:**
- Run at 3 AM: `0 3 * * *`
- Run twice daily (2 AM and 2 PM): `0 2,14 * * *`
- Run every 6 hours: `0 */6 * * *`

---

### 3. Cleanup Expired Predictions
**Default Schedule:** Daily at 3:00 AM UTC
**Cron Expression:** `0 3 * * *`
**Queue:** `low`
**Purpose:** Deletes predictions older than 30 days that users never reviewed.

**Customization Examples:**
- Run weekly on Monday: `0 3 * * 1`
- Run monthly on the 1st: `0 3 1 * *`

---

### 4. Send Reminders
**Default Schedule:** Every hour
**Cron Expression:** `0 * * * *`
**Queue:** `default`
**Purpose:** Checks for upcoming calendar items and sends push notifications to users.

**Customization Examples:**
- Every 30 minutes: `*/30 * * * *`
- Every 2 hours: `0 */2 * * *`
- Only during business hours (9 AM - 5 PM): `0 9-17 * * *`

---

### 5. Cleanup Outbox
**Default Schedule:** Weekly on Sunday at 4:00 AM UTC
**Cron Expression:** `0 4 * * 0`
**Queue:** `low`
**Purpose:** Deletes processed outbox messages older than 7 days to keep the database clean.

**Customization Examples:**
- Daily: `0 4 * * *`
- Monthly on the 1st: `0 4 1 * *`

---

## Cron Expression Format

```
┌───────────── second (0 - 59) [OPTIONAL - only for sub-minute intervals]
│ ┌───────────── minute (0 - 59)
│ │ ┌───────────── hour (0 - 23)
│ │ │ ┌───────────── day of month (1 - 31)
│ │ │ │ ┌───────────── month (1 - 12)
│ │ │ │ │ ┌───────────── day of week (0 - 6) (Sunday to Saturday)
│ │ │ │ │ │
* * * * * *
```

### Common Patterns

| Expression | Description |
|------------|-------------|
| `* * * * *` | Every minute |
| `*/5 * * * *` | Every 5 minutes |
| `0 * * * *` | Every hour |
| `0 */6 * * *` | Every 6 hours |
| `0 2 * * *` | Daily at 2 AM |
| `0 2 * * 1` | Every Monday at 2 AM |
| `0 2 1 * *` | First day of month at 2 AM |
| `0 2 * * 0` | Every Sunday at 2 AM |

### Special Characters

- `*` - Any value
- `,` - Value list separator (e.g., `1,3,5`)
- `-` - Range of values (e.g., `1-5`)
- `/` - Step values (e.g., `*/5` = every 5 units)

---

## Queue Priority

Jobs are assigned to different queues based on priority:

1. **critical** - Processed first (Outbox processing)
2. **default** - Normal priority (Predictions, Reminders)
3. **low** - Processed last (Cleanup jobs)

---

## Monitoring

Access the Hangfire Dashboard at: `https://your-domain/hangfire`

From the dashboard you can:
- View job execution history
- See failed jobs and retry them
- Manually trigger jobs
- Monitor queue lengths
- View server statistics

---

## Production Recommendations

### For High-Traffic Systems:
```json
{
  "BackgroundJobs": { 
    "ProcessOutbox": {
      "CronExpression": "*/5 * * * * *"  // Every 5 seconds
    },
    "BatchPrediction": {
      "CronExpression": "0 2 * * *"  // Keep at 2 AM
    },
    "SendReminders": {
      "CronExpression": "*/15 * * * *"  // Every 15 minutes
    }
  },
  "Hangfire": {
    "WorkerCount": 50,  // Increase workers
    "RetryAttempts": 5
  }
}
```

### For Low-Traffic Systems:
```json
{
  "BackgroundJobs": {
    "ProcessOutbox": {
      "CronExpression": "*/30 * * * * *"  // Every 30 seconds
    },
    "BatchPrediction": {
      "CronExpression": "0 3 * * *"  // 3 AM
    },
    "SendReminders": {
      "CronExpression": "0 */2 * * *"  // Every 2 hours
    }
  },
  "Hangfire": {
    "WorkerCount": 10,
    "RetryAttempts": 3
  }
}
```

---

## Troubleshooting

### Jobs Not Running
1. Check Hangfire Dashboard for errors
2. Verify connection string in appsettings.json
3. Check application logs for exceptions
4. Ensure `HangfireConfiguration.ConfigureRecurringJobs()` is called in `Program.cs`

### Jobs Running Too Slowly
1. Increase `WorkerCount` in Hangfire settings
2. Consider adding more queues
3. Optimize job code (add indexes, reduce queries)

### Database Growing Too Large
1. Reduce Outbox retention period
2. Run cleanup jobs more frequently
3. Archive old data to separate storage
