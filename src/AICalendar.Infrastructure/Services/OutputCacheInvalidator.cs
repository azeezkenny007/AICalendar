using AICalendar.Application.Common.Interfaces;
using Microsoft.AspNetCore.OutputCaching;

namespace AICalendar.Infrastructure.Services;

public class OutputCacheInvalidator : ICacheInvalidator
{
    private readonly IOutputCacheStore _cacheStore;

    public OutputCacheInvalidator(IOutputCacheStore cacheStore)
    {
        _cacheStore = cacheStore;
    }

    public async Task InvalidatePredictionsCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cacheStore.EvictByTagAsync("predictions", cancellationToken);
    }

    public async Task InvalidateCalendarCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cacheStore.EvictByTagAsync("calendar", cancellationToken);
    }
}
