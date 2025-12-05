# AI Prediction Service - REST API Migration

**Date**: December 4, 2025
**Status**: ✅ Complete

## Overview

Successfully migrated the AI Prediction Service from gRPC to REST API to align with the AI team's deployed Azure Function endpoint.

## Changes Made

### 1. Removed gRPC Dependencies

**Files Modified:**
- `src/AICalendar.API/AICalendar.API.csproj`
- `src/AICalendar.Infrastructure/AICalendar.Infrastructure.csproj`

**Packages Removed:**
- `Grpc.Net.Client` (v2.59.0)
- `Grpc.Tools` (v2.59.0)
- `Google.Protobuf` (v3.25.1)

**Packages Added:**
- `Microsoft.Extensions.Http` (v8.0.0) - Infrastructure project only

**Files Deleted:**
- `src/AICalendar.API/Protos/predictive_calendar.proto`
- `src/AICalendar.API/Protos/` (directory)

### 2. Created REST API DTOs

**New File:** `src/AICalendar.Infrastructure/Services/AIPredictionModels.cs`

**DTOs Created:**
- `ApiPredictionRequest` - Request payload for predictions endpoint
- `ApiTransaction` - Transaction data in request
- `ApiPredictionResponse` - Response from predictions endpoint
- `ApiPredictionItem` - Individual prediction in response
- `ApiPredictionMetadata` - Metadata about prediction generation

**Key Features:**
- Uses `System.Text.Json` with `[JsonPropertyName]` attributes
- Matches the REST API specification exactly (snake_case field names)
- Includes `ReceiverId` field (critical for accurate predictions)
- Uses `decimal` for amounts (not cents/kobo as in gRPC)
- Uses ISO 8601 strings for dates

### 3. Rewrote AIPredictionService

**File Modified:** `src/AICalendar.Infrastructure/Services/AIPredictionService.cs`

**Changes:**
- Replaced gRPC client with `HttpClient`
- Updated constructor to inject `HttpClient` and read configuration
- Implemented REST API call with proper error handling
- Added `ReceiverId` mapping with fallback logic: `ReceiverId ?? MerchantId ?? "UNKNOWN"`
- Maps API DTOs to Application layer DTOs (`PredictedItemDto`, `PredictionMetadataDto`)
- Removed `Dispose()` method (no longer needed)

**Using Statements:**
- Removed: `Alat.PredictiveCalendar.V1`, `Google.Protobuf.WellKnownTypes`, `Grpc.Core`, `Grpc.Net.Client`
- Added: `System.Net.Http.Json`

### 4. Updated Dependency Injection

**File Modified:** `src/AICalendar.API/Program.cs`

**Change:**
```csharp
// Before
builder.Services.AddScoped<IAIPredictionService, AIPredictionService>();

// After
builder.Services.AddHttpClient<IAIPredictionService, AIPredictionService>();
```

**Benefits:**
- Proper `HttpClient` lifecycle management
- Connection pooling
- Follows best practices for HttpClient usage

### 5. Updated Configuration

**File Modified:** `src/AICalendar.API/appsettings.json`

**Changes:**
```json
{
  "AIService": {
    "Url": "http://localhost:7071/api",  // Changed from localhost:5001
    "Key": "YOUR_FUNCTION_KEY_HERE"      // Added for Azure Function authentication
  }
}
```

## Key Differences: gRPC vs REST

| Aspect | gRPC (Old) | REST (New) |
|--------|-----------|-----------|
| **Protocol** | HTTP/2 with Protobuf | HTTP/1.1 with JSON |
| **Authentication** | None (or metadata) | Query parameter `?code={key}` |
| **Amount Format** | `int64` (cents) | `decimal` (Naira) |
| **Date Format** | `Timestamp` (Protobuf) | ISO 8601 string |
| **ReceiverId** | ❌ Missing | ✅ Included |
| **Endpoint** | `PredictiveCalendarService/GeneratePredictions` | `POST /predictions` |

## Data Mapping

### ReceiverId Fallback Logic
```csharp
ReceiverId = t.ReceiverId ?? t.MerchantId ?? "UNKNOWN"
```

This ensures the AI service always receives a receiver identifier, which is critical for grouping recurring transactions correctly.

### Date Formatting
```csharp
TransactionDate = t.TransactionDate.ToString("yyyy-MM-ddTHH:mm:ss")
```

Produces ISO 8601 format: `2025-11-15T14:19:48`

### Amount Handling
- **Old (gRPC)**: `Amount = (long)(transaction.Amount * 100)` → Sent as kobo/cents
- **New (REST)**: `Amount = transaction.Amount` → Sent as decimal Naira

## Configuration Required

Before deploying, update `appsettings.json` (or environment variables) with:

1. **AIService:Url** - The Azure Function base URL (e.g., `https://your-function-app.azurewebsites.net/api`)
2. **AIService:Key** - The Azure Function key for authentication

## Testing Checklist

- [x] Build succeeds without errors
- [ ] Unit tests pass (if applicable)
- [ ] Integration test with AI service endpoint
- [ ] Verify predictions are generated correctly
- [ ] Verify `ReceiverId` is being sent
- [ ] Verify authentication works with Function Key
- [ ] Test error handling (401, 400, 500 responses)

## Notes

- **Feedback Endpoint**: Not implemented as per request (assigned to another developer)
- **Logging**: Enhanced logging remains in place from previous implementation
- **Backward Compatibility**: None - this is a breaking change requiring AI service to be deployed as REST API

## Related Documentation

- [AI Prediction Generation](./ai-prediction-generation.md) - Overall feature documentation
- [API Documentation](../../../Downloads/API_Doc.md) - REST API specification from AI team

## Deployment Steps

1. Ensure AI service is deployed as Azure Function with REST endpoints
2. Obtain the Function Key from Azure Portal
3. Update `appsettings.json` or environment variables with correct URL and Key
4. Deploy the updated application
5. Monitor logs for successful AI service calls
6. Verify predictions are being generated with correct `ReceiverId` grouping
