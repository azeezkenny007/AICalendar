using System.Text.Json;
using Microsoft.Data.SqlClient;

Console.WriteLine("==============================================");
Console.WriteLine("Transaction Database Seeder");
Console.WriteLine("==============================================\n");

// Configuration
var jsonFilePath = @"c:\Users\Purple_Serve\Downloads\Transactions_Table_Updated_v2_fixed.json";

// Determine connection string
var connectionString = "Server=localhost;Database=AICalendarDb;Integrated Security=True;TrustServerCertificate=True;";

// Check for environment variable first
var envPassword = Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD");
if (!string.IsNullOrWhiteSpace(envPassword))
{
    connectionString = $"Server=localhost;Database=AICalendarDb;User Id=sa;Password={envPassword};TrustServerCertificate=True;";
    Console.WriteLine("Using SQL Server Authentication (from environment variable)");
}
else
{
    // Check if running interactively
    if (Environment.UserInteractive && !Console.IsInputRedirected)
    {
        Console.Write("Enter SQL Server SA password (leave empty for Windows Authentication): ");
        var password = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(password))
        {
            connectionString = $"Server=localhost;Database=AICalendarDb;User Id=sa;Password={password};TrustServerCertificate=True;";
            Console.WriteLine("Using SQL Server Authentication");
        }
        else
        {
            Console.WriteLine("Using Windows Authentication");
        }
    }
    else
    {
        Console.WriteLine("Using Windows Authentication (non-interactive mode)");
    }
}

// Verify JSON file exists
if (!File.Exists(jsonFilePath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: JSON file not found at {jsonFilePath}");
    Console.ResetColor();
    return;
}

Console.WriteLine($"Reading transactions from: {jsonFilePath}");

try
{
    // Read and deserialize JSON
    var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
    var transactions = JsonSerializer.Deserialize<List<TransactionDto>>(jsonContent);

    if (transactions == null || transactions.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Warning: No transactions found in JSON file.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"Found {transactions.Count} transactions in JSON file.\n");

    // Connect to database
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    Console.WriteLine("Connected to database successfully.");

    // Begin transaction
    using var dbTransaction = connection.BeginTransaction();

    try
    {
        // Step 1: Delete existing transactions
        Console.WriteLine("\nStep 1: Deleting existing transactions...");
        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = dbTransaction;
            deleteCommand.CommandText = "DELETE FROM Transactions";
            var deletedRows = await deleteCommand.ExecuteNonQueryAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Deleted {deletedRows} existing transactions.");
            Console.ResetColor();
        }

        // Step 2: Insert new transactions
        Console.WriteLine("\nStep 2: Inserting new transactions...");
        var insertedCount = 0;
        var batchSize = 100;

        using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.Transaction = dbTransaction;
            insertCommand.CommandText = @"
                INSERT INTO Transactions (Id, UserId, Amount, Description, Type, TransactionDate, CreatedAt, IsDiscarded, IsKept, MerchantId, ReceiverId)
                VALUES (@Id, @UserId, @Amount, @Description, @Type, @TransactionDate, @CreatedAt, @IsDiscarded, @IsKept, @MerchantId, @ReceiverId)";

            insertCommand.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier);
            insertCommand.Parameters.Add("@UserId", System.Data.SqlDbType.UniqueIdentifier);
            insertCommand.Parameters.Add("@Amount", System.Data.SqlDbType.Decimal);
            insertCommand.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 500);
            insertCommand.Parameters.Add("@Type", System.Data.SqlDbType.NVarChar, 50);
            insertCommand.Parameters.Add("@TransactionDate", System.Data.SqlDbType.DateTime);
            insertCommand.Parameters.Add("@CreatedAt", System.Data.SqlDbType.DateTime);
            insertCommand.Parameters.Add("@IsDiscarded", System.Data.SqlDbType.Bit);
            insertCommand.Parameters.Add("@IsKept", System.Data.SqlDbType.Bit);
            insertCommand.Parameters.Add("@MerchantId", System.Data.SqlDbType.NVarChar, 100);
            insertCommand.Parameters.Add("@ReceiverId", System.Data.SqlDbType.NVarChar, 100);

            foreach (var transaction in transactions)
            {
                insertCommand.Parameters["@Id"].Value = Guid.Parse(transaction.Id);
                insertCommand.Parameters["@UserId"].Value = Guid.Parse(transaction.UserId);
                insertCommand.Parameters["@Amount"].Value = transaction.Amount;
                insertCommand.Parameters["@Description"].Value = transaction.Description;
                insertCommand.Parameters["@Type"].Value = transaction.Type;
                insertCommand.Parameters["@TransactionDate"].Value = DateTime.Parse(transaction.TransactionDate);
                insertCommand.Parameters["@CreatedAt"].Value = DateTime.Parse(transaction.CreatedAt);
                insertCommand.Parameters["@IsDiscarded"].Value = transaction.IsDiscarded == 1;
                insertCommand.Parameters["@IsKept"].Value = transaction.IsKept == 1;
                insertCommand.Parameters["@MerchantId"].Value = string.IsNullOrWhiteSpace(transaction.MerchantId) || transaction.MerchantId == "NULL"
                    ? DBNull.Value
                    : transaction.MerchantId;
                insertCommand.Parameters["@ReceiverId"].Value = string.IsNullOrWhiteSpace(transaction.ReceiverId) || transaction.ReceiverId == "NULL"
                    ? DBNull.Value
                    : transaction.ReceiverId;

                await insertCommand.ExecuteNonQueryAsync();
                insertedCount++;

                // Show progress
                if (insertedCount % batchSize == 0)
                {
                    Console.Write($"\rInserted {insertedCount}/{transactions.Count} transactions...");
                }
            }

            Console.Write($"\rInserted {insertedCount}/{transactions.Count} transactions...");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Successfully inserted {insertedCount} transactions.");
            Console.ResetColor();
        }

        // Step 3: Verify the data
        Console.WriteLine("\nStep 3: Verifying transaction data...");
        using (var verifyCommand = connection.CreateCommand())
        {
            verifyCommand.Transaction = dbTransaction;
            verifyCommand.CommandText = @"
                SELECT
                    UserId,
                    COUNT(*) AS TransactionCount,
                    MIN(TransactionDate) AS FirstTransaction,
                    MAX(TransactionDate) AS LastTransaction,
                    SUM(Amount) AS TotalAmount
                FROM Transactions
                GROUP BY UserId
                ORDER BY UserId";

            using var reader = await verifyCommand.ExecuteReaderAsync();
            Console.WriteLine("\nSummary by User:");
            Console.WriteLine(new string('-', 120));
            Console.WriteLine($"{"UserId",-40} {"Count",-10} {"First Transaction",-20} {"Last Transaction",-20} {"Total Amount",-20}");
            Console.WriteLine(new string('-', 120));

            while (await reader.ReadAsync())
            {
                var userId = reader.GetGuid(0);
                var count = reader.GetInt32(1);
                var firstDate = reader.GetDateTime(2).ToString("yyyy-MM-dd");
                var lastDate = reader.GetDateTime(3).ToString("yyyy-MM-dd");
                var totalAmount = reader.GetDecimal(4);

                Console.WriteLine($"{userId,-40} {count,-10} {firstDate,-20} {lastDate,-20} {totalAmount,-20:N2}");
            }
            Console.WriteLine(new string('-', 120));
        }

        // Commit transaction
        await dbTransaction.CommitAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✓ Transaction seeding completed successfully!");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        // Rollback on error
        await dbTransaction.RollbackAsync();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n✗ Error occurred during seeding: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        Console.ResetColor();
        throw;
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n✗ Fatal error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    Console.ResetColor();
}

Console.WriteLine("\nSeeding completed. Exiting...");

// DTO class for JSON deserialization
public class TransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public int IsDiscarded { get; set; }
    public int IsKept { get; set; }
    public string? MerchantId { get; set; }
    public string? ReceiverId { get; set; }
}
