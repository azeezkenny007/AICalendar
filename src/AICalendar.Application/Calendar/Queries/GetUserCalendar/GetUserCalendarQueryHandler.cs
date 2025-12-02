using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public class GetUserCalendarQueryHandler : IRequestHandler<GetUserCalendarQuery, Result<CalendarDto>>
{
    private readonly ICalendarRepository _calendarRepository;

    public GetUserCalendarQueryHandler(ICalendarRepository calendarRepository)
    {
        _calendarRepository = calendarRepository;
    }

    public async Task<Result<CalendarDto>> Handle(GetUserCalendarQuery request, CancellationToken cancellationToken)
    {
        var calendar = await _calendarRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (calendar == null)
        {
            return Result<CalendarDto>.Failure($"Calendar for user {request.UserId.Value} not found.");
        }

        var dto = new CalendarDto(
            calendar.Id.Value,
            calendar.UserId.Value,
            calendar.Items.Select(i => new CalendarItemDto(
                i.Id.Value,
                i.PredictionItemId.Value,
                i.Merchant,
                i.Amount,
                i.DueDate,
                i.Account,
                i.AccountName,
                i.Description,
                i.IsPaid,
                i.PaidDate,
                i.CreatedAt
            )).ToList(),
            calendar.CreatedAt
        );

        return Result<CalendarDto>.Success(dto);
    }
}
