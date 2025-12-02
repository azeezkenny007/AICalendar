using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.Commands.EditCalendarItem;

public class EditCalendarItemCommandHandler : IRequestHandler<EditCalendarItemCommand, Result>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly ICalendarDomainService _calendarDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<EditCalendarItemCommandHandler> _logger;

    public EditCalendarItemCommandHandler(
        ICalendarRepository calendarRepository,
        ICalendarDomainService calendarDomainService,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<EditCalendarItemCommandHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _calendarDomainService = calendarDomainService;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(EditCalendarItemCommand request, CancellationToken cancellationToken)
    {
        // Find the calendar containing this item
        var calendars = await _calendarRepository.GetAllAsync(cancellationToken);
        var calendar = calendars.FirstOrDefault(c => c.Items.Any(i => i.Id == request.ItemId));

        if (calendar == null)
        {
            return Result.Failure($"Calendar item {request.ItemId.Value} not found.");
        }

        // Validate if the item can be updated using domain service
        var canUpdateResult = _calendarDomainService.CanUpdateItem(calendar, request.ItemId);
        if (!canUpdateResult.IsSuccess)
        {
            return Result.Failure(canUpdateResult.Error!);
        }

        // Check for duplicates (excluding the current item)
        var duplicateResult = _calendarDomainService.IsDuplicateItem(
            calendar,
            request.Merchant,
            request.DueDate,
            request.ItemId
        );

        if (!duplicateResult.IsSuccess)
        {
            return Result.Failure(duplicateResult.Error!);
        }

        // Update the calendar item
        var updateResult = calendar.UpdateItem(
            request.ItemId,
            request.Merchant,
            request.Amount,
            request.DueDate,
            request.Account,
            request.AccountName,
            request.Description
        );

        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache for this user's calendar
        var cacheKey = $"calendar:user:{calendar.UserId.Value}";
        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogInformation(
            "Calendar item {ItemId} updated: {Merchant} - ${Amount} due on {DueDate}",
            request.ItemId.Value,
            request.Merchant,
            request.Amount,
            request.DueDate
        );

        return Result.Success();
    }
}
