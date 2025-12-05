# ALAT Predictive Calendar - API Documentation

**Version**: 1.0.0  
**Last Updated**: December 2025  
**Team**: AI/ML Engineering

---

## Overview

The Predictive Calendar API analyzes users' transaction history to generate personalized calendar suggestions for recurring payments. The service identifies patterns in past transactions and predicts future occurrences.

### Key Features

- Detects recurring patterns (monthly, weekly, bi-weekly)
- Groups transactions by **Receiver ID** (merchant/recipient), not description
- Returns up to 10 predictions per request
- Learns from user feedback to improve accuracy

### Base URL

```
https://{FUNCTION_APP_NAME}.azurewebsites.net/api
```

### Authentication

All requests require the function key as a query parameter:

```
?code={FUNCTION_KEY}
```

---

## Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/predictions` | Generate predictions for a user |
| POST | `/feedback` | Submit user feedback (Keep/Discard/Edit) |

---

## Endpoint 1: Generate Predictions

Analyzes user's transaction history and returns up to 10 predicted calendar items for the specified month.

### Request

```
POST /api/predictions?code={FUNCTION_KEY}
Content-Type: application/json
```

### Request Body

```json
{
    "user_id": "11111111-1111-1111-1111-111111111111",
    "target_month": "2025-12",
    "historical_transactions": [
        {
            "Id": "A853B38D-9AEA-4D78-A933-0076172015E9",
            "UserId": "11111111-1111-1111-1111-111111111111",
            "Amount": 15000.00,
            "Description": "Electricity Payment - November",
            "Type": "BillPayment",
            "ReceiverId": "EKEDC",
            "TransactionDate": "2025-11-15T14:19:48",
            "CreatedAt": "2025-11-15T14:19:48"
        },
        {
            "Id": "B964C49E-0BFB-5E89-B044-1187DC2B80FA",
            "UserId": "11111111-1111-1111-1111-111111111111",
            "Amount": 15200.00,
            "Description": "EKEDC Bill October",
            "Type": "BillPayment",
            "ReceiverId": "EKEDC",
            "TransactionDate": "2025-10-15T10:30:00",
            "CreatedAt": "2025-10-15T10:30:00"
        }
    ],
    "max_predictions": 10
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `user_id` | string | Yes | Unique identifier for the user |
| `target_month` | string | Yes | Month to generate predictions for (format: `YYYY-MM`) |
| `historical_transactions` | array | Yes | Array of past transactions (minimum 2-3 months recommended) |
| `max_predictions` | integer | No | Maximum predictions to return (default: 10, max: 10) |

### Transaction Object Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Id` | string | Yes | Unique transaction ID |
| `UserId` | string | Yes | User who made the transaction |
| `Amount` | number | Yes | Transaction amount in Naira |
| `Description` | string | Yes | Transaction description |
| `Type` | string | Yes | Transaction type (see enum below) |
| `ReceiverId` | string | Yes | **Merchant/Recipient ID** - This is the key field for grouping |
| `TransactionDate` | string | Yes | When transaction occurred (ISO 8601) |
| `CreatedAt` | string | Yes | When record was created (ISO 8601) |

### Response

```json
{
    "status": "SUCCESS",
    "predictions": [
        {
            "prediction_id": "pred_11111111_202512_0001",
            "title": "Payment to EKEDC",
            "description": "Payment to EKEDC",
            "category": "BILL_PAYMENT",
            "predicted_date": "2025-12-15T00:00:00",
            "predicted_amount": 15100,
            "currency": "NGN",
            "confidence_score": 0.847,
            "reasoning": "Based on 3 monthly transactions with an average of ₦15,100.00. Typically occurs around day 15 of the month.",
            "pattern_type": "MONTHLY",
            "source_transaction_ids": ["A853B38D-...", "B964C49E-...", "C075D50F-..."],
            "reminder_hours_before": 24
        },
        {
            "prediction_id": "pred_11111111_202512_0002",
            "title": "Payment to ALAT_LOANS",
            "description": "Payment to ALAT_LOANS",
            "category": "LOAN_REPAYMENT",
            "predicted_date": "2025-12-25T00:00:00",
            "predicted_amount": 50000,
            "currency": "NGN",
            "confidence_score": 0.92,
            "reasoning": "Based on 3 monthly transactions with an average of ₦50,000.00. Typically occurs around day 25 of the month.",
            "pattern_type": "MONTHLY",
            "source_transaction_ids": ["D186E61G-...", "E297F72H-..."],
            "reminder_hours_before": 48
        }
    ],
    "metadata": {
        "processing_time_ms": 234,
        "transactions_analyzed": 45,
        "patterns_detected": 6,
        "model_version": "1.0.0",
        "generated_at": "2025-12-03T10:30:00.000000"
    },
    "error_message": null
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `status` | string | Request status (see Status enum) |
| `predictions` | array | Array of predicted items (max 10) |
| `metadata` | object | Processing metadata |
| `error_message` | string | Error details if status is not SUCCESS |

### Prediction Object Fields

| Field | Type | Description |
|-------|------|-------------|
| `prediction_id` | string | Unique ID for this prediction (use in feedback) |
| `title` | string | Human-readable title |
| `description` | string | Detailed description |
| `category` | string | Payment category (see Category enum) |
| `predicted_date` | string | Predicted date (ISO 8601) |
| `predicted_amount` | integer | Predicted amount in Naira |
| `currency` | string | Currency code (always "NGN") |
| `confidence_score` | number | Confidence level (0.0 to 1.0) |
| `reasoning` | string | Human-readable explanation |
| `pattern_type` | string | Type of pattern detected (see Pattern Type enum) |
| `source_transaction_ids` | array | Transaction IDs that led to this prediction |
| `reminder_hours_before` | integer | Suggested reminder time (hours before due) |

### HTTP Status Codes

| Code | Status | Description |
|------|--------|-------------|
| 200 | SUCCESS | Predictions generated successfully |
| 400 | INVALID_REQUEST | Missing or invalid parameters |
| 422 | INSUFFICIENT_DATA | Not enough transaction history |
| 500 | FAILURE | Internal server error |

---

## Endpoint 2: Submit Feedback

Receives user feedback on predictions (Keep/Discard/Edit) to improve future predictions.

### Request

```
POST /api/feedback?code={FUNCTION_KEY}
Content-Type: application/json
```

### Request Body

```json
{
    "user_id": "11111111-1111-1111-1111-111111111111",
    "feedback_items": [
        {
            "prediction_id": "pred_11111111_202512_0001",
            "action": "KEPT",
            "prediction_details": {
                "category": "BILL_PAYMENT",
                "predicted_amount": 15100
            }
        },
        {
            "prediction_id": "pred_11111111_202512_0002",
            "action": "DISCARDED",
            "discard_reason": "I paid off this loan already"
        },
        {
            "prediction_id": "pred_11111111_202512_0003",
            "action": "EDITED",
            "edited_data": {
                "corrected_title": "Internet Bill",
                "corrected_amount": 18000,
                "corrected_date": "2025-12-20T00:00:00",
                "corrected_category": "BILL_PAYMENT"
            },
            "prediction_details": {
                "category": "OTHER",
                "predicted_amount": 15000
            }
        }
    ]
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `user_id` | string | Yes | User providing feedback |
| `feedback_items` | array | Yes | Array of feedback items |

### Feedback Item Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `prediction_id` | string | Yes | ID of the prediction (from predictions response) |
| `action` | string | Yes | User's action: `KEPT`, `DISCARDED`, or `EDITED` |
| `prediction_details` | object | No | Original prediction details (helps with learning) |
| `discard_reason` | string | No | Why user discarded (for DISCARDED action) |
| `edited_data` | object | No | User's corrections (for EDITED action) |

### Edited Data Fields (for EDITED action)

| Field | Type | Description |
|-------|------|-------------|
| `corrected_title` | string | User-corrected title |
| `corrected_amount` | integer | User-corrected amount |
| `corrected_date` | string | User-corrected date (ISO 8601) |
| `corrected_category` | string | User-corrected category |

### Response

```json
{
    "status": "SUCCESS",
    "processed_count": 3,
    "failed_items": [],
    "message": "Processed 3 feedback items",
    "retraining_triggered": false
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `status` | string | Request status |
| `processed_count` | integer | Number of items successfully processed |
| `failed_items` | array | Items that failed to process (with reasons) |
| `message` | string | Summary message |
| `retraining_triggered` | boolean | Whether model retraining was triggered |

---

## Enums Reference

### Status

| Value | Description |
|-------|-------------|
| `SUCCESS` | Request completed successfully |
| `PARTIAL_SUCCESS` | Some items succeeded, some failed |
| `FAILURE` | Internal server error |
| `INVALID_REQUEST` | Missing or invalid parameters |
| `USER_NOT_FOUND` | User ID not found |
| `INSUFFICIENT_DATA` | Not enough transaction history to make predictions |

### Category

| Value | Description |
|-------|-------------|
| `BILL_PAYMENT` | Electricity, water, waste management |
| `SUBSCRIPTION` | Streaming services, memberships |
| `LOAN_REPAYMENT` | Monthly loan installments |
| `SAVINGS_CONTRIBUTION` | Recurring savings deposits |
| `INSURANCE_PREMIUM` | Insurance payments |
| `AIRTIME_DATA` | Phone recharges and data bundles |
| `TRANSFER` | Regular transfers to specific accounts |
| `DIRECT_DEBIT` | Automated debits |
| `INVESTMENT` | Investment purchases |
| `OTHER` | Unclassified transactions |

### Transaction Type

| Value | Maps To Category |
|-------|------------------|
| `BillPayment` | BILL_PAYMENT |
| `DirectDebit` | DIRECT_DEBIT |
| `LoanRepayment` | LOAN_REPAYMENT |
| `SavingsContribution` | SAVINGS_CONTRIBUTION |
| `InsurancePayment` | INSURANCE_PREMIUM |
| `AirtimeTopup` | AIRTIME_DATA |
| `DataPurchase` | AIRTIME_DATA |
| `TransferLocal` | TRANSFER |
| `TransferInternational` | TRANSFER |
| `InvestmentPurchase` | INVESTMENT |
| `VoucherRedeem` | OTHER |
| `CardIssue` | OTHER |
| `TravelBooking` | OTHER |
| `LoanDisbursement` | OTHER |

### Pattern Type

| Value | Description |
|-------|-------------|
| `MONTHLY` | Occurs once per month |
| `WEEKLY` | Occurs every week |
| `BI_WEEKLY` | Occurs every two weeks |
| `QUARTERLY` | Occurs every three months |
| `ANNUAL` | Occurs once per year |
| `IRREGULAR` | Recurring but no clear pattern |

### Feedback Action

| Value | Description |
|-------|-------------|
| `KEPT` | User accepted the prediction |
| `DISCARDED` | User rejected the prediction |
| `EDITED` | User modified the prediction |

---

## Important Notes

### About Receiver ID

The `ReceiverId` field is **critical** for accurate predictions. It identifies WHO the user is paying:

- **Bill payments**: Merchant ID (e.g., "EKEDC", "DSTV", "MTN")
- **Transfers**: Recipient's account number or user ID
- **Subscriptions**: Service provider ID

This ensures that payments to the same entity are grouped correctly, even if descriptions vary month to month.

### Minimum Data Requirements

- **Recommended**: 2-3 months of transaction history
- **Minimum**: At least 2 transactions to the same receiver to detect a pattern

### Performance

- Response time: < 5 seconds (typically 200-500ms)
- Maximum predictions: 10 per request

---

## Example: Full Flow

### Step 1: Get Predictions

```bash
curl -X POST "https://your-function.azurewebsites.net/api/predictions?code=YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "user_123",
    "target_month": "2025-12",
    "historical_transactions": [...]
  }'
```

### Step 2: User Reviews in App

User sees predictions in mobile app and taps:
- ✅ Keep (for predictions they want)
- ❌ Discard (for predictions they don't want)
- ✏️ Edit (to correct details)

### Step 3: Submit Feedback

```bash
curl -X POST "https://your-function.azurewebsites.net/api/feedback?code=YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "user_123",
    "feedback_items": [
      {"prediction_id": "pred_001", "action": "KEPT"},
      {"prediction_id": "pred_002", "action": "DISCARDED", "discard_reason": "Not needed"}
    ]
  }'
```

---

## Contact

For questions about this API, contact the AI/ML team.
