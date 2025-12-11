using AICalendar.Application.Calendar.Commands.DeleteAllCalendarData;
using AICalendar.Application.Calendar.Commands.EditCalendarItem;
using AICalendar.Application.Calendar.Commands.MarkItemAsPaid;
using AICalendar.Application.Calendar.Queries.GetUserCalendar;
using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

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
    /// Sample 200 response:
    ///
    ///     {
    ///       "calendarId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "items": [
    ///         {
    ///           "itemId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    ///           "merchant": "Netflix",
    ///           "amount": 15.99,
    ///           "dueDate": "2025-01-15T00:00:00Z",
    ///           "isPaid": false,
    ///           "account": "Credit Card",
    ///           "description": "Monthly subscription"
    ///         }
    ///       ]
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     "Calendar not found for user"
    ///
    /// This endpoint is cached for performance. The cache is automatically invalidated when calendar items are modified.
    /// </remarks>
    [HttpGet("user/{userId:guid}")]
    [OutputCache(PolicyName = "calendar-short")]
    [ProducesResponseType(typeof(CalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserCalendar(Guid userId)
    {
        var query = new GetUserCalendarQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Updates an existing calendar item with new details (partial update supported)
    /// </summary>
    /// <param name="itemId">The unique identifier of the calendar item to edit</param>
    /// <param name="request">The updated calendar item details (only provide fields you want to update)</param>
    /// <returns>Success status of the update operation</returns>
    /// <response code="200">If the calendar item was successfully updated. Cache is automatically invalidated.</response>
    /// <response code="400">If the update request is invalid or the item cannot be updated</response>
    /// <response code="404">If the calendar item is not found</response>
    /// <response code="409">If there is a conflict with the update (e.g., duplicate entry)</response>
    /// <remarks>
    /// Sample request (update only merchant and amount):
    ///
    ///     PUT /api/calendar/items/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///       "merchant": "Netflix Premium",
    ///       "amount": 19.99
    ///     }
    ///
    /// Sample request (update only due date):
    ///
    ///     PUT /api/calendar/items/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///       "dueDate": "2025-02-01T00:00:00Z"
    ///     }
    ///
    /// Sample 200 response:
    ///
    ///     {
    ///       "title": "Item Updated",
    ///       "message": "Calendar item updated successfully",
    ///       "data": null
    ///     }
    ///
    /// Sample 400 response:
    ///
    ///     {
    ///       "title": "Validation Error",
    ///       "error": "At least one field must be provided",
    ///       "detail": "No fields were provided for update"
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     {
    ///       "title": "Not Found",
    ///       "error": "Calendar item not found",
    ///       "detail": "The specified calendar item does not exist"
    ///     }
    ///
    /// All fields are optional. Only provide the fields you want to update.
    /// At least one field must be provided. This operation invalidates the calendar cache for the associated user.
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
    /// <response code="404">If the calendar item is not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/calendar/items/3fa85f64-5717-4562-b3fc-2c963f66afa6/mark-paid
    ///     {
    ///       "paidDate": "2025-01-10T14:30:00Z"
    ///     }
    ///
    /// Sample 200 response:
    ///
    ///     {
    ///       "title": "Payment Recorded",
    ///       "message": "Calendar item marked as paid successfully",
    ///       "data": null
    ///     }
    ///
    /// Sample 400 response:
    ///
    ///     {
    ///       "title": "Invalid Request",
    ///       "error": "Invalid payment date",
    ///       "detail": "Payment date cannot be in the future"
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     {
    ///       "title": "Not Found",
    ///       "error": "Calendar item not found",
    ///       "detail": "The specified calendar item does not exist"
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

    /// <summary>
    /// Deletes all calendar data from the system (Admin operation)
    /// </summary>
    /// <returns>The number of calendar records deleted</returns>
    /// <response code="200">Successfully deleted all calendar data</response>
    /// <response code="400">If there was an error during deletion</response>
    /// <remarks>
    /// This is an administrative operation that removes all calendar records from the system.
    /// Use with caution as this action cannot be undone.
    ///
    /// Sample request:
    ///
    ///     DELETE /api/calendar/delete-all
    ///
    /// Sample 200 response:
    ///
    ///     {
    ///       "title": "Success",
    ///       "message": "All calendar data has been deleted successfully",
    ///       "data": {
    ///         "deletedRecordCount": 5
    ///       }
    ///     }
    /// </remarks>
    [HttpDelete("delete-all")]
    public async Task<IActionResult> DeleteAllCalendarData()
    {
        var result = await _mediator.Send(new DeleteAllCalendarDataCommand());
        
        if (result.IsSuccess && result.Value != null)
        {
            return Ok(new SuccessResponse(
                "Success",
                "All calendar data has been deleted successfully",
                new { deletedRecordCount = result.Value.DeletedRecordCount }
            ));
        }

        return BadRequest(new ErrorResponse(
            "Delete Failed",
            result.Error,
            "An error occurred while deleting calendar data"
        ));
    }
}

/// <summary>
/// Request model for editing a calendar item
/// </summary>
/// <param name="Merchant">The merchant or payee name (optional - only provide fields you want to update)</param>
/// <param name="Amount">The payment amount in decimal format (optional - only provide fields you want to update)</param>
/// <param name="DueDate">The date when the payment is due (optional - only provide fields you want to update)</param>
/// <param name="Account">The account used for payment (optional)</param>
/// <param name="AccountName">The display name of the account (optional)</param>
/// <param name="Description">Additional notes or description (optional)</param>
public record EditCalendarItemRequest(
    string? Merchant = null,
    decimal? Amount = null,
    DateTime? DueDate = null,
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
