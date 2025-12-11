using AICalendar.Application.Common.Models;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.MarkItemAsPaid;

public record MarkItemAsPaidCommand(
    CalendarItemId ItemId,
    DateTime PaidDate
) : IRequest<OperationResult>;
