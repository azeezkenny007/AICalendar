using AICalendar.Domain.Entities;
using AICalendar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

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
                Username = "john_doe",
                Email = "john.doe@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = UserId.Create(User2Id),
                Username = "jane_smith",
                Email = "jane.smith@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = UserId.Create(User3Id),
                Username = "bob_johnson",
                Email = "bob.johnson@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = UserId.Create(User4Id),
                Username = "alice_williams",
                Email = "alice.williams@example.com",
                CreatedAt = FourMonthsAgo,
                UpdatedAt = (DateTime?)null
            }
        };

        modelBuilder.Entity<User>().HasData(users);
    }

    private static void SeedTransactions(ModelBuilder modelBuilder)
    {
        var random = new Random(12345); // Fixed seed for consistent data
        var transactionTypes = Enum.GetValues<TransactionType>();

        var transactions = new List<object>();
        int transactionCounter = 1;

        // Create 100 transactions for each user starting from 4 months ago
        foreach (var userId in new[] { User1Id, User2Id, User3Id, User4Id })
        {
            var startDate = FourMonthsAgo;

            for (int i = 0; i < 100; i++)
            {
                var transactionId = Guid.NewGuid();
                var randomType = transactionTypes[random.Next(transactionTypes.Length)];
                var amount = GetAmountForTransactionType(randomType, random);
                var description = GetDescriptionForTransactionType(randomType, i + 1);

                // Spread transactions over 4 months (approximately 3 days apart)
                var transactionDate = startDate.AddDays(i * 1.2);

                transactions.Add(new
                {
                    Id = TransactionId.Create(transactionId),
                    UserId = UserId.Create(userId),
                    Amount = amount,
                    Description = description,
                    Type = randomType,
                    TransactionDate = transactionDate,
                    CreatedAt = transactionDate
                });

                transactionCounter++;
            }
        }

        modelBuilder.Entity<Transaction>().HasData(transactions);
    }

    private static decimal GetAmountForTransactionType(TransactionType type, Random random)
    {
        return type switch
        {
            TransactionType.BillPayment => Math.Round((decimal)(random.NextDouble() * 200 + 50), 2), // $50-$250
            TransactionType.Savings => Math.Round((decimal)(random.NextDouble() * 500 + 100), 2), // $100-$600
            TransactionType.Subscriptions => Math.Round((decimal)(random.NextDouble() * 30 + 5), 2), // $5-$35
            TransactionType.Transfer => Math.Round((decimal)(random.NextDouble() * 1000 + 100), 2), // $100-$1100
            TransactionType.Travel => Math.Round((decimal)(random.NextDouble() * 500 + 100), 2), // $100-$600
            TransactionType.GymPayment => Math.Round((decimal)(random.NextDouble() * 80 + 20), 2), // $20-$100
            _ => 0m
        };
    }

    private static string GetDescriptionForTransactionType(TransactionType type, int number)
    {
        return type switch
        {
            TransactionType.BillPayment => $"Utility Bill Payment #{number}",
            TransactionType.Savings => $"Monthly Savings Transfer #{number}",
            TransactionType.Subscriptions => $"Subscription Service #{number}",
            TransactionType.Transfer => $"Bank Transfer #{number}",
            TransactionType.Travel => $"Travel Expense #{number}",
            TransactionType.GymPayment => $"Gym Membership #{number}",
            _ => $"Transaction #{number}"
        };
    }
}
