using AICalendar.Infrastructure.Data;
using AICalendar.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire job that cleans up old processed outbox messages
/// Runs weekly to keep the database clean
/// </summary>
public class CleanupOutboxJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CleanupOutboxJob> _logger;

    public CleanupOutboxJob(
        ApplicationDbContext context,
        ILogger<CleanupOutboxJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanupProcessedMessages()
    {
        try
        {
            _logger.LogInformation("Starting outbox cleanup");

            // Delete messages older than 7 days that have been processed
            var cutoffDate = DateTime.UtcNow.AddDays(-7);

            var messagesToDelete = await _context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < cutoffDate)
                .ToListAsync();

            if (!messagesToDelete.Any())
            {
                _logger.LogInformation("No old outbox messages to clean up");
                return;
            }

            _context.Set<OutboxMessage>().RemoveRange(messagesToDelete);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cleaned up {Count} old outbox messages (older than {CutoffDate})",
                messagesToDelete.Count,
                cutoffDate
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up outbox messages");
            throw;
        }
    }
}
