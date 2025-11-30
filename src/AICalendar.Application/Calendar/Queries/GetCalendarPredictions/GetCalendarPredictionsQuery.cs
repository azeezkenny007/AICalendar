using AICalendar.Application.Calendar.DTOs;
using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetCalendarPredictions;

public record GetCalendarPredictionsQuery(Guid UserId) : IRequest<Result<CalendarPredictionResponseDto>>;
