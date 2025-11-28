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
            // Utilities & Bills (₦5,000 - ₦50,000)
            TransactionType.ElectricityBill => Math.Round((decimal)(random.NextDouble() * 35000 + 8000), 2),
            TransactionType.WaterBill => Math.Round((decimal)(random.NextDouble() * 8000 + 3000), 2),
            TransactionType.InternetBill => Math.Round((decimal)(random.NextDouble() * 15000 + 5000), 2),
            TransactionType.CableTVSubscription => Math.Round((decimal)(random.NextDouble() * 12000 + 3000), 2),
            TransactionType.PhoneAirtime => Math.Round((decimal)(random.NextDouble() * 4000 + 500), 2),
            TransactionType.PhoneData => Math.Round((decimal)(random.NextDouble() * 8000 + 2000), 2),

            // Food & Groceries (₦2,000 - ₦50,000)
            TransactionType.Groceries => Math.Round((decimal)(random.NextDouble() * 35000 + 15000), 2),
            TransactionType.RestaurantDining => Math.Round((decimal)(random.NextDouble() * 18000 + 5000), 2),
            TransactionType.FastFood => Math.Round((decimal)(random.NextDouble() * 4000 + 1500), 2),

            // Transportation (₦500 - ₦30,000)
            TransactionType.FuelPurchase => Math.Round((decimal)(random.NextDouble() * 25000 + 5000), 2),
            TransactionType.TransportFare => Math.Round((decimal)(random.NextDouble() * 2500 + 500), 2),
            TransactionType.RideHailing => Math.Round((decimal)(random.NextDouble() * 7000 + 1500), 2),
            TransactionType.VehicleMaintenance => Math.Round((decimal)(random.NextDouble() * 45000 + 15000), 2),

            // Entertainment & Leisure (₦3,000 - ₦40,000)
            TransactionType.MovieTickets => Math.Round((decimal)(random.NextDouble() * 7000 + 3000), 2),
            TransactionType.Concerts => Math.Round((decimal)(random.NextDouble() * 30000 + 10000), 2),
            TransactionType.GymMembership => Math.Round((decimal)(random.NextDouble() * 18000 + 8000), 2),
            TransactionType.SportsActivities => Math.Round((decimal)(random.NextDouble() * 15000 + 5000), 2),

            // Shopping (₦5,000 - ₦200,000)
            TransactionType.ClothingPurchase => Math.Round((decimal)(random.NextDouble() * 45000 + 10000), 2),
            TransactionType.ElectronicsGadgets => Math.Round((decimal)(random.NextDouble() * 180000 + 50000), 2),
            TransactionType.HomeAppliances => Math.Round((decimal)(random.NextDouble() * 150000 + 30000), 2),
            TransactionType.PersonalCare => Math.Round((decimal)(random.NextDouble() * 12000 + 3000), 2),

            // Health & Wellness (₦3,000 - ₦100,000)
            TransactionType.MedicalExpense => Math.Round((decimal)(random.NextDouble() * 70000 + 10000), 2),
            TransactionType.PharmacyPurchase => Math.Round((decimal)(random.NextDouble() * 8000 + 2000), 2),
            TransactionType.HealthInsurance => Math.Round((decimal)(random.NextDouble() * 40000 + 20000), 2),

            // Education (₦5,000 - ₦300,000)
            TransactionType.SchoolFees => Math.Round((decimal)(random.NextDouble() * 250000 + 50000), 2),
            TransactionType.OnlineCourses => Math.Round((decimal)(random.NextDouble() * 35000 + 5000), 2),
            TransactionType.BooksAndMaterials => Math.Round((decimal)(random.NextDouble() * 20000 + 5000), 2),

            // Financial (₦10,000 - ₦500,000)
            TransactionType.Savings => Math.Round((decimal)(random.NextDouble() * 200000 + 50000), 2),
            TransactionType.Investment => Math.Round((decimal)(random.NextDouble() * 450000 + 100000), 2),
            TransactionType.LoanRepayment => Math.Round((decimal)(random.NextDouble() * 150000 + 30000), 2),
            TransactionType.BankTransfer => Math.Round((decimal)(random.NextDouble() * 180000 + 20000), 2),

            // Housing (₦20,000 - ₦500,000)
            TransactionType.Rent => Math.Round((decimal)(random.NextDouble() * 400000 + 100000), 2),
            TransactionType.PropertyMaintenance => Math.Round((decimal)(random.NextDouble() * 80000 + 20000), 2),

            // Services (₦2,000 - ₦30,000)
            TransactionType.HairSalon => Math.Round((decimal)(random.NextDouble() * 18000 + 5000), 2),
            TransactionType.Laundry => Math.Round((decimal)(random.NextDouble() * 4000 + 2000), 2),
            TransactionType.ProfessionalServices => Math.Round((decimal)(random.NextDouble() * 70000 + 30000), 2),

            // Travel (₦10,000 - ₦300,000)
            TransactionType.TravelBooking => Math.Round((decimal)(random.NextDouble() * 250000 + 50000), 2),
            TransactionType.HotelAccommodation => Math.Round((decimal)(random.NextDouble() * 80000 + 20000), 2),

            // Other (₦1,000 - ₦50,000)
            TransactionType.Donation => Math.Round((decimal)(random.NextDouble() * 30000 + 5000), 2),
            TransactionType.GiftPurchase => Math.Round((decimal)(random.NextDouble() * 35000 + 5000), 2),
            TransactionType.Miscellaneous => Math.Round((decimal)(random.NextDouble() * 15000 + 2000), 2),

            _ => 0m
        };
    }

    private static string GetDescriptionForTransactionType(TransactionType type, int number)
    {
        return type switch
        {
            // Utilities & Bills
            TransactionType.ElectricityBill => $"PHCN/EKEDC Electricity Bill - {GetRandomMonth()}",
            TransactionType.WaterBill => $"Water Board Payment - {GetRandomMonth()}",
            TransactionType.InternetBill => $"Spectranet/Airtel Fiber - {GetRandomMonth()}",
            TransactionType.CableTVSubscription => $"DSTV/GOtv Subscription - {GetRandomPackage()}",
            TransactionType.PhoneAirtime => $"{GetRandomTelecom()} Airtime Recharge",
            TransactionType.PhoneData => $"{GetRandomTelecom()} Data Bundle - {GetRandomDataSize()}",

            // Food & Groceries
            TransactionType.Groceries => $"Shoprite/Spar Shopping - {GetRandomFoodItem()}",
            TransactionType.RestaurantDining => $"{GetRandomRestaurant()} - Family Dinner",
            TransactionType.FastFood => $"{GetRandomFastFood()} - Quick Meal",

            // Transportation
            TransactionType.FuelPurchase => $"Petrol Station - {GetRandomLiters()}L Fuel",
            TransactionType.TransportFare => $"Danfo/Keke Transport - {GetRandomRoute()}",
            TransactionType.RideHailing => $"{GetRandomRideService()} Trip - {GetRandomLocation()}",
            TransactionType.VehicleMaintenance => $"Car Service - {GetRandomMaintenance()}",

            // Entertainment & Leisure
            TransactionType.MovieTickets => $"Filmhouse/Silverbird Cinema - {GetRandomMovie()}",
            TransactionType.Concerts => $"{GetRandomEvent()} Concert/Show Tickets",
            TransactionType.GymMembership => $"{GetRandomGym()} Monthly Membership",
            TransactionType.SportsActivities => $"{GetRandomSport()} - Weekend Activity",

            // Shopping
            TransactionType.ClothingPurchase => $"{GetRandomClothingStore()} - {GetRandomClothing()}",
            TransactionType.ElectronicsGadgets => $"{GetRandomElectronicsStore()} - {GetRandomGadget()}",
            TransactionType.HomeAppliances => $"{GetRandomAppliance()} Purchase",
            TransactionType.PersonalCare => $"{GetRandomPersonalCare()} Products",

            // Health & Wellness
            TransactionType.MedicalExpense => $"{GetRandomHospital()} - Medical Consultation",
            TransactionType.PharmacyPurchase => $"{GetRandomPharmacy()} - Medications",
            TransactionType.HealthInsurance => $"HMO Health Insurance - {GetRandomMonth()}",

            // Education
            TransactionType.SchoolFees => $"School Fees Payment - {GetRandomTerm()}",
            TransactionType.OnlineCourses => $"{GetRandomOnlinePlatform()} Course Enrollment",
            TransactionType.BooksAndMaterials => $"Academic Books & Supplies",

            // Financial
            TransactionType.Savings => $"Monthly Savings - {GetRandomBank()}",
            TransactionType.Investment => $"{GetRandomInvestment()} Investment",
            TransactionType.LoanRepayment => $"Loan Installment - {GetRandomMonth()}",
            TransactionType.BankTransfer => $"Transfer to {GetRandomRecipient()}",

            // Housing
            TransactionType.Rent => $"House Rent Payment - {GetRandomLocation()}",
            TransactionType.PropertyMaintenance => $"{GetRandomMaintenance()} - House Repairs",

            // Services
            TransactionType.HairSalon => $"{GetRandomSalon()} - Hair Styling",
            TransactionType.Laundry => $"Laundry & Dry Cleaning Service",
            TransactionType.ProfessionalServices => $"{GetRandomProfessionalService()}",

            // Travel
            TransactionType.TravelBooking => $"Flight/Bus Ticket to {GetRandomCity()}",
            TransactionType.HotelAccommodation => $"{GetRandomHotel()} Accommodation",

            // Other
            TransactionType.Donation => $"Donation - {GetRandomCharity()}",
            TransactionType.GiftPurchase => $"Gift Purchase - {GetRandomOccasion()}",
            TransactionType.Miscellaneous => $"Miscellaneous Expense #{number}",

            _ => $"Transaction #{number}"
        };
    }

    // Helper methods for realistic Nigerian transaction descriptions
    private static string GetRandomMonth() => new[] { "January", "February", "March", "April", "May", "June" }[new Random(Guid.NewGuid().GetHashCode()).Next(6)];
    private static string GetRandomPackage() => new[] { "Compact Plus", "Premium", "Confam" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomTelecom() => new[] { "MTN", "Airtel", "Glo", "9mobile" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomDataSize() => new[] { "2GB", "5GB", "10GB", "20GB" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomFoodItem() => new[] { "Groceries", "Rice & Provisions", "Fresh Vegetables" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomRestaurant() => new[] { "Mama Cass", "Tantalizers", "Sweet Sensation", "Yellow Chilli" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomFastFood() => new[] { "KFC", "Chicken Republic", "Domino's Pizza", "Mr Biggs" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomLiters() => new[] { "10", "15", "20", "25", "30" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)];
    private static string GetRandomRoute() => new[] { "Ikeja to VI", "Lekki to CMS", "Surulere to Yaba", "Ajah to Ikoyi" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomRideService() => new[] { "Uber", "Bolt", "inDrive" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomLocation() => new[] { "Lekki", "Victoria Island", "Ikeja", "Yaba", "Ikoyi", "Surulere" }[new Random(Guid.NewGuid().GetHashCode()).Next(6)];
    private static string GetRandomMaintenance() => new[] { "Oil Change", "Tire Replacement", "Brake Service", "AC Repair" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomMovie() => new[] { "Latest Blockbuster", "Nollywood Premier", "Action Movie" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomEvent() => new[] { "Burna Boy", "Wizkid", "Davido", "Tiwa Savage" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomGym() => new[] { "Bodyline Fitness", "Genesis Gym", "The Gym" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomSport() => new[] { "Football Match", "Basketball", "Tennis" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomClothingStore() => new[] { "Balogun Market", "Jumia Fashion", "Yaba Market" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomClothing() => new[] { "Native Attire", "Corporate Wear", "Casual Outfit" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomElectronicsStore() => new[] { "Slot", "Pointek", "3C Hub" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomGadget() => new[] { "Smartphone", "Laptop", "Headphones", "Smart Watch" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomAppliance() => new[] { "Washing Machine", "Refrigerator", "Microwave", "Air Conditioner" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
    private static string GetRandomPersonalCare() => new[] { "Skincare", "Hair Care", "Grooming" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomHospital() => new[] { "Lagos University Teaching Hospital", "Reddington Hospital", "Lagoon Hospital" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomPharmacy() => new[] { "HealthPlus", "MedPlus", "Pharma Express" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomTerm() => new[] { "First Term", "Second Term", "Third Term" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomOnlinePlatform() => new[] { "Udemy", "Coursera", "LinkedIn Learning" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomBank() => new[] { "GTBank", "Access Bank", "First Bank", "UBA", "Zenith Bank" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)];
    private static string GetRandomInvestment() => new[] { "Mutual Fund", "Treasury Bills", "Stock Market" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomRecipient() => new[] { "Family Member", "Business Partner", "Friend" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomSalon() => new[] { "Tresses Hair Salon", "BeautyHub", "Pearl Hair Studio" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomProfessionalService() => new[] { "Legal Consultation", "Accounting Services", "IT Support" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomCity() => new[] { "Abuja", "Port Harcourt", "Kano", "Ibadan", "Calabar" }[new Random(Guid.NewGuid().GetHashCode()).Next(5)];
    private static string GetRandomHotel() => new[] { "Eko Hotel", "Transcorp Hilton", "Federal Palace Hotel" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomCharity() => new[] { "Charity Organization", "Community Development", "Religious Institution" }[new Random(Guid.NewGuid().GetHashCode()).Next(3)];
    private static string GetRandomOccasion() => new[] { "Birthday", "Wedding", "Anniversary", "Graduation" }[new Random(Guid.NewGuid().GetHashCode()).Next(4)];
}
}
