namespace AICalendar.Application.Calendar.DTOs;

public class CalendarPredictionResponseDto
{
    public List<TransactionDetailDto> Transactions { get; set; } = new();
    public int TotalPredictions { get; set; }
    public DateTime GeneratedAt { get; set; }
}
