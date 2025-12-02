using AICalendar.Application.Common.Models;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.EditCalendarItem;

public record EditCalendarItemCommand(
    CalendarItemId ItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    string? Account = null,
    string? AccountName = null,
    string? Description = null
) : IRequest<OperationResult>;
