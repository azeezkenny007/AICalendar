using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.MarkItemAsPaid;

public record MarkItemAsPaidCommand(
    CalendarItemId ItemId,
    DateTime PaidDate
) : IRequest<Result>;
