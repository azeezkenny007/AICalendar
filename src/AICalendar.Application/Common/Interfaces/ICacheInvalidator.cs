namespace AICalendar.Application.Common.Interfaces;

public interface ICacheInvalidator
{
    Task InvalidatePredictionsCacheAsync(CancellationToken cancellationToken = default);
    Task InvalidateCalendarCacheAsync(CancellationToken cancellationToken = default);
}
