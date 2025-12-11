# Get Predictions By Month Endpoint

## Overview

This endpoint retrieves all predictions for a specific user for a particular month. It allows you to filter predictions based on the year and month, making it easy to get predictions for specific time periods like November 2024, December 2024, etc.

## Endpoint Details

**URL**: `/api/predictions/user/{userId}/month/{year}/{month}`

**Method**: `GET`

**Authentication**: Not required (based on current controller configuration)

## Parameters

| Parameter | Type | Location | Required | Description | Example |
|-----------|------|----------|----------|-------------|---------|
| `userId` | GUID | Path | Yes | The unique identifier of the user | `3fa85f64-5717-4562-b3fc-2c963f66afa6` |
| `year` | Integer | Path | Yes | The year (must be between 2000-2100) | `2024` |
| `month` | Integer | Path | Yes | The month (1-12, where 1=January, 11=November, 12=December) | `11` |

## Month Reference

| Month Number | Month Name |
|--------------|------------|
| 1 | January |
| 2 | February |
| 3 | March |
| 4 | April |
| 5 | May |
| 6 | June |
| 7 | July |
| 8 | August |
| 9 | September |
| 10 | October |
| 11 | November |
| 12 | December |

## Response Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success - Returns list of predictions for the specified month |
| 400 | Bad Request - Invalid year or month parameter |
| 404 | Not Found - User not found |

## Request Examples

### Example 1: Get November 2024 Predictions

```bash
GET /api/predictions/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/month/2024/11
```

**cURL**:
```bash
curl -X GET "https://your-api.com/api/predictions/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/month/2024/11" \
  -H "accept: application/json"
```

### Example 2: Get December 2024 Predictions

```bash
GET /api/predictions/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/month/2024/12
```

**cURL**:
```bash
curl -X GET "https://your-api.com/api/predictions/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/month/2024/12" \
  -H "accept: application/json"
```

### Example 3: Get January 2025 Predictions

```bash
GET /api/predictions/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/month/2025/1
```

**cURL**:
```bash
curl -X GET "https://your-api.com/api/predictions/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/month/2025/1" \
  -H "accept: application/json"
```

## Response Format

### Success Response (200 OK)

```json
[
  {
    "id": "7d3e4f5a-6b7c-8d9e-0f1a-2b3c4d5e6f7a",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Completed",
    "createdAt": "2024-11-01T10:30:00Z",
    "items": [
      {
        "id": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
        "merchant": "Netflix",
        "amount": 15.99,
        "dueDate": "2024-11-15T00:00:00Z",
        "explanation": "Monthly subscription payment based on historical pattern",
        "confidence": 0.95,
        "pattern": "Monthly",
        "isAccepted": true,
        "isEdited": false,
        "account": "1234****5678",
        "accountName": "Personal Checking",
        "description": "Netflix Premium subscription"
      },
      {
        "id": "2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7e",
        "merchant": "Rent Payment",
        "amount": 1200.00,
        "dueDate": "2024-11-01T00:00:00Z",
        "explanation": "Monthly rent payment on the 1st of each month",
        "confidence": 0.98,
        "pattern": "Monthly",
        "isAccepted": true,
        "isEdited": false,
        "account": "1234****5678",
        "accountName": "Personal Checking",
        "description": "Apartment rent"
      }
    ]
  },
  {
    "id": "8e4f5a6b-7c8d-9e0f-1a2b-3c4d5e6f7a8b",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Pending",
    "createdAt": "2024-11-10T08:15:00Z",
    "items": [
      {
        "id": "3c4d5e6f-7a8b-9c0d-1e2f-3a4b5c6d7e8f",
        "merchant": "Electric Company",
        "amount": 85.50,
        "dueDate": "2024-11-20T00:00:00Z",
        "explanation": "Utility payment based on average monthly consumption",
        "confidence": 0.87,
        "pattern": "Monthly",
        "isAccepted": null,
        "isEdited": false,
        "account": "1234****5678",
        "accountName": "Personal Checking",
        "description": null
      }
    ]
  }
]
```

### Empty Result (200 OK)

If no predictions exist for the specified month, an empty array is returned:

```json
[]
```

### Error Response - Invalid Month (400 Bad Request)

```json
"Month must be between 1 and 12."
```

### Error Response - Invalid Year (400 Bad Request)

```json
"Year must be between 2000 and 2100."
```

### Error Response - User Not Found (404 Not Found)

```json
"User 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found."
```

## Response Fields

### Prediction Object

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique identifier for the prediction |
| `userId` | GUID | The user who owns this prediction |
| `status` | String | Status of the prediction (`Pending`, `Generated`, `Reviewing`, `Completed`, `Expired`, `Failed`) |
| `createdAt` | DateTime | When the prediction was created (UTC) |
| `items` | Array | List of predicted payment items |

### Prediction Item Object

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique identifier for the prediction item |
| `merchant` | String | The merchant or payee name |
| `amount` | Decimal | The predicted payment amount |
| `dueDate` | DateTime | The predicted due date |
| `explanation` | String | AI-generated explanation for why this prediction was made |
| `confidence` | Double | Confidence score (0.0 - 1.0) indicating how confident the AI is |
| `pattern` | String | The payment pattern detected (`OneTime`, `Weekly`, `BiWeekly`, `Monthly`, `Quarterly`, `Yearly`) |
| `isAccepted` | Boolean? | Whether the user accepted this prediction (null = not reviewed, true = accepted, false = rejected) |
| `isEdited` | Boolean | Whether the user edited this prediction item |
| `account` | String? | Masked account number |
| `accountName` | String? | Name of the account |
| `description` | String? | Additional description or notes |

## Use Cases

### 1. Monthly Dashboard View

Display all predictions for the current month to give users an overview of their predicted payments:

```javascript
const currentDate = new Date();
const year = currentDate.getFullYear();
const month = currentDate.getMonth() + 1; // JavaScript months are 0-indexed

const response = await fetch(
  `https://your-api.com/api/predictions/user/${userId}/month/${year}/${month}`
);
const predictions = await response.json();
```

### 2. Calendar Integration

Fetch predictions for a specific month when the user navigates to that month in a calendar view:

```javascript
function fetchPredictionsForMonth(userId, year, month) {
  return fetch(
    `https://your-api.com/api/predictions/user/${userId}/month/${year}/${month}`
  ).then(res => res.json());
}

// User navigates to November 2024
const novemberPredictions = await fetchPredictionsForMonth(userId, 2024, 11);
```

### 3. Historical Analysis

Retrieve predictions from previous months to analyze accuracy or trends:

```javascript
// Get last 6 months of predictions
const months = [];
for (let i = 0; i < 6; i++) {
  const date = new Date();
  date.setMonth(date.getMonth() - i);
  const year = date.getFullYear();
  const month = date.getMonth() + 1;

  const predictions = await fetchPredictionsForMonth(userId, year, month);
  months.push({ year, month, predictions });
}
```

### 4. Budget Planning

Help users plan their budget by showing predicted expenses for upcoming months:

```javascript
// Get next 3 months
const upcomingMonths = [];
for (let i = 0; i < 3; i++) {
  const date = new Date();
  date.setMonth(date.getMonth() + i);
  const year = date.getFullYear();
  const month = date.getMonth() + 1;

  const predictions = await fetchPredictionsForMonth(userId, year, month);
  const totalPredicted = predictions.reduce((sum, pred) => {
    return sum + pred.items.reduce((itemSum, item) => itemSum + item.amount, 0);
  }, 0);

  upcomingMonths.push({ year, month, totalPredicted, predictions });
}
```

## Query Logic

The endpoint filters predictions based on the following criteria:

1. **User Match**: Predictions must belong to the specified user
2. **Year Match**: The prediction's cycle start date year must match the requested year
3. **Month Match**: The prediction's cycle start date month must match the requested month
4. **Ordering**: Results are ordered by creation date (most recent first)

### Example:

If you request predictions for November 2024:
- `year = 2024`
- `month = 11`

The query will return all predictions where:
- `prediction.Cycle.StartDate.Year == 2024`
- `prediction.Cycle.StartDate.Month == 11`

## Differences from Other Endpoints

### vs. `/api/predictions/user/{userId}`

| Feature | `/user/{userId}` | `/user/{userId}/month/{year}/{month}` |
|---------|------------------|---------------------------------------|
| Filter by month | No | Yes |
| Returns | All predictions for user | Only predictions for specified month |
| Use case | Get complete prediction history | Get predictions for specific month |

## Validation

The endpoint includes built-in validation:

- **Month**: Must be between 1 and 12
- **Year**: Must be between 2000 and 2100
- **User ID**: Must be a valid GUID format
- **User Existence**: User must exist in the database

## Performance Considerations

- The query is optimized with proper indexing on the `UserId` and `Cycle.StartDate` fields
- Results are projected to DTOs to avoid loading unnecessary data
- The query uses `NoTracking` for better read performance

## Testing with Swagger

If you have Swagger UI enabled, you can test this endpoint at:

```
https://your-api.com/swagger
```

1. Navigate to the **Predictions** section
2. Find the `GET /api/predictions/user/{userId}/month/{year}/{month}` endpoint
3. Click "Try it out"
4. Enter the required parameters:
   - userId: A valid user GUID
   - year: e.g., `2024`
   - month: e.g., `11` (for November)
5. Click "Execute"
6. View the response

## Related Endpoints

- `GET /api/predictions/{predictionId}` - Get a single prediction by ID
- `GET /api/predictions/user/{userId}` - Get all predictions for a user
- `PUT /api/predictions/items/{itemId}` - Edit a prediction item
- `POST /api/predictions/batch-process` - Accept or reject multiple prediction items

## Technical Implementation

### Files Created

1. **Query**: `src/AICalendar.Application/Predictions/Queries/GetUserPredictionsByMonth/GetUserPredictionsByMonthQuery.cs`
2. **Handler**: `src/AICalendar.Application/Predictions/Queries/GetUserPredictionsByMonth/GetUserPredictionsByMonthQueryHandler.cs`
3. **Validator**: `src/AICalendar.Application/Predictions/Queries/GetUserPredictionsByMonth/GetUserPredictionsByMonthQueryValidator.cs`
4. **Controller**: Updated `src/AICalendar.API/Controllers/PredictionsController.cs`

### Architecture Pattern

The implementation follows the **CQRS (Command Query Responsibility Segregation)** pattern:

- **Query**: Defines the request with user ID, year, and month
- **Handler**: Processes the query and retrieves data from the database
- **Validator**: Ensures input parameters are valid using FluentValidation
- **Controller**: Exposes the HTTP endpoint and handles responses

## Troubleshooting

### Issue: Getting 404 for valid user

**Solution**: Verify the user ID is correct and the user exists in the database.

### Issue: Getting empty array

**Possible causes**:
1. No predictions exist for that month
2. Predictions exist but for a different month/year
3. Check the `Cycle.StartDate` of existing predictions

### Issue: Getting 400 error

**Solution**: Check that:
- Month is between 1-12
- Year is between 2000-2100
- User ID is a valid GUID format

## Best Practices

1. **Cache Results**: Consider caching predictions for the current month since they don't change frequently
2. **Error Handling**: Always handle potential error responses (400, 404)
3. **Loading States**: Show loading indicators while fetching data
4. **Empty States**: Display friendly messages when no predictions exist for a month
5. **Date Formatting**: Format dates according to user's locale and timezone

## Example Integration (React)

```jsx
import { useState, useEffect } from 'react';

function MonthlyPredictions({ userId, year, month }) {
  const [predictions, setPredictions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    async function fetchPredictions() {
      try {
        setLoading(true);
        const response = await fetch(
          `https://your-api.com/api/predictions/user/${userId}/month/${year}/${month}`
        );

        if (!response.ok) {
          throw new Error(`Error: ${response.status}`);
        }

        const data = await response.json();
        setPredictions(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    }

    fetchPredictions();
  }, [userId, year, month]);

  if (loading) return <div>Loading predictions...</div>;
  if (error) return <div>Error: {error}</div>;
  if (predictions.length === 0) {
    return <div>No predictions for this month</div>;
  }

  return (
    <div>
      <h2>Predictions for {month}/{year}</h2>
      {predictions.map(prediction => (
        <div key={prediction.id}>
          <h3>Status: {prediction.status}</h3>
          {prediction.items.map(item => (
            <div key={item.id}>
              <p>{item.merchant}: ${item.amount}</p>
              <p>Due: {new Date(item.dueDate).toLocaleDateString()}</p>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
```

---

**Last Updated**: 2025-12-09
**Version**: 1.0
