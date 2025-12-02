using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public record GetUserCalendarQuery(UserId UserId) : IRequest<Result<CalendarDto>>;
