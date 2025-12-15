using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Aggregates.CalendarAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.Commands.DeleteAllCalendarData;

/// <summary>
/// Handler for deleting all calendar data
/// </summary>
public class DeleteAllCalendarDataCommandHandler : IRequestHandler<DeleteAllCalendarDataCommand, Result<DeleteAllCalendarDataResponse>>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ILogger<DeleteAllCalendarDataCommandHandler> _logger;

    public DeleteAllCalendarDataCommandHandler(
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ICacheInvalidator cacheInvalidator,
        ILogger<DeleteAllCalendarDataCommandHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _cacheInvalidator = cacheInvalidator;
        _logger = logger;
    }

    public async Task<Result<DeleteAllCalendarDataResponse>> Handle(
        DeleteAllCalendarDataCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Starting deletion of all calendar data");

            // Get all calendars before deletion to clear their caches
            var allCalendars = await _calendarRepository.GetAllAsync(cancellationToken);

            var deletedCount = allCalendars.Count;

            if (deletedCount > 0)
            {
                // Delete each calendar
                foreach (var calendar in allCalendars)
                {
                    await _calendarRepository.DeleteAsync(calendar, cancellationToken);
                }
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                // Invalidate cache for all users whose calendars were deleted
                foreach (var calendar in allCalendars)
                {
                    var cacheKey = $"calendar:user:{calendar.UserId.Value}";
                    await _cacheService.RemoveAsync(cacheKey);
                }
                
                // Also invalidate the output cache for all calendar operations
                await _cacheInvalidator.InvalidateCalendarCacheAsync(cancellationToken);
                
                _logger.LogWarning(
                    "Successfully deleted all calendar data. Removed {DeletedCount} calendar record(s)",
                    deletedCount
                );
            }
            else
            {
                _logger.LogInformation("No calendar data to delete");
            }

            return Result<DeleteAllCalendarDataResponse>.Success(
                new DeleteAllCalendarDataResponse(deletedCount)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all calendar data");
            return Result<DeleteAllCalendarDataResponse>.Failure("Failed to delete calendar data: " + ex.Message);
        }
    }
}

