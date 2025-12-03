using AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedbackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats/user/{userId:guid}")]
    public async Task<IActionResult> GetUserStats(Guid userId)
    {
        var query = new GetUserFeedbackStatsQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}