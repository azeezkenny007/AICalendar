// src/AICalendar.API/Controllers/HealthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using AICalendar.Infrastructure.Data;
using AICalendar.Domain.Interfaces;
using System.Diagnostics;
using System.Net.Http;
using Hangfire;
using Hangfire.Storage;

namespace AICalendar.API.Controllers;

/// <summary>
/// Provides health check endpoints for monitoring application and infrastructure status
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly AICalendar.Application.Common.Interfaces.ICacheService _cacheService;

    public HealthController(
        ILogger<HealthController> logger,
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        AICalendar.Application.Common.Interfaces.ICacheService cacheService,
        IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _cacheService = cacheService;
        _redis = redis;
    }

    /// <summary>
    /// Basic health check endpoint that returns OK if the API is running
    /// </summary>
    /// <returns>Health status with timestamp</returns>
    /// <response code="200">API is running and responsive</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /health
    ///
    /// This lightweight endpoint is used by Docker healthchecks and load balancers.
    /// It only verifies the API process is running, not infrastructure dependencies.
    /// For detailed infrastructure checks, use GET /health/detailed
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Comprehensive health check that verifies all infrastructure dependencies
    /// </summary>
    /// <returns>Detailed health status for all system components</returns>
    /// <response code="200">All systems are healthy</response>
    /// <response code="503">One or more systems are unhealthy (service unavailable)</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /health/detailed
    ///
    /// This endpoint checks:
    /// - Database connectivity (PostgreSQL)
    /// - Redis cache connectivity (if configured)
    /// - API internal services and DI container
    /// - Hangfire background job server
    /// - Prometheus metrics collection service
    /// - Grafana monitoring dashboard service
    ///
    /// Use this for monitoring dashboards and alerting systems.
    /// </remarks>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDetailed()
    {
        var databaseStatus = await CheckDatabase();
        var redisStatus = await CheckRedis();
        var apiStatus = CheckApi(); // Synchronous check - no async operations needed
        var hangfireStatus = CheckHangfire();
        var prometheusStatus = await CheckPrometheus();
        var grafanaStatus = await CheckGrafana();

        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            checks = new
            {
                database = databaseStatus,
                redis = redisStatus,
                apihealth = apiStatus,
                hangfire = hangfireStatus,
                prometheus = prometheusStatus,
                grafana = grafanaStatus
            }
        };

        var allHealthy = health.checks.database == "healthy"
                      && (health.checks.redis == "healthy" || health.checks.redis == "not_configured")
                      && health.checks.apihealth == "healthy"
                      && health.checks.hangfire == "healthy"
                      && health.checks.prometheus == "healthy"
                      && health.checks.grafana == "healthy";

        return allHealthy
            ? Ok(health)
            : StatusCode(503, health);
    }

    /// <summary>
    /// Tests the cache service functionality with read/write operations
    /// </summary>
    /// <returns>Cache test results with operation status</returns>
    /// <response code="200">Cache operations completed (check status field for success/failure)</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /health/cache-test
    ///
    /// This endpoint performs three cache operations:
    /// 1. SetAsync - Writes a test value to cache
    /// 2. GetAsync - Retrieves the written value
    /// 3. GetOrSetAsync - Tests the cache-or-create pattern
    ///
    /// Use this for diagnosing cache issues in development/staging environments.
    /// </remarks>
    [HttpGet("cache-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestCache()
    {
        var key = $"health_check_{Guid.NewGuid()}";
        var expectedValue = "cache_working";

        // Test Set
        await _cacheService.SetAsync(key, expectedValue, TimeSpan.FromMinutes(1));

        // Test Get
        var value = await _cacheService.GetAsync<string>(key);

        // Test GetOrSet
        var getOrSetKey = $"health_check_getorset_{Guid.NewGuid()}";
        var getOrSetValue = await _cacheService.GetOrSetAsync(
            getOrSetKey,
            () => Task.FromResult("generated_value"),
            TimeSpan.FromMinutes(1));

        return Ok(new
        {
            status = value == expectedValue ? "success" : "failed",
            directGet = value,
            getOrSet = getOrSetValue,
            timestamp = DateTime.UtcNow
        });
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

    /// <summary>
    /// Checks if Prometheus is functioning properly by verifying:
    /// 1. Prometheus service is accessible via HTTP
    /// 2. Can retrieve basic metrics endpoint
    /// </summary>
    private async Task<string> CheckPrometheus()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Try to access Prometheus health endpoint
            var response = await client.GetAsync("http://prometheus:9090/-/ready");

            if (response.IsSuccessStatusCode)
            {
                return "healthy";
            }
            else
            {
                _logger.LogWarning("Prometheus health check returned status code: {StatusCode}", response.StatusCode);
                return "unhealthy";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Prometheus health check failed - service may not be running");
            return "unhealthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prometheus health check failed with exception: {Exception}", ex.Message);
            return "unhealthy";
        }
    }

    /// <summary>
    /// Checks if Grafana is functioning properly by verifying:
    /// 1. Grafana service is accessible via HTTP
    /// 2. Can access the health endpoint
    /// </summary>
    private async Task<string> CheckGrafana()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Try to access Grafana health endpoint
            var response = await client.GetAsync("http://grafana:3000/api/health");

            if (response.IsSuccessStatusCode)
            {
                return "healthy";
            }
            else
            {
                _logger.LogWarning("Grafana health check returned status code: {StatusCode}", response.StatusCode);
                return "unhealthy";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Grafana health check failed - service may not be running");
            return "unhealthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Grafana health check failed with exception: {Exception}", ex.Message);
            return "unhealthy";
        }
    }
}
