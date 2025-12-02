using System.Text.Json;
using AICalendar.Domain.Common;
using AICalendar.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hangfire;

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

    [JobDisplayName("Process Outbox Messages ({0})")]
    public async Task ProcessOutboxMessages(string jobType)
    {
        try
        {
            // 1. Get unprocessed messages (batch of 20)
            // Filter out already dispatched messages to avoid double-queueing
            var messages = await _context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null && m.Error != "Dispatched")
                .OrderBy(m => m.OccurredOnUtc)
                .Take(20)
                .ToListAsync();

            if (!messages.Any())
            {
                return;
            }

            _logger.LogInformation("Dispatching {Count} outbox messages", messages.Count);

            foreach (var message in messages)
            {
                // Enqueue the individual job
                // Extract just the event class name for cleaner dashboard display
                var eventTypeName = ExtractEventTypeName(message.Type);
                BackgroundJob.Enqueue<OutboxProcessorJob>(job => job.ProcessOutboxEvent(message.Id, eventTypeName));

                // Mark as dispatched to prevent re-queueing in the next tick
                message.Error = "Dispatched";
            }

            // Save changes to mark them as dispatched
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in outbox dispatcher");
            throw;
        }
    }

    [JobDisplayName("Process Event: {1} | ID: {0}")]
    public async Task ProcessOutboxEvent(Guid messageId, string eventTypeDisplayName)
    {
        var message = await _context.Set<OutboxMessage>().FindAsync(messageId);

        if (message == null)
        {
            _logger.LogWarning("Outbox message {MessageId} not found", messageId);
            return;
        }

        if (message.ProcessedOnUtc != null)
        {
            _logger.LogDebug("Message {MessageId} already processed", messageId);
            return;
        }

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
                await _context.SaveChangesAsync();
                return;
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
                await _context.SaveChangesAsync();
                return;
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

            _logger.LogDebug(
                "Successfully processed message {MessageId} of type {EventType}",
                message.Id,
                eventType.Name
            );
        }
        catch (Exception ex)
        {
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

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Extracts the simple class name from a full assembly-qualified type name.
    /// Example: "AICalendar.Domain.Events.PredictionBatchRejectedEvent, AICalendar.Domain, Version=..."
    /// becomes "PredictionBatchRejectedEvent"
    /// </summary>
    private static string ExtractEventTypeName(string fullTypeName)
    {
        // Split by comma to remove assembly info
        var typePart = fullTypeName.Split(',')[0].Trim();

        // Get the last part after the final dot (the class name)
        var lastDotIndex = typePart.LastIndexOf('.');
        return lastDotIndex >= 0 ? typePart.Substring(lastDotIndex + 1) : typePart;
    }
}
