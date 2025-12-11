using Microsoft.AspNetCore.Mvc;
using MediatR;
using AICalendar.Application.Outbox.Commands.DeleteAllOutboxMessages;

namespace AICalendar.API.Controllers;

/// <summary>
/// Provides endpoints for managing OutboxMessage records
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OutboxMessageController : ControllerBase
{
    private readonly IMediator _mediator;

    public OutboxMessageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Deletes all rows from the OutboxMessage table
    /// </summary>
    /// <returns>Number of records deleted</returns>
    /// <response code="200">Records deleted successfully</response>
    /// <response code="400">Operation failed</response>
    /// <response code="500">An error occurred while deleting records</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/outboxmessage
    ///
    /// This endpoint will remove all outbox messages from the database. Use with caution.
    /// </remarks>
    [HttpDelete]
    public async Task<ActionResult<object>> DeleteAllOutboxMessages(CancellationToken cancellationToken)
    {
        var command = new DeleteAllOutboxMessagesCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new
            {
                success = true,
                message = "All OutboxMessage records have been deleted successfully",
                deletedCount = result.Value!.DeletedRecordCount,
                timestamp = DateTime.UtcNow
            });
        }

        return BadRequest(new
        {
            success = false,
            message = "An error occurred while deleting records",
            error = result.Error,
            timestamp = DateTime.UtcNow
        });
    }
}
