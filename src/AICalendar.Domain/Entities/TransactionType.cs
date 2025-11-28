namespace AICalendar.Domain.Entities;

public enum TransactionType
{
    // Utilities & Bills
    ElectricityBill = 1,
    WaterBill = 2,
    InternetBill = 3,
    CableTVSubscription = 4,
    PhoneAirtime = 5,
    PhoneData = 6,

    // Food & Groceries
    Groceries = 7,
    RestaurantDining = 8,
    FastFood = 9,

    // Transportation
    FuelPurchase = 10,
    TransportFare = 11,
    RideHailing = 12,
    VehicleMaintenance = 13,

    // Entertainment & Leisure
    MovieTickets = 14,
    Concerts = 15,
    GymMembership = 16,
    SportsActivities = 17,

    // Shopping
    ClothingPurchase = 18,
    ElectronicsGadgets = 19,
    HomeAppliances = 20,
    PersonalCare = 21,

    // Health & Wellness
    MedicalExpense = 22,
    PharmacyPurchase = 23,
    HealthInsurance = 24,

    // Education
    SchoolFees = 25,
    OnlineCourses = 26,
    BooksAndMaterials = 27,

    // Financial
    Savings = 28,
    Investment = 29,
    LoanRepayment = 30,
    BankTransfer = 31,

    // Housing
    Rent = 32,
    PropertyMaintenance = 33,

    // Services
    HairSalon = 34,
    Laundry = 35,
    ProfessionalServices = 36,

    // Travel
    TravelBooking = 37,
    HotelAccommodation = 38,

    // Other
    Donation = 39,
    GiftPurchase = 40,
    Miscellaneous = 41
}
