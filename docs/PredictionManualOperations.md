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
  "predictionId": "24ffe7a8-6c10-4ebf-a810-56e6f27220b1"
}
```

### Get Prediction Details
```bash
curl -X GET "http://localhost:8080/api/predictions/{predictionId}"
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

### Accept Item
```bash
curl -X POST "http://localhost:8080/api/predictions/items/{itemId}/accept"
```

### Reject Item
```bash
curl -X POST "http://localhost:8080/api/predictions/items/{itemId}/reject"
```

### Edit Item
```bash
curl -X PUT "http://localhost:8080/api/predictions/items/{itemId}" \
  -H "Content-Type: application/json" \
  -d '{
    "merchant": "Netflix Premium",
    "amount": 19.99,
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
SELECT Id, Type, Error, AttemptCount, ProcessedOnUtc
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
**Cause:** Domain model changed (e.g., `PredictionId` property removed).

**Solution:**
```sql
-- Clear old incompatible messages
DELETE FROM OutboxMessages WHERE Error LIKE '%PredictionId%';
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
   PRED_ID=$(curl -s -X POST "http://localhost:8080/api/predictions/test-seed" | jq -r '.predictionId')
   echo "Prediction ID: $PRED_ID"
   ```

2. **Get prediction details:**
   ```bash
   ITEM_ID=$(curl -s "http://localhost:8080/api/predictions/$PRED_ID" | jq -r '.items[0].id')
   echo "Item ID: $ITEM_ID"
   ```

3. **Accept item (triggers outbox event):**
   ```bash
   curl -X POST "http://localhost:8080/api/predictions/items/$ITEM_ID/accept"
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

- **Test User ID:** `11111111-1111-1111-1111-111111111111`
- **Outbox Processing:** Every 10 seconds
- **Max Retry Attempts:** 5
- **Hangfire Dashboard:** http://localhost:8080/hangfire
