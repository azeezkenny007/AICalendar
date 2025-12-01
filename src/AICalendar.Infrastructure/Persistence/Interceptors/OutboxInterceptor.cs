using System.Text.Json;
using AICalendar.Domain.Interfaces;
using AICalendar.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AICalendar.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that converts domain events to outbox messages
/// before saving changes to the database.
/// This ensures domain events are persisted atomically with aggregate changes.
/// </summary>
public class OutboxInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<OutboxInterceptor> _logger;

    public OutboxInterceptor(ILogger<OutboxInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        await ConvertDomainEventsToOutboxMessages(context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task ConvertDomainEventsToOutboxMessages(
        DbContext context,
        CancellationToken cancellationToken)
    {
        // 1. Get all aggregates with domain events
        var aggregatesWithEvents = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Any())
            .Select(entry => entry.Entity)
            .ToList();

        if (!aggregatesWithEvents.Any())
        {
            _logger.LogDebug("No domain events to convert to outbox messages");
            return;
        }

        // 2. Collect all domain events
        var domainEvents = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        _logger.LogInformation(
            "Converting {Count} domain events to outbox messages",
            domainEvents.Count
        );

        // 3. Convert to outbox messages
        var outboxMessages = domainEvents.Select(domainEvent =>
        {
            var eventType = domainEvent.GetType();

            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = eventType.AssemblyQualifiedName ?? eventType.FullName!,
                Content = JsonSerializer.Serialize(
                    domainEvent,
                    eventType,
                    new JsonSerializerOptions
                    {
                        WriteIndented = false
                    }
                ),
                OccurredOnUtc = DateTime.UtcNow,
                ProcessedOnUtc = null,
                RetryCount = 0
            };
        }).ToList();

        // 4. Add to context
        await context.Set<OutboxMessage>()
            .AddRangeAsync(outboxMessages, cancellationToken);

        _logger.LogDebug(
            "Added {Count} outbox messages to context",
            outboxMessages.Count
        );

        // 5. Clear domain events from aggregates
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
