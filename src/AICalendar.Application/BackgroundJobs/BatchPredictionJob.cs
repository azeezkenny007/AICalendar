using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.BackgroundJobs;

/// <summary>
/// Hangfire job that generates predictions for all active users
/// Runs nightly at 2 AM UTC
/// </summary>
public class BatchPredictionJob
{
    private readonly ILogger<BatchPredictionJob> _logger;
    private readonly IMediator _mediator;

    public BatchPredictionJob(
        ILogger<BatchPredictionJob> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task GeneratePredictionsForAllUsers()
    {
        _logger.LogInformation("Starting batch prediction generation at {Time}", DateTime.UtcNow);

        try
        {
            // TODO: Implement when UserRepository and GeneratePredictionsCommand are ready
            // 1. Get all active users
            // var users = await _userRepository.GetActiveUsersAsync();

            // 2. For each user, generate predictions
            // foreach (var user in users)
            // {
            //     var command = new GeneratePredictionsCommand(
            //         UserId: user.UserId,
            //         Cycle: PredictionCycle.Monthly(DateTime.UtcNow.Year, DateTime.UtcNow.Month)
            //     );
            //
            //     await _mediator.Send(command);
            // }

            _logger.LogInformation("Batch prediction generation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch prediction generation");
            throw;
        }
    }
}
