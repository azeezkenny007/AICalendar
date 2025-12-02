using AICalendar.Application.Calendar.Commands.EditCalendarItem;
using AICalendar.Application.Calendar.Commands.MarkItemAsPaid;
using AICalendar.Application.Calendar.Queries.GetUserCalendar;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

/// <summary>
/// Manages user calendar operations including viewing, editing, and payment tracking
/// </summary>
[ApiController]
[Route("api/calendar")]
[Produces("application/json")]
public class CalendarController : ControllerBase
{
    private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves the calendar for a specific user with all calendar items
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>The user's calendar containing all scheduled payment items</returns>
    /// <response code="200">Returns the user's calendar with all items. Results are cached for 1 hour.</response>
    /// <response code="404">If the calendar for the specified user is not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/calendar/user/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// This endpoint is cached for performance. The cache is automatically invalidated when calendar items are modified.
    /// </remarks>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(CalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserCalendar(Guid userId)
    {
        var query = new GetUserCalendarQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Updates an existing calendar item with new details
    /// </summary>
    /// <param name="itemId">The unique identifier of the calendar item to edit</param>
    /// <param name="request">The updated calendar item details</param>
    /// <returns>Success status of the update operation</returns>
    /// <response code="200">If the calendar item was successfully updated. Cache is automatically invalidated.</response>
    /// <response code="400">If the update request is invalid or the item cannot be updated</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/calendar/items/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///       "merchant": "Netflix",
    ///       "amount": 15.99,
    ///       "dueDate": "2025-01-15T00:00:00Z",
    ///       "account": "Credit Card",
    ///       "accountName": "Chase Visa",
    ///       "description": "Monthly subscription"
    ///     }
    ///
    /// All fields are required. This operation invalidates the calendar cache for the associated user.
    /// </remarks>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
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

        return result.StatusCode switch
        {
            200 => Ok(new SuccessResponse(result.Title!, result.Detail!, result.Data)),
            404 => NotFound(new ErrorResponse(result.Title!, result.Error!, result.Detail!)),
            409 => Conflict(new ErrorResponse(result.Title!, result.Error!, result.Detail!)),
            _ => BadRequest(new ErrorResponse(result.Title!, result.Error!, result.Detail!))
        };
    }

    /// <summary>
    /// Marks a calendar item as paid with a specific payment date
    /// </summary>
    /// <param name="itemId">The unique identifier of the calendar item to mark as paid</param>
    /// <param name="request">The payment date information</param>
    /// <returns>Success status of the operation</returns>
    /// <response code="200">If the item was successfully marked as paid. Cache is automatically invalidated.</response>
    /// <response code="400">If the request is invalid or the item cannot be marked as paid</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/calendar/items/3fa85f64-5717-4562-b3fc-2c963f66afa6/mark-paid
    ///     {
    ///       "paidDate": "2025-01-10T14:30:00Z"
    ///     }
    ///
    /// This operation updates the payment status and invalidates the calendar cache for the associated user.
    /// </remarks>
    [HttpPost("items/{itemId:guid}/mark-paid")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkItemAsPaid(Guid itemId, [FromBody] MarkPaidRequest request)
    {
        var command = new MarkItemAsPaidCommand(
            CalendarItemId.Create(itemId),
            request.PaidDate
        );

        var result = await _mediator.Send(command);

        return result.StatusCode switch
        {
            200 => Ok(new SuccessResponse(result.Title!, result.Detail!, result.Data)),
            404 => NotFound(new ErrorResponse(result.Title!, result.Error!, result.Detail!)),
            _ => BadRequest(new ErrorResponse(result.Title!, result.Error!, result.Detail!))
        };
    }
}

/// <summary>
/// Request model for editing a calendar item
/// </summary>
/// <param name="Merchant">The merchant or payee name (required)</param>
/// <param name="Amount">The payment amount in decimal format (required)</param>
/// <param name="DueDate">The date when the payment is due (required)</param>
/// <param name="Account">The account used for payment (optional)</param>
/// <param name="AccountName">The display name of the account (optional)</param>
/// <param name="Description">Additional notes or description (optional)</param>
public record EditCalendarItemRequest(
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    string? Account = null,
    string? AccountName = null,
    string? Description = null
);

/// <summary>
/// Request model for marking a calendar item as paid
/// </summary>
/// <param name="PaidDate">The date when the payment was made</param>
public record MarkPaidRequest(DateTime PaidDate);

/// <summary>
/// Standard success response format
/// </summary>
/// <param name="Title">Short title of the success message</param>
/// <param name="Message">Detailed success message</param>
/// <param name="Data">Additional data related to the success (optional)</param>
public record SuccessResponse(string Title, string Message, object? Data = null);

/// <summary>
/// Standard error response format
/// </summary>
/// <param name="Title">Short title of the error</param>
/// <param name="Error">The error message from the domain/application layer</param>
/// <param name="Detail">User-friendly explanation of the error</param>
public record ErrorResponse(string Title, string Error, string Detail);
