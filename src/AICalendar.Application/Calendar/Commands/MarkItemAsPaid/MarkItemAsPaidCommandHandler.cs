using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.Commands.MarkItemAsPaid;

public class MarkItemAsPaidCommandHandler : IRequestHandler<MarkItemAsPaidCommand, Result>
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

    public async Task<Result> Handle(MarkItemAsPaidCommand request, CancellationToken cancellationToken)
    {
        // Find the calendar containing this item
        var calendars = await _calendarRepository.GetAllAsync(cancellationToken);
        var calendar = calendars.FirstOrDefault(c => c.Items.Any(i => i.Id == request.ItemId));

        if (calendar == null)
        {
            return Result.Failure($"Calendar item {request.ItemId.Value} not found.");
        }

        var result = calendar.MarkItemAsPaid(request.ItemId, request.PaidDate);
        if (!result.IsSuccess)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache for this user's calendar
        var cacheKey = $"calendar:user:{calendar.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogInformation(
            "Calendar item {ItemId} marked as paid on {PaidDate}",
            request.ItemId.Value,
            request.PaidDate
        );

        return Result.Success();
    }
}
