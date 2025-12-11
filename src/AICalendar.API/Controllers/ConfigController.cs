using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace AICalendar.API.Controllers;

/// <summary>
/// Configuration endpoint for testing runtime configuration changes
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        IConfiguration configuration,
        IFeatureManager featureManager,
        ILogger<ConfigController> logger)
    {
        _configuration = configuration;
        _featureManager = featureManager;
        _logger = logger;
    }

    /// <summary>
    /// Get current application configuration values
    /// </summary>
    /// <returns>Current configuration settings</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ConfigurationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentConfiguration()
    {
        var config = new ConfigurationResponse
        {
            MaxUploadSizeMB = _configuration.GetValue<int>("ApplicationSettings:MaxUploadSizeMB", 100),
            SessionTimeoutMinutes = _configuration.GetValue<int>("ApplicationSettings:SessionTimeoutMinutes", 30),
            EnableSwagger = _configuration.GetValue<bool>("ApplicationSettings:EnableSwagger", true),
            MaintenanceMode = _configuration.GetValue<bool>("ApplicationSettings:MaintenanceMode", false),
            Features = new FeatureFlagsResponse
            {
                NewDashboard = await _featureManager.IsEnabledAsync("NewDashboard"),
                BetaAccess = await _featureManager.IsEnabledAsync("BetaAccess")
            },
            LastRefreshed = DateTime.UtcNow
        };

        _logger.LogInformation("Configuration retrieved: {@Config}", config);

        return Ok(config);
    }

    /// <summary>
    /// Health check endpoint that respects maintenance mode
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetHealth()
    {
        var maintenanceMode = _configuration.GetValue<bool>("ApplicationSettings:MaintenanceMode", false);

        if (maintenanceMode)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthResponse
            {
                Status = "Maintenance",
                Message = "Application is currently under maintenance. Please try again later.",
                MaintenanceMode = true
            });
        }

        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Message = "Application is running normally",
            MaintenanceMode = false
        });
    }
}

public class ConfigurationResponse
{
    public int MaxUploadSizeMB { get; set; }
    public int SessionTimeoutMinutes { get; set; }
    public bool EnableSwagger { get; set; }
    public bool MaintenanceMode { get; set; }
    public FeatureFlagsResponse Features { get; set; } = new();
    public DateTime LastRefreshed { get; set; }
}

public class FeatureFlagsResponse
{
    public bool NewDashboard { get; set; }
    public bool BetaAccess { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool MaintenanceMode { get; set; }
}
