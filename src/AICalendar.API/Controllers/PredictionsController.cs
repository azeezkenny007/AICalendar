using AICalendar.Application.Predictions.Commands.AcceptPredictionItem;
using AICalendar.Application.Predictions.Commands.EditPredictionItem;
using AICalendar.Application.Predictions.Commands.RejectPredictionItem;
using AICalendar.Application.Predictions.Queries.GetPrediction;
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

    [HttpPost("items/{itemId:guid}/accept")]
    public async Task<IActionResult> AcceptItem(Guid itemId)
    {
        var command = new AcceptPredictionItemCommand(
            PredictionItemId.Create(itemId)
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("items/{itemId:guid}/reject")]
    public async Task<IActionResult> RejectItem(Guid itemId)
    {
        var command = new RejectPredictionItemCommand(
            PredictionItemId.Create(itemId)
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> EditItem(Guid itemId, [FromBody] EditItemRequest request)
    {
        var command = new EditPredictionItemCommand(
            PredictionItemId.Create(itemId),
            request.Merchant,
            request.Amount,
            request.DueDate
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("test-seed")]
    public async Task<IActionResult> CreateTestPrediction()
    {
        // Use a dummy user ID for testing
        var userId = AICalendar.Domain.ValueObjects.UserId.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var command = new AICalendar.Application.Predictions.Commands.CreateTestPrediction.CreateTestPredictionCommand(userId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { PredictionId = result.Value.Value })
            : BadRequest(result.Error);
    }
}

public record EditItemRequest(string Merchant, decimal Amount, DateTime DueDate);
