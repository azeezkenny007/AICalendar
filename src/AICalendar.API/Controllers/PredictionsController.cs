using AICalendar.Application.Predictions.Commands.BatchProcessPredictionItems;
using AICalendar.Application.Predictions.Commands.CreateTestPrediction;
using AICalendar.Application.Predictions.Commands.EditPredictionItem;
using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Application.Predictions.Queries.GetUserPredictions;
using AICalendar.Application.Predictions.Queries.GetUserPredictionsByMonth;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

// dummy code

namespace AICalendar.API.Controllers;

/// <summary>
/// Manages prediction operations including retrieval, editing, and batch processing
/// </summary>
[ApiController]
[Route("api/predictions")]
[Produces("application/json")]
public class PredictionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PredictionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves a specific prediction by its unique identifier
    /// </summary>
    /// <param name="predictionId">The unique identifier of the prediction</param>
    /// <returns>The prediction details if found</returns>
    /// <response code="200">Returns the requested prediction</response>
    /// <response code="404">If the prediction is not found</response>
    [HttpGet("{predictionId:guid}")]
    [ProducesResponseType(typeof(Domain.Aggregates.PredictionAggregate.Prediction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrediction(Guid predictionId)
    {
        var query = new GetPredictionQuery(PredictionId.Create(predictionId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Retrieves all predictions for a specific user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>A list of predictions for the user</returns>
    /// <response code="200">Returns the list of predictions</response>
    /// <response code="404">If no predictions are found for the user</response>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(List<Domain.Aggregates.PredictionAggregate.Prediction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPredictions(Guid userId)
    {
        var query = new GetUserPredictionsQuery(
            UserId.Create(userId)
        );
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Retrieves all predictions for a specific user for a particular month
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="year">The year (e.g., 2024)</param>
    /// <param name="month">The month (1-12, where 1 = January, 11 = November)</param>
    /// <returns>A list of predictions for the user for the specified month</returns>
    /// <response code="200">Returns the list of predictions for the specified month</response>
    /// <response code="400">If the year or month parameters are invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/predictions/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/month/2024/11
    ///
    /// This retrieves all predictions for November 2024 for the specified user.
    /// Month parameter: 1 = January, 2 = February, ..., 11 = November, 12 = December
    /// </remarks>
    [HttpGet("user/{userId:guid}/month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(List<PredictionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPredictionsByMonth(Guid userId, int year, int month)
    {
        var query = new GetUserPredictionsByMonthQuery(
            UserId.Create(userId),
            year,
            month
        );
        var result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        // Check if it's a validation error (400) or not found (404)
        if (result.Error.Contains("not found"))
        {
            return NotFound(result.Error);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Edits a specific item within a prediction
    /// </summary>
    /// <param name="itemId">The unique identifier of the prediction item</param>
    /// <param name="request">The details to update</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the item was successfully updated</response>
    /// <response code="400">If the update request is invalid</response>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EditItem(Guid itemId, [FromBody] EditItemRequest request)
    {
        var command = new EditPredictionItemCommand(
            PredictionItemId.Create(itemId),
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

    /// <summary>
    /// Processes a batch of prediction items, accepting or rejecting them
    /// </summary>
    /// <param name="request">The list of accepted and/or rejected item IDs (at least one array should be provided)</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the batch was successfully processed</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost("batch-process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchProcess([FromBody] BatchProcessRequest request)
    {
        var command = new BatchProcessPredictionItemsCommand(
            (request.AcceptedItemIds ?? new List<Guid>()).Select(id => PredictionItemId.Create(id)).ToList(),
            (request.RejectedItemIds ?? new List<Guid>()).Select(id => PredictionItemId.Create(id)).ToList()
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    /// <summary>
    /// Creates a test prediction with sample data for development purposes
    /// </summary>
    /// <returns>The created test prediction</returns>
    /// <response code="200">Returns the created test prediction</response>
    /// <response code="400">If creation fails</response>
    [HttpPost("test-seed")]
    [ProducesResponseType(typeof(Domain.Aggregates.PredictionAggregate.Prediction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTestPrediction()
    {
        var command = new CreateTestPredictionCommand();
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}

/// <summary>
/// Request model for editing a prediction item
/// </summary>
public record EditItemRequest(
    string? Merchant,
    decimal? Amount,
    DateTime? DueDate,
    string? Account,
    string? AccountName,
    string? Description
);

/// <summary>
/// Request model for batch processing prediction items.
/// At least one of AcceptedItemIds or RejectedItemIds should be provided.
/// </summary>
/// <param name="AcceptedItemIds">Optional list of prediction item IDs to accept</param>
/// <param name="RejectedItemIds">Optional list of prediction item IDs to reject</param>
public record BatchProcessRequest(List<Guid>? AcceptedItemIds, List<Guid>? RejectedItemIds);
