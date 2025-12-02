using AICalendar.Application.Common.Interfaces;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Calendar.Commands.EditCalendarItem;

public class EditCalendarItemCommandHandler : IRequestHandler<EditCalendarItemCommand, OperationResult>
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

    public async Task<OperationResult> Handle(EditCalendarItemCommand request, CancellationToken cancellationToken)
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

        // Validate if the item can be updated using domain service
        var canUpdateResult = _calendarDomainService.CanUpdateItem(calendar, request.ItemId);
        if (!canUpdateResult.IsSuccess)
        {
            return OperationResult.BadRequest(
                "Cannot update paid item",
                canUpdateResult.Error!,
                "Paid calendar items cannot be modified. Please create a new calendar item if needed."
            );
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
            return OperationResult.Conflict(
                "Duplicate calendar item",
                duplicateResult.Error!,
                "A calendar item with the same merchant and due date already exists in your calendar."
            );
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
            // Handle validation errors from domain
            if (updateResult.Error!.Contains("Merchant cannot be empty"))
            {
                return OperationResult.BadRequest(
                    "Invalid merchant name",
                    updateResult.Error,
                    "Merchant name is required and cannot be empty."
                );
            }

            if (updateResult.Error.Contains("Amount must be greater than zero"))
            {
                return OperationResult.BadRequest(
                    "Invalid amount",
                    updateResult.Error,
                    "The payment amount must be greater than zero."
                );
            }

            // Generic validation error
            return OperationResult.BadRequest(
                "Validation failed",
                updateResult.Error,
                "The provided data is invalid. Please check your input and try again."
            );
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

        return OperationResult.Success(
            "Calendar item updated successfully",
            $"{request.Merchant} updated - ${request.Amount:F2} due on {request.DueDate:MMM dd, yyyy}",
            new
            {
                itemId = request.ItemId.Value,
                merchant = request.Merchant,
                amount = request.Amount,
                dueDate = request.DueDate
            }
        );
    }
}
