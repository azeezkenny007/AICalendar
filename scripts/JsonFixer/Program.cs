using System.Text.Json;

Console.WriteLine("==============================================");
Console.WriteLine("Transaction JSON Fixer");
Console.WriteLine("==============================================\n");

var inputFile = @"c:\Users\Purple_Serve\Downloads\Transactions_Table_Updated_v2_fixed.json";
var outputFile = @"c:\Users\Purple_Serve\Desktop\AICalendar\src\AICalendar.Infrastructure\Data\SeedData\transactions.json";

if (!File.Exists(inputFile))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: Input file not found at {inputFile}");
    Console.ResetColor();
    return;
}

Console.WriteLine($"Reading from: {inputFile}");
var jsonContent = File.ReadAllText(inputFile);
var transactions = JsonSerializer.Deserialize<List<TransactionDto>>(jsonContent);

if (transactions == null || transactions.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: No transactions found in file!");
    Console.ResetColor();
    return;
}

Console.WriteLine($"Found {transactions.Count} transactions");

// Track seen IDs and fix issues
var seenIds = new HashSet<string>();
var fixedTransactions = new List<TransactionDto>();
var duplicateCount = 0;
var typeFixCount = 0;

var nullFixCount = 0;

foreach (var trans in transactions)
{
    // Fix transaction type
    if (trans.Type == "OTHER")
    {
        trans.Type = "VoucherRedeem";
        typeFixCount++;
    }
    else if (trans.Type == "LocalTransfer")
    {
        trans.Type = "TransferLocal";
        typeFixCount++;
    }

    // Check for duplicate ID
    if (seenIds.Contains(trans.Id))
    {
        var oldId = trans.Id;
        trans.Id = Guid.NewGuid().ToString();
        Console.WriteLine($"Fixed duplicate ID: {oldId} -> {trans.Id}");
        duplicateCount++;
    }

    // Fix NULL ReceiverId and MerchantId
    if (string.IsNullOrWhiteSpace(trans.ReceiverId) || trans.ReceiverId == "NULL")
    {
        trans.ReceiverId = GetReceiverIdForType(trans.Type, trans.Description);
        nullFixCount++;
    }

    if (string.IsNullOrWhiteSpace(trans.MerchantId) || trans.MerchantId == "NULL")
    {
        trans.MerchantId = GetMerchantIdForType(trans.Type, trans.Description);
        nullFixCount++;
    }

    seenIds.Add(trans.Id);
    fixedTransactions.Add(trans);
}

// Write the fixed JSON
var options = new JsonSerializerOptions
{
    WriteIndented = true
};
var fixedJson = JsonSerializer.Serialize(fixedTransactions, options);
File.WriteAllText(outputFile, fixedJson);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n✓ Fixed {duplicateCount} duplicate IDs");
Console.WriteLine($"✓ Fixed {typeFixCount} transaction types");
Console.WriteLine($"✓ Fixed {nullFixCount} NULL ReceiverId/MerchantId values");
Console.WriteLine($"✓ Total transactions: {fixedTransactions.Count}");
Console.WriteLine($"✓ Saved to: {outputFile}");
Console.ResetColor();

static string GetReceiverIdForType(string type, string description)
{
    // For Transfer types, extract receiver from description or use generic
    if (type == "TransferLocal" || type == "TransferInternational")
    {
        return "Recipient Account";
    }

    // For other types, use the transaction type or merchant from description
    return type;
}

static string GetMerchantIdForType(string type, string description)
{
    // Try to extract merchant from description
    if (description.Contains("MTN"))
        return "MTN Nigeria";
    if (description.Contains("Glo"))
        return "Glo Mobile";
    if (description.Contains("Airtel"))
        return "Airtel Nigeria";
    if (description.Contains("9mobile"))
        return "9mobile";
    if (description.Contains("DSTV"))
        return "DSTV";
    if (description.Contains("Netflix"))
        return "Netflix Nigeria";
    if (description.Contains("Cowrywise"))
        return "Cowrywise";
    if (description.Contains("FairMoney"))
        return "FairMoney";
    if (description.Contains("PHCN"))
        return "PHCN";
    if (description.Contains("AXA"))
        return "AXA Mansard";
    if (description.Contains("Uber"))
        return "Uber";
    if (description.Contains("Jumia"))
        return "Jumia";

    // Default based on type
    return type switch
    {
        "BillPayment" => "Utility Company",
        "AirtimeTopup" => "Mobile Network",
        "DataPurchase" => "Mobile Network",
        "LoanDisbursement" => "Lending Platform",
        "LoanRepayment" => "Lending Platform",
        "SavingsContribution" => "Savings Platform",
        "InvestmentPurchase" => "Investment Platform",
        "CardIssue" => "Bank Card Services",
        "InsurancePayment" => "Insurance Company",
        "TravelBooking" => "Travel Agency",
        "VoucherRedeem" => "Merchant",
        "DirectDebit" => "Service Provider",
        "TransferLocal" => "Bank",
        "TransferInternational" => "Bank",
        _ => "Unknown Merchant"
    };
}

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
