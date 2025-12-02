using AICalendar.Application.Predictions.Commands.BatchProcessPredictionItems;
using AICalendar.Application.Predictions.Commands.CreateTestPrediction;
using AICalendar.Application.Predictions.Commands.EditPredictionItem;
using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Application.Predictions.Queries.GetUserPredictions;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/predictions")]
public class PredictionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PredictionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{predictionId:guid}")]
    public async Task<IActionResult> GetPrediction(Guid predictionId)
    {
        var query = new GetPredictionQuery(PredictionId.Create(predictionId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserPredictions(Guid userId)
    {
        var query = new GetUserPredictionsQuery(
            UserId.Create(userId)
        );
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("items/{itemId:guid}")]
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

    [HttpPost("batch-process")]
    public async Task<IActionResult> BatchProcess([FromBody] BatchProcessRequest request)
    {
        var command = new BatchProcessPredictionItemsCommand(
            request.AcceptedItemIds.Select(id => PredictionItemId.Create(id)).ToList(),
            request.RejectedItemIds.Select(id => PredictionItemId.Create(id)).ToList()
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("test-seed")]
    public async Task<IActionResult> CreateTestPrediction()
    {
        var command = new CreateTestPredictionCommand();
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}

public record EditItemRequest(
    string? Merchant,
    decimal? Amount,
    DateTime? DueDate,
    string? Account,
    string? AccountName,
    string? Description
);
public record BatchProcessRequest(List<Guid> AcceptedItemIds, List<Guid> RejectedItemIds);
