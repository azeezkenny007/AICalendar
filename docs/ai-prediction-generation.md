# AI Prediction Generation Implementation

This document describes the implementation of the AI prediction generation feature that integrates with the external AI service via gRPC.

## Overview

The system automatically generates predictions for all users by:
1. Fetching all users from the database
2. Retrieving their historical transactions
3. Calling the AI prediction service via gRPC
4. Saving the generated predictions to the database

## Architecture

### Components

#### 1. **gRPC Client** (`AIPredictionService`)
- **Location**: `AICalendar.Infrastructure/Services/AIPredictionService.cs`
- **Purpose**: Communicates with the external AI service using gRPC
- **Configuration**: AI service URL is configured in `appsettings.json` under `AIService:Url`

#### 2. **Application Service Interface** (`IAIPredictionService`)
- **Location**: `AICalendar.Application/Services/IAIPredictionService.cs`
- **Purpose**: Defines the contract for AI prediction generation
- **Returns**: `PredictionResult` with predictions, metadata, and error information

#### 3. **Generate Predictions Command**
- **Command**: `AICalendar.Application/Predictions/Commands/GeneratePredictions/GeneratePredictionsCommand.cs`
- **Handler**: `GeneratePredictionsCommandHandler.cs`
- **Purpose**: Orchestrates the prediction generation process for a single user

#### 4. **Batch Prediction Job**
- **Location**: `AICalendar.Application/BackgroundJobs/BatchPredictionJob.cs`
- **Schedule**: Runs daily at 2 AM UTC (configured in `appsettings.json`)
- **Purpose**: Generates predictions for all users automatically

## Data Flow

```
BatchPredictionJob
    ↓
GetAllUsers (UserRepository)
    ↓
For each user:
    ↓
GeneratePredictionsCommand
    ↓
GetUserTransactions (TransactionRepository)
    ↓
AIPredictionService.GeneratePredictionsAsync (gRPC call)
    ↓
Create Prediction Aggregate with Items
    ↓
Save to Database (PredictionRepository + UnitOfWork)
```

## gRPC Contract

The proto file is located at: `AICalendar.API/Protos/predictive_calendar.proto`

### Key Messages

**Request (`PredictionRequest`)**:
- `user_id`: User identifier
- `target_month`: Month to generate predictions for (YYYY-MM format)
- `historical_transactions`: List of user's past transactions
- `max_predictions`: Maximum number of predictions to generate
- `timezone`: User's timezone

**Response (`PredictionResponse`)**:
- `status`: Success/Failure status
- `predictions`: List of predicted items
- `metadata`: Processing information (time, model version, etc.)
- `error_message`: Error details if failed

**Predicted Item (`PredictedItem`)**:
- `title`: Merchant/payment name
- `description`: Additional details
- `category`: Payment category (BILL_PAYMENT, SUBSCRIPTION, etc.)
- `predicted_date`: When the payment is expected
- `predicted_amount`: Expected amount (in cents/kobo)
- `currency`: Currency code
- `confidence_score`: AI confidence (0.0 - 1.0)
- `reasoning`: Why this prediction was made
- `pattern_type`: MONTHLY, WEEKLY, QUARTERLY, etc.

## Configuration

### appsettings.json

```json
{
  "AIService": {
    "Url": "http://localhost:5001"
  },
  "BackgroundJobs": {
    "BatchPrediction": {
      "CronExpression": "0 2 * * *",
      "Description": "Generate predictions for all users daily at 2 AM UTC"
    }
  }
}
```

### Environment Variables (Docker)

For production deployment, override the AI service URL:
```bash
AIService__Url=http://ai-service:5001
```

## Error Handling

The implementation includes comprehensive error handling:

1. **gRPC Errors**: Caught and logged with status codes
2. **No Transactions**: Returns a friendly error if user has no transaction history
3. **AI Service Failures**: Logged and reported without crashing the batch job
4. **Individual User Failures**: Don't stop the batch process for other users

## Logging

The system logs:
- Start/completion of batch prediction jobs
- Number of users processed
- Success/failure counts
- Individual prediction generation results
- gRPC communication errors
- Processing time and metadata

## Testing

### Manual Testing

1. **Trigger batch job manually** via Hangfire Dashboard:
   - Navigate to `/hangfire`
   - Go to "Recurring Jobs"
   - Click "Trigger now" on "batch-prediction-job"

2. **Test single user prediction**:
   ```csharp
   var command = new GeneratePredictionsCommand(
       UserId: UserId.Create(userId),
       TargetMonth: "2025-01",
       MaxPredictions: 10,
       Timezone: "UTC"
   );
   var result = await mediator.Send(command);
   ```

### Prerequisites

- AI service must be running and accessible at the configured URL
- Users must have transaction history
- Database must be accessible

## Future Enhancements

1. **Retry Logic**: Add retry policies for transient gRPC failures
2. **Batch Size**: Process users in batches to avoid memory issues
3. **User Preferences**: Allow users to configure timezone and max predictions
4. **Feedback Loop**: Send user feedback back to AI service for model improvement
5. **Caching**: Cache AI service responses to reduce load
6. **Monitoring**: Add metrics for prediction generation success rates

## Dependencies

### NuGet Packages
- `Grpc.Net.Client` (2.59.0): gRPC client library
- `Grpc.Tools` (2.59.0): Proto file compilation
- `Google.Protobuf` (3.25.1): Protobuf serialization

### Services
- External AI Prediction Service (gRPC)
- SQL Server (for persistence)
- Hangfire (for scheduling)

## Troubleshooting

### Common Issues

1. **gRPC connection errors**:
   - Verify AI service is running
   - Check `AIService:Url` configuration
   - Ensure network connectivity

2. **No predictions generated**:
   - Check if users have transaction history
   - Verify AI service is returning data
   - Check logs for AI service errors

3. **Proto compilation errors**:
   - Run `dotnet build` to regenerate gRPC client code
   - Ensure `Grpc.Tools` package is installed
   - Verify proto file syntax

## Related Documentation

- [AI Service Integration Contract](../../docs/ai-service-integration.md)
- [Prediction Aggregate Documentation](../../docs/prediction-aggregate.md)
- [Background Jobs Configuration](../../docs/background-jobs.md)
