using AICalendar.Application.Calendar.Commands.DiscardPrediction;
using AICalendar.Application.Calendar.Commands.EditTransaction;
using AICalendar.Application.Calendar.Commands.KeepPrediction;
using AICalendar.Application.Calendar.DTOs;
using AICalendar.Application.Calendar.Queries.GetCalendarPredictions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PredictionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(IMediator mediator, ILogger<PredictionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get AI-generated calendar predictions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of predicted transactions with AI analysis</returns>
    /// <response code="200">Returns the calendar predictions</response>
    /// <response code="404">User has no transactions</response>
    [HttpGet("predictions/{userId:guid}")]
    [ProducesResponseType(typeof(CalendarPredictionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCalendarPredictions(Guid userId)
    {
        _logger.LogInformation("Getting calendar predictions for user {UserId}", userId);

        var query = new GetCalendarPredictionsQuery(userId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Mark a prediction as kept (user accepts the prediction)
    /// </summary>
    /// <param name="transactionId">Transaction ID from the prediction</param>
    /// <returns>Success status</returns>
    /// <response code="200">Prediction marked as kept</response>
    [HttpPut("keep/{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> KeepPrediction(Guid transactionId)
    {
        _logger.LogInformation("Keeping prediction for transaction {TransactionId}", transactionId);

        var command = new KeepPredictionCommand(transactionId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Prediction marked as kept", transactionId });
    }

    /// <summary>
    /// Mark a prediction as discarded (user rejects the prediction)
    /// </summary>
    /// <param name="transactionId">Transaction ID from the prediction</param>
    /// <returns>Success status</returns>
    /// <response code="200">Prediction marked as discarded</response>
    [HttpPut("discard/{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DiscardPrediction(Guid transactionId)
    {
        _logger.LogInformation("Discarding prediction for transaction {TransactionId}", transactionId);

        var command = new DiscardPredictionCommand(transactionId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Prediction marked as discarded", transactionId });
    }

    /// <summary>
    /// Edit the transaction associated with a prediction
    /// </summary>
    /// <param name="transactionId">Transaction ID to edit</param>
    /// <param name="editData">Updated transaction data</param>
    /// <returns>Success status</returns>
    /// <response code="200">Transaction updated successfully</response>
    /// <response code="404">Transaction not found</response>
    [HttpPatch("edit/{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditTransaction(Guid transactionId, [FromBody] EditTransactionDto editData)
    {
        _logger.LogInformation("Editing transaction {TransactionId}", transactionId);

        var command = new EditTransactionCommand(transactionId, editData);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(new { message = "Transaction updated and marked as edited", transactionId });
    }
}
