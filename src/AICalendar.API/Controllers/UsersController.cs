using AICalendar.Application.Users;
using AICalendar.Application.Users.Queries.GetAllUsers;
using AICalendar.Application.Users.Queries.GetUserById;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.API.Controllers;

/// <summary>
/// Manages user operations including viewing user information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all users in the system
    /// </summary>
    /// <returns>A list of all users</returns>
    /// <response code="200">Returns the list of all users</response>
    /// <response code="500">If there was an error retrieving users</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/users
    ///
    /// Sample 200 response:
    ///
    ///     [
    ///       {
    ///         "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "email": "user@example.com",
    ///         "name": "John Doe",
    ///         "createdAt": "2025-01-01T10:00:00Z",
    ///         "hasPushNotifications": true
    ///       },
    ///       {
    ///         "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    ///         "email": "jane@example.com",
    ///         "name": "Jane Smith",
    ///         "createdAt": "2025-01-05T14:30:00Z",
    ///         "hasPushNotifications": false
    ///       }
    ///     ]
    ///
    /// Sample 500 response:
    ///
    ///     "An error occurred while retrieving users"
    ///
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsers()
    {
        _logger.LogInformation("Getting all users");

        var query = new GetAllUsersQuery();
        var result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        _logger.LogError("Failed to retrieve users: {Error}", result.Error);
        return StatusCode(500, result.Error);
    }

    /// <summary>
    /// Retrieves a specific user by their ID
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>The user with the specified ID</returns>
    /// <response code="200">Returns the user</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If there was an error retrieving the user</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// Sample 200 response:
    ///
    ///     {
    ///       "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "email": "user@example.com",
    ///       "name": "John Doe",
    ///       "createdAt": "2025-01-01T10:00:00Z",
    ///       "hasPushNotifications": true,
    ///       "totalPredictions": 15,
    ///       "totalCalendarItems": 42
    ///     }
    ///
    /// Sample 404 response:
    ///
    ///     "User not found"
    ///
    /// Sample 500 response:
    ///
    ///     "An error occurred while retrieving the user"
    ///
    /// </remarks>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", userId);

        var query = new GetUserByIdQuery(UserId.Create(userId));
        var result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(result.Error);
        }

        _logger.LogError("Failed to retrieve user {UserId}: {Error}", userId, result.Error);
        return StatusCode(500, result.Error);
    }
}
