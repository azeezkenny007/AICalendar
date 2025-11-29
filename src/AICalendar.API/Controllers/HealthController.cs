// src/AICalendar.API/Controllers/HealthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using AICalendar.Infrastructure.Data;
using AICalendar.Domain.Interfaces;
using System.Diagnostics;
using Hangfire;
using Hangfire.Storage;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public HealthController(
        ILogger<HealthController> logger,
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _redis = redis;
    }

    /// <summary>
    /// Simple health check - just returns OK
    /// Used by Docker healthcheck
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Detailed health check - checks all dependencies
    /// Use this for monitoring
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var databaseStatus = await CheckDatabase();
        var redisStatus = await CheckRedis();
        var apiStatus = CheckApi(); // Synchronous check - no async operations needed
        var hangfireStatus = CheckHangfire();

        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            checks = new
            {
                database = databaseStatus,
                redis = redisStatus,
                apihealth = apiStatus,
                hangfire = hangfireStatus
            }
        };

        var allHealthy = health.checks.database == "healthy"
                      && (health.checks.redis == "healthy" || health.checks.redis == "not_configured")
                      && health.checks.apihealth == "healthy"
                      && health.checks.hangfire == "healthy";

        return allHealthy
            ? Ok(health)
            : StatusCode(503, health);
    }

    private async Task<string> CheckDatabase()
    {
        try
        {
            await _dbContext.Database.CanConnectAsync();
            return "healthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return "unhealthy";
        }
    }

    private async Task<string> CheckRedis()
    {
        if (_redis == null)
        {
            return "not_configured";
        }

        try
        {
            if (!_redis.IsConnected)
            {
                return "unhealthy";
            }

            var db = _redis.GetDatabase();
            await db.PingAsync();
            return "healthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return "unhealthy";
        }
    }

    /// <summary>
    /// Checks if the API is functioning properly by verifying:
    /// 1. Critical services are registered and can be resolved from DI container
    /// 2. Application domain is loaded and responsive
    /// 3. Memory usage is within acceptable limits (monitoring only)
    ///
    /// Note: The fact that we can respond to this request already proves
    /// the API is at least partially functional. This check verifies internal state.
    /// </summary>
    private string CheckApi()
    {
        try
        {
            // 1. Verify critical services are registered and can be resolved
            // This ensures dependency injection is working correctly
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            if (unitOfWork == null)
            {
                _logger.LogError("Critical service IUnitOfWork is not registered - API is in degraded state");
                return "unhealthy";
            }

            // 2. Verify ApplicationDbContext can be resolved (already injected, but double-check)
            var dbContext = _serviceProvider.GetService<ApplicationDbContext>();
            if (dbContext == null)
            {
                _logger.LogError("ApplicationDbContext is not available - API cannot function");
                return "unhealthy";
            }

            // 3. Check memory usage for monitoring purposes
            // This doesn't fail the health check but logs warnings
            var process = Process.GetCurrentProcess();
            var memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
            var warningThresholdMB = 512; // 512MB - log warning
            var criticalThresholdMB = 1024; // 1GB - still healthy but log error

            if (memoryUsageMB > criticalThresholdMB)
            {
                _logger.LogError("Critical memory usage detected: {MemoryMB}MB - API may be unstable", memoryUsageMB);
                // Still return healthy, but this should be monitored
            }
            else if (memoryUsageMB > warningThresholdMB)
            {
                _logger.LogWarning("High memory usage detected: {MemoryMB}MB - monitor closely", memoryUsageMB);
            }

            // 4. Verify the application is responsive
            // If we've reached this point and can execute code, the API is functional
            // The ability to respond to this request already proves basic functionality

            // All checks passed - API is healthy
            return "healthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API health check failed with exception: {Exception}", ex.Message);
            return "unhealthy";
        }
    }

    /// <summary>
    /// Checks if Hangfire is functioning properly by verifying:
    /// 1. Hangfire storage connection is available
    /// 2. Hangfire server is running
    /// 3. Can access Hangfire monitoring API
    /// </summary>
    private string CheckHangfire()
    {
        try
        {
            // Check if Hangfire storage is accessible
            using var connection = JobStorage.Current.GetConnection();
            if (connection == null)
            {
                _logger.LogWarning("Hangfire connection is null");
                return "unhealthy";
            }

            // Try to get server count to verify Hangfire is operational
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var servers = monitoringApi.Servers();

            // Check if at least one Hangfire server is running
            if (servers == null || servers.Count == 0)
            {
                _logger.LogWarning("No Hangfire servers are running");
                return "unhealthy";
            }

            // Verify we can get job statistics (lightweight check)
            var stats = monitoringApi.GetStatistics();
            if (stats == null)
            {
                _logger.LogWarning("Unable to retrieve Hangfire statistics");
                return "unhealthy";
            }

            // All checks passed - Hangfire is healthy
            return "healthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire health check failed: {Exception}", ex.Message);
            return "unhealthy";
        }
    }
}
