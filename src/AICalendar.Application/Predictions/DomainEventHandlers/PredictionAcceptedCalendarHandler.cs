using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Notifications;
using AICalendar.Domain.Events;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using CalendarAggregate = AICalendar.Domain.Aggregates.CalendarAggregate.Calendar;

namespace AICalendar.Application.Predictions.DomainEventHandlers;

public class PredictionAcceptedCalendarHandler : INotificationHandler<DomainEventNotification<PredictionBatchAcceptedEvent>>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PredictionAcceptedCalendarHandler> _logger;

    public PredictionAcceptedCalendarHandler(
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<PredictionAcceptedCalendarHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PredictionBatchAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        Console.WriteLine($"Handling PredictionBatchAcceptedEvent for PredictionId: {domainEvent}");

        _logger.LogInformation(
            "CALENDAR: Handling batch acceptance for Prediction {PredictionId}. {Count} items accepted.",
            domainEvent.PredictionId.Value,
            domainEvent.Items.Count
        );

        // Get or create calendar for user
        var calendar = await _calendarRepository.GetByUserIdAsync(domainEvent.UserId, cancellationToken);

        if (calendar == null)
        {
            _logger.LogInformation("CALENDAR: Creating new calendar for user {UserId}", domainEvent.UserId.Value);
            calendar = CalendarAggregate.Create(domainEvent.UserId);
            await _calendarRepository.AddAsync(calendar, cancellationToken);
        }

        // Add each accepted item to the calendar
        foreach (var item in domainEvent.Items)
        {
            _logger.LogInformation(
                "CALENDAR: Adding item {ItemId} to Calendar: {Merchant} - ${Amount} due on {DueDate}",
                item.ItemId.Value,
                item.Merchant,
                item.Amount,
                item.DueDate.ToString("yyyy-MM-dd")
            );

            var result = calendar.AddItemFromPrediction(
                item.ItemId,
                item.Merchant,
                item.Amount,
                item.DueDate
            );

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "CALENDAR: Failed to add item {ItemId}: {Error}",
                    item.ItemId.Value,
                    result.Error
                );
            }
        }

        // Save changes (this will also trigger OutboxInterceptor to save domain events)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache for this user's calendar
        var cacheKey = $"calendar:user:{domainEvent.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogInformation(
            "CALENDAR: Successfully processed {Count} items for user {UserId}",
            domainEvent.Items.Count,
            domainEvent.UserId.Value
        );
    }
}