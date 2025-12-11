using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public class GetUserCalendarQueryHandler : IRequestHandler<GetUserCalendarQuery, Result<CalendarDto>>
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly ICacheService _cacheService;

    public GetUserCalendarQueryHandler(
        ICalendarRepository calendarRepository,
        ICacheService cacheService)
    {
        _calendarRepository = calendarRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<CalendarDto>> Handle(GetUserCalendarQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"calendar:user:{request.UserId.Value}";

        var cachedResult = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var calendar = await _calendarRepository.GetByUserIdAsync(request.UserId, cancellationToken);

                if (calendar == null)
                {
                    return null;
                }

                return new CalendarDto(
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
            },
            TimeSpan.FromHours(1)
        );

        if (cachedResult == null)
        {
            return Result<CalendarDto>.Failure($"Calendar for user {request.UserId.Value} not found.");
        }

        return Result<CalendarDto>.Success(cachedResult);
    }
}
