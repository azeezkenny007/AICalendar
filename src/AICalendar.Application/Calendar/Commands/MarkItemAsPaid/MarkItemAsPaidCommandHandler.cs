using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.Commands.MarkItemAsPaid;

public class MarkItemAsPaidCommandHandler : IRequestHandler<MarkItemAsPaidCommand, OperationResult>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MarkItemAsPaidCommandHandler> _logger;

    public MarkItemAsPaidCommandHandler(
        ICalendarRepository calendarRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<MarkItemAsPaidCommandHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<OperationResult> Handle(MarkItemAsPaidCommand request, CancellationToken cancellationToken)
    {
        // Find the calendar containing this item
        var calendars = await _calendarRepository.GetAllAsync(cancellationToken);
        var calendar = calendars.FirstOrDefault(c => c.Items.Any(i => i.Id == request.ItemId));

        if (calendar == null)
        {
            return OperationResult.NotFound(
                "Calendar item not found",
                $"Calendar item {request.ItemId.Value} not found.",
                $"The calendar item with ID '{request.ItemId.Value}' does not exist or may have been deleted."
            );
        }

        // Reload the calendar to ensure proper EF Core tracking
        var trackedCalendar = await _calendarRepository.GetByIdAsync(calendar.Id, cancellationToken);
        if (trackedCalendar == null)
        {
            return OperationResult.NotFound(
                "Calendar not found",
                "Failed to reload calendar for update.",
                "The calendar could not be reloaded from the database."
            );
        }

        calendar = trackedCalendar;

        var result = calendar.MarkItemAsPaid(request.ItemId, request.PaidDate);
        if (!result.IsSuccess)
        {
            // Handle "already paid" error
            if (result.Error!.Contains("already marked as paid"))
            {
                return OperationResult.BadRequest(
                    "Item already paid",
                    result.Error,
                    "This calendar item has already been marked as paid."
                );
            }

            // Generic error
            return OperationResult.BadRequest(
                "Operation failed",
                result.Error,
                "Unable to mark the calendar item as paid. Please try again."
            );
        }

        // Explicitly update the calendar in the repository to ensure EF Core tracks changes
        await _calendarRepository.UpdateAsync(calendar, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate distributed cache for this user's calendar
        var cacheKey = $"calendar:user:{calendar.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogInformation(
            "Calendar item {ItemId} marked as paid on {PaidDate}",
            request.ItemId.Value,
            request.PaidDate
        );

        return OperationResult.Success(
            "Payment recorded successfully",
            $"Calendar item marked as paid on {request.PaidDate:MMM dd, yyyy}",
            new
            {
                itemId = request.ItemId.Value,
                paidDate = request.PaidDate
            }
        );
    }
}
