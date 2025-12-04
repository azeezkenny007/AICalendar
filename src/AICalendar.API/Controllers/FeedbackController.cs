using AICalendar.Application.Feedback.Queries.GetUserFeedbackStats;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

/// <summary>
/// Manages user feedback statistics and analytics
/// </summary>
[ApiController]
[Route("api/feedback")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedbackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves comprehensive feedback statistics for a specific user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>User feedback statistics including response counts, accuracy metrics, and engagement data</returns>
    /// <response code="200">Returns the user's feedback statistics with detailed analytics</response>
    /// <response code="404">If the user with the specified ID is not found or has no feedback data</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/feedback/stats/user/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// This endpoint provides insights into user engagement with AI predictions and calendar items.
    /// Statistics include total feedback count, positive/negative response ratios, accuracy percentages,
    /// and other metrics useful for improving the AI prediction model and understanding user satisfaction.
    /// </remarks>
    [HttpGet("stats/user/{userId:guid}")]
    [ProducesResponseType(typeof(FeedbackStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserStats(Guid userId)
    {
        var query = new GetUserFeedbackStatsQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}