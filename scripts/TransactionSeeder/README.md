# Transaction Database Seeder

This tool seeds transaction data from a JSON file into your AICalendar database.

## Prerequisites

- .NET 8.0 SDK installed
- SQL Server running locally
- Transaction JSON file at: `c:\Users\Purple_Serve\Downloads\Transactions_Table_Updated_v2_fixed.json`

## How to Run

### Option 1: Using Windows Authentication (Recommended)
```bash
cd c:\Users\Purple_Serve\Desktop\AICalendar\scripts\TransactionSeeder
dotnet run
# When prompted, press Enter (leave password empty)
```

### Option 2: Using SQL Server Authentication
```bash
cd c:\Users\Purple_Serve\Desktop\AICalendar\scripts\TransactionSeeder
dotnet run
# When prompted, enter your SA password
```

### Option 3: Set Environment Variable
```bash
set SQLSERVER_PASSWORD=YourPasswordHere
cd c:\Users\Purple_Serve\Desktop\AICalendar\scripts\TransactionSeeder
dotnet run
```

## What It Does

1. **Deletes** all existing transactions from the database
2. **Reads** transaction data from the JSON file
3. **Inserts** all transactions from the JSON file
4. **Verifies** the data and displays a summary by user

## Output

The tool will display:
- Number of transactions found in JSON
- Number of existing transactions deleted
- Progress of insertion
- Summary table showing transaction count, date range, and total amount per user
- Success/error status

## Notes

- The operation runs in a database transaction - if anything fails, all changes are rolled back
- All existing transactions will be deleted before inserting new ones
- The JSON file must contain valid transaction data matching the expected schema
