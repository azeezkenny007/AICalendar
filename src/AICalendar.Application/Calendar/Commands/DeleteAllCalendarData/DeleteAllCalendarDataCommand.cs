using AICalendar.Domain.Common;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.DeleteAllCalendarData;

/// <summary>
/// Command to delete all calendar data from the system
/// WARNING: This is a destructive operation that cannot be undone
/// </summary>
public record DeleteAllCalendarDataCommand : IRequest<Result<DeleteAllCalendarDataResponse>>;

/// <summary>
/// Response from deleting all calendar data
/// </summary>
/// <param name="DeletedRecordCount">Number of calendar records that were deleted</param>
public record DeleteAllCalendarDataResponse(int DeletedRecordCount);
