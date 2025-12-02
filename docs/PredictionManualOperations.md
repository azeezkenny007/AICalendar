# 🛠️ Prediction Manual Operations Guide

This document contains manual operations and troubleshooting steps for the Prediction system.

---

## 📋 Table of Contents
1. [Testing Endpoints](#testing-endpoints)
2. [Database Operations](#database-operations)
3. [Outbox Troubleshooting](#outbox-troubleshooting)
4. [Common Issues](#common-issues)

---

## 🧪 Testing Endpoints

### Create Test Prediction
```bash
curl -X POST "http://localhost:8080/api/predictions/test-seed"
```

**Response:**
```json
{
  "predictionId": "24ffe7a8-6c10-4ebf-a810-56e6f27220b1",
  "userId": "11111111-1111-1111-1111-111111111111"
}
```

### Get Prediction Details
```bash
curl "http://localhost:8080/api/predictions/{predictionId}"
```

**Response:**
```json
{
  "id": "24ffe7a8-6c10-4ebf-a810-56e6f27220b1",
  "userId": "11111111-1111-1111-1111-111111111111",
  "status": "Generated",
  "createdAt": "2025-12-02T06:00:00Z",
  "items": [
    {
      "id": "abc123...",
      "merchant": "Netflix",
      "amount": 15.99,
      "dueDate": "2025-12-07T00:00:00Z",
      "explanation": "Monthly subscription detected",
      "confidence": 0.95,
      "pattern": "FixedDateRecurring",
      "isAccepted": null,
      "isEdited": false
    }
  ]
}
```

### Get User Predictions
**Get all predictions for a specific user (ordered by most recent first)**

```bash
curl "http://localhost:8080/api/predictions/user/{userId}"
```

**Response:**
```json
[
  {
    "id": "24ffe7a8-6c10-4ebf-a810-56e6f27220b1",
    "userId": "11111111-1111-1111-1111-111111111111",
    "status": "Generated",
    "createdAt": "2025-12-02T06:00:00Z",
    "items": [...]
  },
  {
    "id": "another-prediction-id",
    "userId": "11111111-1111-1111-1111-111111111111",
    "status": "Completed",
    "createdAt": "2025-12-01T06:00:00Z",
    "items": [...]
  }
]
```

### Batch Process Items (Accept/Reject)
**This is the recommended way to accept or reject predictions - much more efficient than individual requests!**

```bash
curl -X POST "http://localhost:8080/api/predictions/batch-process" \
  -H "Content-Type: application/json" \
  -d '{
    "acceptedItemIds": ["guid-1", "guid-2"],
    "rejectedItemIds": ["guid-3", "guid-4"]
  }'
```

### Edit Item
**Note: All fields are optional. You can update just one or multiple fields.**

```bash
# Example: Update only the amount
curl -X PUT "http://localhost:8080/api/predictions/items/{itemId}" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 19.99
  }'
```

```bash
# Example: Update merchant and due date
curl -X PUT "http://localhost:8080/api/predictions/items/{itemId}" \
  -H "Content-Type: application/json" \
  -d '{
    "merchant": "Netflix Premium",
    "dueDate": "2025-12-10T00:00:00Z"
  }'
```

---

## 💾 Database Operations

### Clear All Predictions
```sql
-- Connect to database
docker exec -it aicalendar-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd"

-- Delete all predictions and items
DELETE FROM PredictionItems;
DELETE FROM Predictions;
GO
```

### View Predictions
```sql
SELECT * FROM Predictions;
SELECT * FROM PredictionItems;
GO
```

---

## 📦 Outbox Troubleshooting

### Clear Failed Outbox Messages
If you see deserialization errors in logs (e.g., after changing domain models):

```sql
-- View failed messages
SELECT Id, Type, Error, RetryCount, ProcessedOnUtc
FROM OutboxMessages
WHERE Error IS NOT NULL;
GO

-- Delete failed messages
DELETE FROM OutboxMessages WHERE Error IS NOT NULL;
GO

-- Or delete all outbox messages
TRUNCATE TABLE OutboxMessages;
GO
```

### Check Outbox Status
```sql
-- Count pending messages
SELECT COUNT(*) as PendingCount
FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL;
GO

-- View recent messages
SELECT TOP 10 *
FROM OutboxMessages
ORDER BY OccurredOnUtc DESC;
GO
```

---

## ⚠️ Common Issues

### Issue: "Prediction not found"
**Cause:** Database was reset or prediction ID is incorrect.

**Solution:**
1. Create a new test prediction using `/api/predictions/test-seed`
2. Use the returned `predictionId` in subsequent requests

### Issue: Outbox deserialization errors
**Cause:** Domain model changed (e.g., removed old event types).

**Solution:**
```sql
-- Clear old incompatible messages
DELETE FROM OutboxMessages WHERE Error IS NOT NULL;
GO
```

### Issue: API not responding
**Cause:** Build failed or container crashed.

**Solution:**
```bash
# Check container status
docker compose ps

# View logs
docker compose logs api --tail 100

# Restart if needed
docker compose restart api
```

### Issue: Migration not applied
**Cause:** Database schema out of sync.

**Solution:**
```bash
# Apply latest migrations
./update-database.bat

# Or rebuild containers
docker compose down
docker compose up --build
```

---

## 🔄 Complete Test Flow

1. **Create prediction:**
   ```bash
   curl -X POST "http://localhost:8080/api/predictions/test-seed"
   # Save the predictionId from response
   ```

2. **Get prediction details:**
   ```bash
   curl "http://localhost:8080/api/predictions/{predictionId}"
   # Note the item IDs
   ```

3. **Batch process items:**
   ```bash
   curl -X POST "http://localhost:8080/api/predictions/batch-process" \
     -H "Content-Type: application/json" \
     -d '{
       "acceptedItemIds": ["item-id-1"],
       "rejectedItemIds": ["item-id-2"]
     }'
   ```

4. **Check Hangfire dashboard:**
   - Open: http://localhost:8080/hangfire
   - Navigate to "Recurring Jobs"
   - Check "process-outbox-messages" job
   - View "Succeeded" jobs to see event processing

5. **Verify in database:**
   ```sql
   SELECT * FROM OutboxMessages ORDER BY OccurredOnUtc DESC;
   GO
   ```

---

## 📝 Notes

- **Outbox Processing:** Every 10 seconds
- **Max Retry Attempts:** 5
- **Hangfire Dashboard:** http://localhost:8080/hangfire
- **Batch Processing:** Always prefer batch endpoints over individual requests for better performance
