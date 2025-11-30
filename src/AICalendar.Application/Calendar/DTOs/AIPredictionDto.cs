namespace AICalendar.Application.Calendar.DTOs;

public class AIPredictionDto
{
    public Guid TransactionId { get; set; }
    public double FinalConfidence { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime PredictedNextDate { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public double RuleConfidence { get; set; }
    public double MLConfidence { get; set; }
}
