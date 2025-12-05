using AICalendar.Application.Predictions.Commands.GeneratePredictions;
using AICalendar.Domain.Interfaces;
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
    private readonly IUserRepository _userRepository;

    public BatchPredictionJob(
        ILogger<BatchPredictionJob> logger,
        IMediator mediator,
        IUserRepository userRepository)
    {
        _logger = logger;
        _mediator = mediator;
        _userRepository = userRepository;
    }

    public async Task GeneratePredictionsForAllUsers()
    {
        var batchStartTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation(
            "═══════════════════════════════════════════════════════════════════");
        _logger.LogInformation(
            "🚀 [BATCH START] Starting batch prediction generation at {Time}",
            batchStartTime);
        _logger.LogInformation(
            "═══════════════════════════════════════════════════════════════════");

        try
        {
            // 1. Get all users
            var users = await _userRepository.GetAllAsync();
            var userList = users.ToList();

            if (!userList.Any())
            {
                _logger.LogWarning("⚠️  [NO USERS] No users found for batch prediction generation");
                return;
            }

            _logger.LogInformation(
                "👥 [USERS LOADED] Found {UserCount} users for prediction generation",
                userList.Count);

            // 2. Calculate target month (next month)
            var nextMonth = DateTime.UtcNow.AddMonths(1);
            var targetMonth = $"{nextMonth.Year:D4}-{nextMonth.Month:D2}";

            _logger.LogInformation(
                "📅 [TARGET MONTH] Generating predictions for {TargetMonth}",
                targetMonth);

            var successCount = 0;
            var failureCount = 0;
            var totalPredictionsGenerated = 0;
            var userTimings = new List<double>();

            // 3. Generate predictions for each user
            foreach (var (user, index) in userList.Select((u, i) => (u, i + 1)))
            {
                var userStartTime = DateTime.UtcNow;
                var userStopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    _logger.LogInformation(
                        "───────────────────────────────────────────────────────────────────");
                    _logger.LogInformation(
                        "👤 [USER {CurrentUser}/{TotalUsers}] Processing user {UserId} ({Email}) for month {TargetMonth}",
                        index, userList.Count, user.Id, user.Email, targetMonth);

                    var command = new GeneratePredictionsCommand(
                        UserId: user.Id,
                        TargetMonth: targetMonth,
                        MaxPredictions: 10,
                        Timezone: "UTC");

                    var result = await _mediator.Send(command);

                    userStopwatch.Stop();
                    var userDuration = userStopwatch.Elapsed.TotalSeconds;
                    userTimings.Add(userDuration);

                    if (result.IsSuccess)
                    {
                        successCount++;
                        totalPredictionsGenerated += result.Value.ItemsGenerated;

                        _logger.LogInformation(
                            "✅ [USER SUCCESS {CurrentUser}/{TotalUsers}] Generated {ItemCount} predictions for user {UserId} " +
                            "in {Duration:F2}s (Model: {ModelVersion}, PredictionId: {PredictionId})",
                            index, userList.Count, result.Value.ItemsGenerated, user.Id,
                            userDuration, result.Value.ModelVersion, result.Value.PredictionId);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning(
                            "❌ [USER FAILED {CurrentUser}/{TotalUsers}] Failed to generate predictions for user {UserId} " +
                            "after {Duration:F2}s: {Error}",
                            index, userList.Count, user.Id, userDuration, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    userStopwatch.Stop();
                    failureCount++;
                    var userDuration = userStopwatch.Elapsed.TotalSeconds;
                    userTimings.Add(userDuration);

                    _logger.LogError(ex,
                        "💥 [USER ERROR {CurrentUser}/{TotalUsers}] Exception while generating predictions for user {UserId} " +
                        "after {Duration:F2}s",
                        index, userList.Count, user.Id, userDuration);
                }
            }

            stopwatch.Stop();
            var totalDuration = stopwatch.Elapsed.TotalSeconds;
            var avgTimePerUser = userTimings.Any() ? userTimings.Average() : 0;
            var successRate = userList.Count > 0 ? (successCount * 100.0 / userList.Count) : 0;

            _logger.LogInformation(
                "═══════════════════════════════════════════════════════════════════");
            _logger.LogInformation(
                "🎉 [BATCH COMPLETE] Batch prediction generation completed successfully!");
            _logger.LogInformation(
                "📊 [STATISTICS]");
            _logger.LogInformation(
                "   • Total Users Processed: {TotalUsers}",
                userList.Count);
            _logger.LogInformation(
                "   • Successful: {SuccessCount} ({SuccessRate:F1}%)",
                successCount, successRate);
            _logger.LogInformation(
                "   • Failed: {FailureCount}",
                failureCount);
            _logger.LogInformation(
                "   • Total Predictions Generated: {TotalPredictions}",
                totalPredictionsGenerated);
            _logger.LogInformation(
                "   • Average Predictions per User: {AvgPredictions:F1}",
                successCount > 0 ? (totalPredictionsGenerated * 1.0 / successCount) : 0);
            _logger.LogInformation(
                "⏱️  [TIMING]");
            _logger.LogInformation(
                "   • Total Execution Time: {TotalDuration:F2}s ({Minutes:F1} minutes)",
                totalDuration, totalDuration / 60);
            _logger.LogInformation(
                "   • Average Time per User: {AvgTime:F2}s",
                avgTimePerUser);
            _logger.LogInformation(
                "   • Fastest User: {FastestTime:F2}s",
                userTimings.Any() ? userTimings.Min() : 0);
            _logger.LogInformation(
                "   • Slowest User: {SlowestTime:F2}s",
                userTimings.Any() ? userTimings.Max() : 0);
            _logger.LogInformation(
                "   • Started: {StartTime:yyyy-MM-dd HH:mm:ss} UTC",
                batchStartTime);
            _logger.LogInformation(
                "   • Completed: {EndTime:yyyy-MM-dd HH:mm:ss} UTC",
                DateTime.UtcNow);
            _logger.LogInformation(
                "═══════════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "💥 [BATCH ERROR] Critical error during batch prediction generation after {Duration:F2}s",
                stopwatch.Elapsed.TotalSeconds);
            throw;
        }
    }
}
