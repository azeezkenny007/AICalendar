namespace AICalendar.API.Middleware;

/// <summary>
/// Middleware that checks for maintenance mode and returns 503 if enabled
/// </summary>
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    public MaintenanceModeMiddleware(
        RequestDelegate next,
        ILogger<MaintenanceModeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // Allow health check endpoints even in maintenance mode
        if (context.Request.Path.StartsWithSegments("/api/config/health") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var maintenanceMode = configuration.GetValue<bool>("ApplicationSettings:MaintenanceMode", false);

        if (maintenanceMode)
        {
            _logger.LogWarning("Request blocked due to maintenance mode: {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = "Maintenance",
                message = "Application is currently under maintenance. Please try again later.",
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to add maintenance mode middleware
/// </summary>
public static class MaintenanceModeMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MaintenanceModeMiddleware>();
    }
}
