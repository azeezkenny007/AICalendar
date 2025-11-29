using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace AICalendar.Application.Calendar.DTOs;

public class EditTransactionDto
{
    /// <summary>
    /// Transaction amount (must be greater than 0 if provided)
    /// </summary>
    [DefaultValue(5000)]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Transaction description
    /// </summary>
    [DefaultValue("Updated transaction description")]
    public string? Description { get; set; }

    /// <summary>
    /// Transaction type - Valid values: TransferLocal, TransferInternational, BillPayment, AirtimeTopup, DataPurchase, LoanDisbursement, LoanRepayment, SavingsContribution, InvestmentPurchase, CardIssue, InsurancePayment, TravelBooking, VoucherRedeem, DirectDebit
    /// </summary>
    [DefaultValue("TransferLocal")]
    public string? TransactionType { get; set; }

    /// <summary>
    /// Transaction date
    /// </summary>
    public DateTime? TransactionDate { get; set; }
}
