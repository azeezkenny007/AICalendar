using AICalendar.Application.Predictions.Commands.GeneratePredictions;
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Entities;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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
    private readonly IFailedPredictionAttemptRepository _failedPredictionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public BatchPredictionJob(
        ILogger<BatchPredictionJob> logger,
        IMediator mediator,
        IUserRepository userRepository,
        IFailedPredictionAttemptRepository failedPredictionRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _logger = logger;
        _mediator = mediator;
        _userRepository = userRepository;
        _failedPredictionRepository = failedPredictionRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task GeneratePredictionsForAllUsers()
    {
        var batchStartTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Circuit breaker tracking (declared outside try block for wider scope)
        var circuitBreakerTriggered = false;
        var consecutiveCircuitBreakerFailures = 0;
        const int CircuitBreakerThreshold = 2; // Stop after 2 consecutive circuit breaker failures

        // Statistics tracking
        var successCount = 0;
        var failureCount = 0;
        var totalPredictionsGenerated = 0;
        var userTimings = new List<double>();
        var failedAttempts = new List<FailedPredictionAttempt>();
        var targetMonth = string.Empty;

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
            targetMonth = $"{nextMonth.Year:D4}-{nextMonth.Month:D2}";

            _logger.LogInformation(
                "📅 [TARGET MONTH] Generating predictions for {TargetMonth}",
                targetMonth);

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
                        consecutiveCircuitBreakerFailures = 0; // Reset counter on success

                        _logger.LogInformation(
                            "✅ [USER SUCCESS {CurrentUser}/{TotalUsers}] Generated {ItemCount} predictions for user {UserId} " +
                            "in {Duration:F2}s (Model: {ModelVersion}, PredictionId: {PredictionId})",
                            index, userList.Count, result.Value.ItemsGenerated, user.Id,
                            userDuration, result.Value.ModelVersion, result.Value.PredictionId);
                    }
                    else
                    {
                        failureCount++;

                        // Check if this is a circuit breaker failure (very fast failure)
                        var isCircuitBreakerFailure = userDuration < 1.0 &&
                            (result.Error?.Contains("circuit", StringComparison.OrdinalIgnoreCase) == true ||
                             result.Error?.Contains("fallback", StringComparison.OrdinalIgnoreCase) == true);

                        if (isCircuitBreakerFailure)
                        {
                            consecutiveCircuitBreakerFailures++;

                            if (consecutiveCircuitBreakerFailures >= CircuitBreakerThreshold)
                            {
                                circuitBreakerTriggered = true;

                                _logger.LogError(
                                    "🚨 [CIRCUIT BREAKER DETECTED] AI service circuit breaker is open. " +
                                    "Stopping batch processing to prevent resource waste. " +
                                    "Remaining {RemainingUsers} users will be tracked for retry.",
                                    userList.Count - index);
                            }
                        }
                        else
                        {
                            consecutiveCircuitBreakerFailures = 0; // Reset if not a circuit breaker failure
                        }

                        // Track failed attempt for retry (upsert logic)
                        var failedAttempt = await TrackFailedAttemptAsync(
                            userId: user.Id,
                            targetMonth: targetMonth,
                            errorMessage: result.Error ?? "Unknown error");
                        failedAttempts.Add(failedAttempt);

                        _logger.LogWarning(
                            "❌ [USER FAILED {CurrentUser}/{TotalUsers}] Failed to generate predictions for user {UserId} " +
                            "after {Duration:F2}s: {Error}. Tracked for retry.",
                            index, userList.Count, user.Id, userDuration, result.Error);

                        // Break early if circuit breaker is triggered
                        if (circuitBreakerTriggered)
                        {
                            // Track all remaining users as failed
                            var remainingUsers = userList.Skip(index).ToList();
                            foreach (var remainingUser in remainingUsers)
                            {
                                var remainingFailedAttempt = await TrackFailedAttemptAsync(
                                    userId: remainingUser.Id,
                                    targetMonth: targetMonth,
                                    errorMessage: "Batch processing stopped due to circuit breaker. Will retry later.");
                                failedAttempts.Add(remainingFailedAttempt);
                                failureCount++;
                            }

                            _logger.LogWarning(
                                "⏭️ [BATCH STOPPED] Skipped {SkippedCount} remaining users. " +
                                "Total failed users: {TotalFailed}. Retry job will be scheduled.",
                                remainingUsers.Count, failureCount);

                            break; // Exit the loop
                        }
                    }
                }
                catch (Exception ex)
                {
                    userStopwatch.Stop();
                    failureCount++;
                    var userDuration = userStopwatch.Elapsed.TotalSeconds;
                    userTimings.Add(userDuration);

                    // Track failed attempt for retry (upsert logic)
                    var failedAttempt = await TrackFailedAttemptAsync(
                        userId: user.Id,
                        targetMonth: targetMonth,
                        errorMessage: ex.Message);
                    failedAttempts.Add(failedAttempt);

                    _logger.LogError(ex,
                        "💥 [USER ERROR {CurrentUser}/{TotalUsers}] Exception while generating predictions for user {UserId} " +
                        "after {Duration:F2}s. Tracked for retry.",
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

            // Save failed attempts for retry (already tracked individually via upsert)
            if (failedAttempts.Any())
            {
                try
                {
                    // Commit all the individual upserts
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation(
                        "💾 [FAILED ATTEMPTS SAVED] Saved {Count} failed attempts for retry",
                        failedAttempts.Count);

                    // Auto-schedule retry job if circuit breaker was triggered
                    if (circuitBreakerTriggered)
                    {
                        var delayMinutes = _configuration.GetValue<int>("AIService:Resilience:Fallback:RetryJobDelayMinutes", 60);
                        var retryDelay = TimeSpan.FromMinutes(delayMinutes);

                        Hangfire.BackgroundJob.Schedule(
                            () => RetryFailedPredictions(targetMonth),
                            retryDelay);

                        _logger.LogWarning(
                            "🔄 [AUTO-RETRY SCHEDULED] Retry job scheduled to run in {Delay} minute(s) at {RetryTime:yyyy-MM-dd HH:mm:ss} UTC. " +
                            "Will retry {Count} failed users.",
                            retryDelay.TotalMinutes, DateTime.UtcNow.Add(retryDelay), failedAttempts.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ [SAVE FAILED] Failed to save {Count} failed attempts to database",
                        failedAttempts.Count);
                }
            }
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

    /// <summary>
    /// Retries failed prediction attempts for a specific target month
    /// This method is automatically scheduled when circuit breaker opens
    /// </summary>
    public async Task RetryFailedPredictions(string targetMonth)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "═══════════════════════════════════════════════════════════════════");
            _logger.LogInformation(
                "🔄 [RETRY START] Starting retry of failed predictions for month {TargetMonth}",
                targetMonth);
            _logger.LogInformation(
                "═══════════════════════════════════════════════════════════════════");

            // Get all unresolved failed attempts for this month
            var failedAttempts = await _failedPredictionRepository.GetUnresolvedAttemptsAsync(targetMonth);

            if (!failedAttempts.Any())
            {
                _logger.LogInformation(
                    "✅ [NO RETRIES NEEDED] No unresolved failed attempts found for month {TargetMonth}",
                    targetMonth);
                return;
            }

            _logger.LogInformation(
                "📋 [RETRY QUEUE] Found {Count} failed attempts to retry for month {TargetMonth}",
                failedAttempts.Count(), targetMonth);

            var successCount = 0;
            var failureCount = 0;
            var totalPredictionsGenerated = 0;

            foreach (var (attempt, index) in failedAttempts.Select((a, i) => (a, i + 1)))
            {
                try
                {
                    _logger.LogInformation(
                        "🔄 [RETRY {Current}/{Total}] Retrying user {UserId} (Attempt #{RetryCount})",
                        index, failedAttempts.Count(), attempt.UserId, attempt.RetryCount + 1);

                    var command = new GeneratePredictionsCommand(
                        UserId: attempt.UserId,
                        TargetMonth: attempt.TargetMonth,
                        MaxPredictions: 10,
                        Timezone: "UTC");

                    var result = await _mediator.Send(command);

                    if (result.IsSuccess)
                    {
                        successCount++;
                        totalPredictionsGenerated += result.Value.ItemsGenerated;

                        // Mark as resolved
                        attempt.MarkAsResolved();

                        _logger.LogInformation(
                            "✅ [RETRY SUCCESS {Current}/{Total}] Generated {ItemCount} predictions for user {UserId}",
                            index, failedAttempts.Count(), result.Value.ItemsGenerated, attempt.UserId);
                    }
                    else
                    {
                        failureCount++;

                        // Increment retry count
                        attempt.IncrementRetryCount();

                        _logger.LogWarning(
                            "❌ [RETRY FAILED {Current}/{Total}] Failed to generate predictions for user {UserId}: {Error}",
                            index, failedAttempts.Count(), attempt.UserId, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    attempt.IncrementRetryCount();

                    _logger.LogError(ex,
                        "❌ [RETRY ERROR {Current}/{Total}] Exception while retrying user {UserId}",
                        index, failedAttempts.Count(), attempt.UserId);
                }
            }

            // Save updated attempts
            await _unitOfWork.SaveChangesAsync();

            stopwatch.Stop();

            _logger.LogInformation(
                "═══════════════════════════════════════════════════════════════════");
            _logger.LogInformation(
                "🎉 [RETRY COMPLETE] Retry job completed!");
            _logger.LogInformation(
                "═══════════════════════════════════════════════════════════════════");
            _logger.LogInformation(
                "📊 [RETRY STATISTICS]");
            _logger.LogInformation(
                "   • Total Attempts Retried: {Total}",
                failedAttempts.Count());
            _logger.LogInformation(
                "   • Successful: {SuccessCount}",
                successCount);
            _logger.LogInformation(
                "   • Failed: {FailureCount}",
                failureCount);
            _logger.LogInformation(
                "   • Total Predictions Generated: {TotalPredictions}",
                totalPredictionsGenerated);
            _logger.LogInformation(
                "   • Execution Time: {Duration:F2}s",
                stopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation(
                "═══════════════════════════════════════════════════════════════════");

            if (failureCount > 0)
            {
                _logger.LogError(
                    "🚨 [ADMIN ALERT] Retry job finished with {FailureCount} failures out of {Total} attempts for month {TargetMonth}. " +
                    "These users are still unresolved and require attention.",
                    failureCount, failedAttempts.Count(), targetMonth);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "💥 [RETRY ERROR] Critical error during retry job after {Duration:F2}s",
                stopwatch.Elapsed.TotalSeconds);
            throw;
        }
    }

    /// <summary>
    /// Tracks a failed prediction attempt. If an unresolved attempt already exists for this user/month,
    /// it updates the existing record. Otherwise, creates a new one.
    /// </summary>
    private async Task<FailedPredictionAttempt> TrackFailedAttemptAsync(
        UserId userId,
        string targetMonth,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        // Check if an unresolved attempt already exists
        var existingAttempt = await _failedPredictionRepository.GetByUserAndMonthAsync(
            userId,
            targetMonth,
            cancellationToken);

        if (existingAttempt != null)
        {
            // Update existing attempt
            existingAttempt.UpdateError(errorMessage);
            _failedPredictionRepository.Update(existingAttempt);
            return existingAttempt;
        }

        // Create new attempt
        var newAttempt = FailedPredictionAttempt.Create(
            userId,
            targetMonth,
            errorMessage);

        await _failedPredictionRepository.AddAsync(newAttempt, cancellationToken);
        return newAttempt;
    }
}
