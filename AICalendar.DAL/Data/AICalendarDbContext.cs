using AICalendar.CORE.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.DAL.Data;

public class AICalendarDbContext : DbContext
{
    public AICalendarDbContext(DbContextOptions<AICalendarDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from separate files
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AICalendarDbContext).Assembly);

        // Seed data
        var user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = user1Id,
                Email = "chidi.okonkwo@example.com",
                Username = "chidiokonkwo",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            },
            new User
            {
                Id = user2Id,
                Email = "amina.bello@example.com",
                Username = "aminabello",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            }
        );
        

        var transactions = new List<Transaction>();
        var random = new Random(42);

        // Use current date as reference and go back 90 days
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-90);

        // Recurring payment definitions (description, recipient, amount in Naira, dayOfMonth/interval)
        var recurringSubscriptions = new[]
        {
            ("Netflix Subscription", "Netflix Inc.", 5000m, 1),       // 1st of each month
            ("Spotify Premium", "Spotify AB", 2000m, 5),              // 5th of each month
            ("Amazon Prime", "Amazon.com", 3500m, 10),                // 10th of each month
            ("YouTube Premium", "Google LLC", 2500m, 15),             // 15th of each month
            ("Apple Music", "Apple Inc.", 1800m, 20)                  // 20th of each month
        };

        var recurringBills = new[]
        {
            ("Electricity Bill", "IKEDC", 8000m, 25),                 // 25th of each month (will vary widely)
            ("Internet Subscription", "Airtel", 12000m, 3),           // 3rd of each month
            ("Water Bill", "Lagos Water Corp", 2000m, 28)             // 28th of each month
        };

        var recurringTransfers = new[]
        {
            ("Money to Parents", "Mama & Papa", 50000m, 23),          // 23rd of each month
            ("Family Support", "Siblings", 30000m, 23)                // 23rd of each month
        };

        var recurringSavings = new[]
        {
            ("Monthly Savings", "Savings Account", 100000m, 5),       // 5th of each month
            ("Investment Plan", "Investment Account", 75000m, 15)     // 15th of each month
        };

        var weeklyRecurring = new[]
        {
            ("Data Bundle", "MTN", 2000m, DayOfWeek.Monday),
            ("Airtime Purchase", "Glo", 1500m, DayOfWeek.Friday)
        };

        // Generate recurring transactions for user 1
        foreach (var sub in recurringSubscriptions)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(sub.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user1Id,
                        Type = TransactionType.Subscription,
                        Amount = sub.Item3,
                        Description = sub.Item1,
                        Recipient = sub.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(sub.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        foreach (var bill in recurringBills)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(bill.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            var billAmount = bill.Item3;

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    // Add variation to bill amounts - electricity varies widely (₦400 to ₦25000), others slightly
                    var variation = bill.Item1.Contains("Electricity")
                        ? (decimal)(random.NextDouble() * 24600 - 7600)  // Range: ₦400 - ₦25,000
                        : (decimal)(random.NextDouble() * 2000 - 1000);  // ±₦1000 for other bills

                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user1Id,
                        Type = TransactionType.BillPayment,
                        Amount = Math.Round(Math.Max(400, billAmount + variation), 2), // Ensure minimum ₦400
                        Description = bill.Item1,
                        Recipient = bill.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(bill.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        // Generate recurring transfers (money to parents/family) for user 1
        foreach (var transfer in recurringTransfers)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(transfer.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user1Id,
                        Type = TransactionType.Transfer,
                        Amount = transfer.Item3,
                        Description = transfer.Item1,
                        Recipient = transfer.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(transfer.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        // Generate recurring savings for user 1
        foreach (var saving in recurringSavings)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(saving.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user1Id,
                        Type = TransactionType.Savings,
                        Amount = saving.Item3,
                        Description = saving.Item1,
                        Recipient = saving.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(saving.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        // Generate weekly recurring transactions for user 1
        foreach (var weekly in weeklyRecurring)
        {
            var currentDate = startDate;
            while (currentDate.DayOfWeek != weekly.Item4)
            {
                currentDate = currentDate.AddDays(1);
            }

            while (currentDate <= endDate)
            {
                transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(),
                    UserId = user1Id,
                    Type = TransactionType.BillPayment,
                    Amount = weekly.Item3,
                    Description = weekly.Item1,
                    Recipient = weekly.Item2,
                    TransactionDate = currentDate,
                    CreatedAt = currentDate,
                    UpdatedAt = null
                });

                currentDate = currentDate.AddDays(7);
            }
        }

        // Generate some random one-time transactions for user 1 (amounts in Naira)
        var oneTimeData = new Dictionary<TransactionType, (string[] descriptions, string[] recipients, decimal minAmount, decimal maxAmount)>
        {
            { TransactionType.Transfer, (
                new[] { "Transfer to Friend", "Family Transfer", "Money Transfer" },
                new[] { "Chidi Okafor", "Amina Hassan", "Tunde Adeyemi", "Ngozi Eze", "Yusuf Mohammed" },
                5000m, 150000m
            )},
            { TransactionType.Savings, (
                new[] { "Monthly Savings", "Emergency Fund", "Investment Deposit", "Savings Transfer" },
                new[] { "Savings Account", "Fixed Deposit", "Money Market Fund", "Investment Account" },
                20000m, 300000m
            )},
            { TransactionType.BillPayment, (
                new[] { "Cable TV Subscription", "Waste Management", "Security Service", "Internet Top-up" },
                new[] { "DSTV", "GOTV", "Startimes", "Waste Disposal Inc", "Security Plus" },
                3000m, 25000m
            )}
        };

        // Generate random one-time transactions: BillPayment, Transfer, and Savings
        var transactionTypes = new[] { TransactionType.BillPayment, TransactionType.Transfer, TransactionType.Savings };

        for (int i = 0; i < 50; i++)
        {
            var daysOffset = random.Next(0, 90);
            var transactionType = transactionTypes[random.Next(transactionTypes.Length)];
            var data = oneTimeData[transactionType];
            var descIndex = random.Next(data.descriptions.Length);
            var recipientIndex = random.Next(data.recipients.Length);

            transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = user1Id,
                Type = transactionType,
                Amount = Math.Round((decimal)(random.NextDouble() * (double)(data.maxAmount - data.minAmount) + (double)data.minAmount), 2),
                Description = data.descriptions[descIndex],
                Recipient = data.recipients[recipientIndex],
                TransactionDate = startDate.AddDays(daysOffset),
                CreatedAt = startDate.AddDays(daysOffset),
                UpdatedAt = null
            });
        }

        // Generate recurring transactions for user 2 (different subscriptions) - amounts in Naira
        var user2Subscriptions = new[]
        {
            ("Netflix Subscription", "Netflix Inc.", 5000m, 3),
            ("Disney Plus", "Disney Inc.", 3200m, 7),
            ("Office 365", "Microsoft Corp.", 4500m, 12)
        };

        var user2Bills = new[]
        {
            ("Electricity Bill", "EKEDC", 10000m, 26),                // Will vary widely
            ("Phone Bill", "9Mobile", 8000m, 2),
            ("Gym Membership", "Planet Fitness", 15000m, 1)
        };

        var user2RecurringTransfers = new[]
        {
            ("Money to Parents", "Family", 40000m, 20)                // 20th of each month
        };

        var user2RecurringSavings = new[]
        {
            ("Monthly Savings", "Savings Account", 80000m, 10)        // 10th of each month
        };

        foreach (var sub in user2Subscriptions)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(sub.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user2Id,
                        Type = TransactionType.Subscription,
                        Amount = sub.Item3,
                        Description = sub.Item1,
                        Recipient = sub.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(sub.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        foreach (var bill in user2Bills)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(bill.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            var billAmount = bill.Item3;

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    // Add variation to bill amounts - electricity varies widely (₦400 to ₦25000), others slightly
                    var variation = bill.Item1.Contains("Electricity")
                        ? (decimal)(random.NextDouble() * 24600 - 9600)  // Range: ₦400 - ₦25,000
                        : (decimal)(random.NextDouble() * 2000 - 1000);  // ±₦1000 for other bills

                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user2Id,
                        Type = TransactionType.BillPayment,
                        Amount = Math.Round(Math.Max(400, billAmount + variation), 2), // Ensure minimum ₦400
                        Description = bill.Item1,
                        Recipient = bill.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(bill.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        // Generate recurring transfers for user 2
        foreach (var transfer in user2RecurringTransfers)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(transfer.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user2Id,
                        Type = TransactionType.Transfer,
                        Amount = transfer.Item3,
                        Description = transfer.Item1,
                        Recipient = transfer.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(transfer.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        // Generate recurring savings for user 2
        foreach (var saving in user2RecurringSavings)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, Math.Min(saving.Item4, DateTime.DaysInMonth(startDate.Year, startDate.Month)), 0, 0, 0, DateTimeKind.Utc);

            while (currentDate <= endDate)
            {
                if (currentDate >= startDate)
                {
                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user2Id,
                        Type = TransactionType.Savings,
                        Amount = saving.Item3,
                        Description = saving.Item1,
                        Recipient = saving.Item2,
                        TransactionDate = currentDate,
                        CreatedAt = currentDate,
                        UpdatedAt = null
                    });
                }

                currentDate = currentDate.AddMonths(1);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(saving.Item4, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)), 0, 0, 0, DateTimeKind.Utc);
            }
        }

        // Generate random transactions for user 2
        for (int i = 0; i < 50; i++)
        {
            var daysOffset = random.Next(0, 90);
            var transactionType = transactionTypes[random.Next(transactionTypes.Length)];
            var data = oneTimeData[transactionType];
            var descIndex = random.Next(data.descriptions.Length);
            var recipientIndex = random.Next(data.recipients.Length);

            transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = user2Id,
                Type = transactionType,
                Amount = Math.Round((decimal)(random.NextDouble() * (double)(data.maxAmount - data.minAmount) + (double)data.minAmount), 2),
                Description = data.descriptions[descIndex],
                Recipient = data.recipients[recipientIndex],
                TransactionDate = startDate.AddDays(daysOffset),
                CreatedAt = startDate.AddDays(daysOffset),
                UpdatedAt = null
            });
        }

        modelBuilder.Entity<Transaction>().HasData(transactions);
    }
}
