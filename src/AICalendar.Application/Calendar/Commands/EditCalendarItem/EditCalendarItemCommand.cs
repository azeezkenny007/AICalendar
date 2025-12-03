using AICalendar.Application.Common.Models;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.EditCalendarItem;

public record EditCalendarItemCommand(
    CalendarItemId ItemId,
    string? Merchant = null,
    decimal? Amount = null,
    DateTime? DueDate = null,
    string? Account = null,
    string? AccountName = null,
    string? Description = null
) : IRequest<OperationResult>;
