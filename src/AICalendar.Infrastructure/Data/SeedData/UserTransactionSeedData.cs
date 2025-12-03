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

                // Set ReceiverId and MerchantId based on transaction type
                var (receiverId, merchantId) = GetReceiverAndMerchantForType(randomType);

                transactions.Add(new
                {
                    Id = TransactionId.Create(transactionId),
                    UserId = UserId.Create(userId),
                    Amount = amount,
                    Description = description,
                    Type = randomType,
                    TransactionDate = transactionDate,
                    CreatedAt = transactionDate,
                    IsKept = false,
                    IsDiscarded = false,
                    ReceiverId = receiverId,
                    MerchantId = merchantId
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
            // Banking Transactions
            TransactionType.TransferLocal => Math.Round((decimal)(random.NextDouble() * 180000 + 5000), 2),
            TransactionType.TransferInternational => Math.Round((decimal)(random.NextDouble() * 450000 + 50000), 2),
            TransactionType.BillPayment => Math.Round((decimal)(random.NextDouble() * 35000 + 3000), 2),
            TransactionType.AirtimeTopup => Math.Round((decimal)(random.NextDouble() * 4000 + 500), 2),
            TransactionType.DataPurchase => Math.Round((decimal)(random.NextDouble() * 8000 + 1000), 2),
            TransactionType.LoanDisbursement => Math.Round((decimal)(random.NextDouble() * 800000 + 200000), 2),
            TransactionType.LoanRepayment => Math.Round((decimal)(random.NextDouble() * 150000 + 30000), 2),
            TransactionType.SavingsContribution => Math.Round((decimal)(random.NextDouble() * 200000 + 20000), 2),
            TransactionType.InvestmentPurchase => Math.Round((decimal)(random.NextDouble() * 450000 + 100000), 2),
            TransactionType.CardIssue => Math.Round((decimal)(random.NextDouble() * 3000 + 1000), 2),
            TransactionType.InsurancePayment => Math.Round((decimal)(random.NextDouble() * 50000 + 10000), 2),
            TransactionType.TravelBooking => Math.Round((decimal)(random.NextDouble() * 250000 + 50000), 2),
            TransactionType.VoucherRedeem => Math.Round((decimal)(random.NextDouble() * 20000 + 5000), 2),
            TransactionType.DirectDebit => Math.Round((decimal)(random.NextDouble() * 40000 + 5000), 2),

            _ => 0m
        };
    }

    private static string GetDescriptionForTransactionType(TransactionType type, int number)
    {
        return type switch
        {
            // Banking Transactions
            TransactionType.TransferLocal => $"Transfer to {GetRandomRecipient()} - {GetRandomBank()}",
            TransactionType.TransferInternational => $"International Transfer - {GetRandomCountry()}",
            TransactionType.BillPayment => $"{GetRandomBillType()} Payment - {GetRandomMonth()}",
            TransactionType.AirtimeTopup => $"{GetRandomTelecom()} Airtime Recharge",
            TransactionType.DataPurchase => $"{GetRandomTelecom()} Data Bundle - {GetRandomDataSize()}",
            TransactionType.LoanDisbursement => $"Loan Disbursement - {GetRandomLoanPurpose()}",
            TransactionType.LoanRepayment => $"Loan Repayment Installment - {GetRandomMonth()}",
            TransactionType.SavingsContribution => $"Savings Contribution - {GetRandomSavingsType()}",
            TransactionType.InvestmentPurchase => $"{GetRandomInvestment()} Investment Purchase",
            TransactionType.CardIssue => $"{GetRandomCardType()} Card Issuance Fee",
            TransactionType.InsurancePayment => $"{GetRandomInsuranceType()} Insurance Premium",
            TransactionType.TravelBooking => $"Flight/Bus Ticket to {GetRandomCity()}",
            TransactionType.VoucherRedeem => $"{GetRandomVoucherType()} Voucher Redemption",
            TransactionType.DirectDebit => $"Direct Debit - {GetRandomDirectDebitType()}",

            _ => $"Transaction #{number}"
        };
    }

    // Helper methods for realistic Nigerian transaction descriptions
    private static string GetRandomMonth() => new[] { "January", "February", "March", "April", "May", "June" }[new Random(Guid.NewGuid().GetHashCode()).Next(6)];
    private static string GetRandomTelecom() => new[] { "MTN", "Airtel", "Glo", "9mobile" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomDataSize() => new[] { "2GB", "5GB", "10GB", "20GB", "50GB" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)];
    private static string GetRandomBank() => new[] { "GTBank", "Access Bank", "First Bank", "UBA", "Zenith Bank", "Wema Bank", "Sterling Bank" }[new Random(Guid.NewGuid().GetHashCode()).Next(7)];
    private static string GetRandomInvestment() => new[] { "Mutual Fund", "Treasury Bills", "Stock Market", "Fixed Deposit", "Real Estate Fund" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)];
    private static string GetRandomRecipient() => new[] { "Family Member", "Business Partner", "Friend", "Vendor", "Supplier" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)];
    private static string GetRandomCity() => new[] { "Abuja", "Port Harcourt", "Kano", "Ibadan", "Calabar", "Enugu", "Benin City" }[new Random(Guid.NewGuid().GetHashCode()).Next(7)];
    private static string GetRandomCountry() => new[] { "USA", "UK", "Canada", "Dubai", "South Africa", "Ghana", "Kenya" }[new Random(Guid.NewGuid().GetHashCode()).Next(7)];
    private static string GetRandomBillType() => new[] { "Electricity", "Water", "Internet", "Cable TV", "Waste Management" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)];
    private static string GetRandomLoanPurpose() => new[] { "Personal Loan", "Business Loan", "Emergency Fund", "Asset Finance" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomSavingsType() => new[] { "Target Savings", "Fixed Deposit", "Regular Savings", "Emergency Fund" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomCardType() => new[] { "Debit Card", "Credit Card", "Virtual Card", "Prepaid Card" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomInsuranceType() => new[] { "Health Insurance", "Life Insurance", "Vehicle Insurance", "Property Insurance" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomVoucherType() => new[] { "Shopping Voucher", "Gift Voucher", "Discount Voucher", "Reward Points" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomDirectDebitType() => new[] { "Subscription Service", "Loan Repayment", "Insurance Premium", "Utility Bill" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];

    private static (string? ReceiverId, string? MerchantId) GetReceiverAndMerchantForType(TransactionType type)
    {
        // For transfer types: ReceiverId = receiver name, MerchantId = null
        // For other types: ReceiverId = transaction type, MerchantId = merchant name

        if (type == TransactionType.TransferLocal || type == TransactionType.TransferInternational)
        {
            // Transfer transactions: set ReceiverId with receiver name, MerchantId is null
            return (GetRandomReceiverName(), null);
        }
        else
        {
            // Non-transfer transactions: ReceiverId = transaction type, MerchantId = merchant name
            return (type.ToString(), GetRandomMerchantName(type));
        }
    }

    private static string GetRandomReceiverName() => new[]
    {
        "Adebayo Johnson", "Chioma Nwankwo", "Ibrahim Musa", "Funke Adeleke",
        "Emeka Okafor", "Zainab Abubakar", "Tunde Williams", "Amina Hassan",
        "Chinedu Eze", "Fatima Bello", "Segun Olawale", "Blessing Okoro"
    }[new Random(Guid.NewGuid().GetHashCode()).Next(12)];

    private static string GetRandomMerchantName(TransactionType type)
    {
        return type switch
        {
            TransactionType.BillPayment => new[] { "EKEDC", "IBEDC", "PHCN", "LAWMA", "Lagos Water Corporation" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)],
            TransactionType.AirtimeTopup => new[] { "MTN Nigeria", "Airtel Nigeria", "Glo Mobile", "9mobile" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.DataPurchase => new[] { "MTN Data Services", "Airtel Data", "Glo Data", "9mobile Data" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.LoanDisbursement => new[] { "Carbon Microfinance", "FairMoney", "Branch", "PalmCredit", "RenMoney" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)],
            TransactionType.LoanRepayment => new[] { "Carbon Microfinance", "FairMoney", "Branch", "PalmCredit", "RenMoney" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)],
            TransactionType.SavingsContribution => new[] { "PiggyVest", "Cowrywise", "Kuda Bank", "ALAT Savings" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.InvestmentPurchase => new[] { "ARM Investment", "Stanbic IBTC", "Meristem Securities", "CardinalStone" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.CardIssue => new[] { "GTBank Card Services", "Access Bank Cards", "First Bank Card Center", "UBA Card Division" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.InsurancePayment => new[] { "AXA Mansard", "Old Mutual", "Leadway Assurance", "AIICO Insurance" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.TravelBooking => new[] { "Wakanow", "TravelBeta", "Jumia Travel", "GIG Logistics" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.VoucherRedeem => new[] { "Jumia", "Konga", "Slot", "ShopRite" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            TransactionType.DirectDebit => new[] { "Netflix Nigeria", "DSTV", "Showmax", "Spotify Nigeria" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)],
            _ => "Unknown Merchant"
        };
    }
}
