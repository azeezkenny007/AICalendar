using AICalendar.Domain.Common;

namespace AICalendar.Domain.ValueObjects;

public class PredictionCycle : ValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private PredictionCycle(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
        {
            throw new ArgumentException("StartDate must be before EndDate", nameof(startDate));
        }

        StartDate = startDate;
        EndDate = endDate;
    }

    public static PredictionCycle Create(DateTime startDate, DateTime endDate)
    {
        return new PredictionCycle(startDate, endDate);
    }

    public static PredictionCycle Monthly(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return new PredictionCycle(start, end);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}
