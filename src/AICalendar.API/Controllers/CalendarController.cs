using AICalendar.Application.Calendar.Commands.EditCalendarItem;
using AICalendar.Application.Calendar.Commands.MarkItemAsPaid;
using AICalendar.Application.Calendar.Queries.GetUserCalendar;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserCalendar(Guid userId)
    {
        var query = new GetUserCalendarQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> EditCalendarItem(Guid itemId, [FromBody] EditCalendarItemRequest request)
    {
        var command = new EditCalendarItemCommand(
            CalendarItemId.Create(itemId),
            request.Merchant,
            request.Amount,
            request.DueDate,
            request.Account,
            request.AccountName,
            request.Description
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("items/{itemId:guid}/mark-paid")]
    public async Task<IActionResult> MarkItemAsPaid(Guid itemId, [FromBody] MarkPaidRequest request)
    {
        var command = new MarkItemAsPaidCommand(
            CalendarItemId.Create(itemId),
            request.PaidDate
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

public record EditCalendarItemRequest(
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    string? Account = null,
    string? AccountName = null,
    string? Description = null
);

public record MarkPaidRequest(DateTime PaidDate);
