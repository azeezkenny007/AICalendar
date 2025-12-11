using AICalendar.Domain.Entities;
using AICalendar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AICalendar.Infrastructure.Data.SeedData;

public static class UserTransactionSeedData
{
    private static readonly DateTime FourMonthsAgo = DateTime.UtcNow.AddMonths(-4);

    private static readonly Guid User1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid User2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid User3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid User4Id = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static void SeedUserAndTransactionData(ModelBuilder modelBuilder)
    {
        SeedUsers(modelBuilder);
        SeedTransactions(modelBuilder);
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        var users = new[]
        {
            new
            {
                Id = UserId.Create(User1Id),
                FirstName = "Chukwuemeka",
                LastName = "Okonkwo",
                Username = "chukwuemeka_okonkwo",
                Email = "chukwuemeka.okonkwo@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = UserId.Create(User2Id),
                FirstName = "Aisha",
                LastName = "Bello",
                Username = "aisha_bello",
                Email = "aisha.bello@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = UserId.Create(User3Id),
                FirstName = "Oluwaseun",
                LastName = "Adeyemi",
                Username = "oluwaseun_adeyemi",
                Email = "oluwaseun.adeyemi@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = UserId.Create(User4Id),
                FirstName = "Ngozi",
                LastName = "Okeke",
                Username = "ngozi_okeke",
                Email = "ngozi.okeke@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            }
        };

        modelBuilder.Entity<User>().HasData(users);
    }

    private static void SeedTransactions(ModelBuilder modelBuilder)
    {
        var transactions = new List<object>();

        // Try to find the file - check multiple possible locations (Docker and local)
        var possiblePaths = new[]
        {
            // Primary location: relative to application base directory (works in published apps)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "transactions.json"),

            // Fallback for development: current working directory
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "SeedData", "transactions.json"),

            // Docker container path (if source is mounted)
            "/src/src/AICalendar.Infrastructure/Data/SeedData/transactions.json",

            // Additional fallback paths
            Path.Combine(Directory.GetCurrentDirectory(), "..", "AICalendar.Infrastructure", "Data", "SeedData", "transactions.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "transactions.json"),

            // Absolute path (Windows development only)
            @"c:\Users\Purple_Serve\Desktop\AICalendar\src\AICalendar.Infrastructure\Data\SeedData\transactions.json"
        };

        string? foundPath = null;
        foreach (var path in possiblePaths)
        {
            try
            {
                var normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    foundPath = normalizedPath;
                    break;
                }
            }
            catch
            {
                // Skip invalid paths
                continue;
            }
        }

        if (foundPath == null)
        {
            var searchedPaths = string.Join("\n  - ", possiblePaths.Select(p => {
                try { return Path.GetFullPath(p); } catch { return p; }
            }));
            throw new FileNotFoundException(
                $"Transaction seed data JSON file not found!\n\nSearched in the following locations:\n  - {searchedPaths}\n\n" +
                $"Current Directory: {Directory.GetCurrentDirectory()}\n" +
                $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}\n\n" +
                "Please ensure transactions.json exists in the Data/SeedData directory and is copied to output.");
        }

        try
        {
            var jsonContent = File.ReadAllText(foundPath);
            var transactionDtos = JsonSerializer.Deserialize<List<TransactionDto>>(jsonContent);

            if (transactionDtos == null || transactionDtos.Count == 0)
            {
                throw new InvalidOperationException("Transaction JSON file is empty or invalid!");
            }

            foreach (var dto in transactionDtos)
            {
                // Parse and validate transaction type
                if (!Enum.TryParse<TransactionType>(dto.Type, out var transactionType))
                {
                    Console.WriteLine($"Warning: Invalid transaction type '{dto.Type}' for transaction {dto.Id}. Skipping...");
                    continue;
                }

                transactions.Add(new
                {
                    Id = TransactionId.Create(Guid.Parse(dto.Id)),
                    UserId = UserId.Create(Guid.Parse(dto.UserId)),
                    Amount = dto.Amount,
                    Description = dto.Description,
                    Type = transactionType,
                    TransactionDate = DateTime.Parse(dto.TransactionDate),
                    CreatedAt = DateTime.Parse(dto.CreatedAt),
                    IsKept = dto.IsKept == 1,
                    IsDiscarded = dto.IsDiscarded == 1,
                    ReceiverId = string.IsNullOrWhiteSpace(dto.ReceiverId) || dto.ReceiverId == "NULL" ? null : dto.ReceiverId,
                    MerchantId = string.IsNullOrWhiteSpace(dto.MerchantId) || dto.MerchantId == "NULL" ? null : dto.MerchantId
                });
            }

            Console.WriteLine($"✓ Successfully loaded {transactions.Count} transactions from JSON file: {foundPath}");
            modelBuilder.Entity<Transaction>().HasData(transactions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load transaction seed data from JSON file: {ex.Message}", ex);
        }
    }


    // DTO class for JSON deserialization
    private class TransactionDto
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
}
