using System.Text.Json;
using AICalendar.Domain.Common;
using AICalendar.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Outbox;

/// <summary>
/// Hangfire background job that processes domain events from the Outbox table
/// Runs every 10 seconds to ensure reliable event processing
/// </summary>
public class OutboxProcessorJob
{
    private readonly ApplicationDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxProcessorJob> _logger;

    public OutboxProcessorJob(
        ApplicationDbContext context,
        IPublisher publisher,
        ILogger<OutboxProcessorJob> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessOutboxMessages()
    {
        try
        {
            // 1. Get unprocessed messages (batch of 20)
            var messages = await _context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(20)
                .ToListAsync();

            if (!messages.Any())
            {
                _logger.LogDebug("No outbox messages to process");
                return;
            }

            _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

            var successCount = 0;
            var failureCount = 0;

            foreach (var message in messages)
            {
                try
                {
                    // 2. Deserialize the domain event
                    var eventType = Type.GetType(message.Type);

                    if (eventType == null)
                    {
                        _logger.LogError(
                            "Could not resolve event type {Type} for message {MessageId}",
                            message.Type,
                            message.Id
                        );
                        message.Error = $"Could not resolve type: {message.Type}";
                        message.RetryCount++;
                        continue;
                    }

                    var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);

                    if (domainEvent == null)
                    {
                        _logger.LogError(
                            "Failed to deserialize event {Type} for message {MessageId}",
                            message.Type,
                            message.Id
                        );
                        message.Error = "Failed to deserialize event";
                        message.RetryCount++;
                        continue;
                    }

                    // 3. Publish to MediatR (triggers all event handlers)
                    _logger.LogDebug(
                        "Publishing event {EventType} from message {MessageId}",
                        eventType.Name,
                        message.Id
                    );

                    // Wrap domain event in DomainEventNotification<> so it can be handled by MediatR
                    var notificationType = typeof(AICalendar.Application.Common.Notifications.DomainEventNotification<>).MakeGenericType(eventType);
                    var notification = Activator.CreateInstance(notificationType, domainEvent);

                    if (notification == null)
                    {
                         throw new InvalidOperationException($"Failed to create notification wrapper for event type {eventType.Name}");
                    }

                    await _publisher.Publish(notification);

                    // 4. Mark as processed
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;
                    successCount++;

                    _logger.LogDebug(
                        "Successfully processed message {MessageId} of type {EventType}",
                        message.Id,
                        eventType.Name
                    );
                }
                catch (Exception ex)
                {
                    failureCount++;
                    message.RetryCount++;
                    message.Error = ex.Message.Length > 2000
                        ? ex.Message.Substring(0, 2000)
                        : ex.Message;

                    _logger.LogError(
                        ex,
                        "Error processing outbox message {MessageId} (Attempt {RetryCount})",
                        message.Id,
                        message.RetryCount
                    );

                    // If retry count exceeds threshold, mark as processed with error
                    if (message.RetryCount >= 5)
                    {
                        message.ProcessedOnUtc = DateTime.UtcNow;
                        _logger.LogError(
                            "Message {MessageId} exceeded max retry attempts and will be marked as failed",
                            message.Id
                        );
                    }
                }
            }

            // 5. Save all changes
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Outbox processing completed. Success: {Success}, Failed: {Failed}",
                successCount,
                failureCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in outbox processor");
            throw;
        }
    }
}
