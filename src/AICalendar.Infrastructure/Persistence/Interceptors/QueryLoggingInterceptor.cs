using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that logs SQL queries and their execution time.
/// Useful for debugging and performance monitoring.
/// </summary>
public class QueryLoggingInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryLoggingInterceptor> _logger;

    public QueryLoggingInterceptor(ILogger<QueryLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Duration.TotalMilliseconds > 1000) // Log slow queries (> 1 second)
        {
            _logger.LogWarning(
                "Slow query detected ({Duration}ms): {CommandText}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText
            );
        }
        else
        {
            _logger.LogDebug(
                "Query executed ({Duration}ms): {CommandText}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText
            );
        }

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
