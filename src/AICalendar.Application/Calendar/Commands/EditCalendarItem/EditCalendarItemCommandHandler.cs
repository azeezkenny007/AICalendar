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
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ILogger<EditCalendarItemCommandHandler> _logger;

    public EditCalendarItemCommandHandler(
        ICalendarRepository calendarRepository,
        ICalendarDomainService calendarDomainService,
        IUnitOfWork unitOfWork,
        ICacheInvalidator cacheInvalidator,
        ILogger<EditCalendarItemCommandHandler> logger)
    {
        _calendarRepository = calendarRepository;
        _calendarDomainService = calendarDomainService;
        _unitOfWork = unitOfWork;
        _cacheInvalidator = cacheInvalidator;
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

        // Get the existing item to use current values as fallbacks
        var existingItem = calendar.Items.First(i => i.Id == request.ItemId);

        // Use provided values or fall back to existing values
        var merchant = request.Merchant ?? existingItem.Merchant;
        var amount = request.Amount ?? existingItem.Amount;
        var dueDate = request.DueDate ?? existingItem.DueDate;
        var account = request.Account ?? existingItem.Account;
        var accountName = request.AccountName ?? existingItem.AccountName;
        var description = request.Description ?? existingItem.Description;

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

        // Check for duplicates (excluding the current item) - only if merchant or dueDate changed
        if (request.Merchant != null || request.DueDate.HasValue)
        {
            var duplicateResult = _calendarDomainService.IsDuplicateItem(
                calendar,
                merchant,
                dueDate,
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
        }

        // Update the calendar item
        var updateResult = calendar.UpdateItem(
            request.ItemId,
            merchant,
            amount,
            dueDate,
            account,
            accountName,
            description
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

        // Invalidate cache for calendar
        await _cacheInvalidator.InvalidateCalendarCacheAsync(cancellationToken);

        _logger.LogInformation(
            "Calendar item {ItemId} updated: {Merchant} - ${Amount} due on {DueDate}",
            request.ItemId.Value,
            merchant,
            amount,
            dueDate
        );

        return OperationResult.Success(
            "Calendar item updated successfully",
            $"{merchant} updated - ${amount:F2} due on {dueDate:MMM dd, yyyy}",
            new
            {
                itemId = request.ItemId.Value,
                merchant = merchant,
                amount = amount,
                dueDate = dueDate,
                account = account,
                accountName = accountName,
                description = description
            }
        );
    }
}
