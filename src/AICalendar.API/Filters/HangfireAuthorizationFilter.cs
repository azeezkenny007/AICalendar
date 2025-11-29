using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace AICalendar.API.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// In Development: Allow all access
/// In Production: Requires authentication (implement based on your auth system)
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // In Development: Allow all access
        if (environment == "Development" || environment == null)
        {
            return true;
        }

        // In Production: Require authentication
        // TODO: Implement your authentication logic here
        // Examples:
        // 1. Check if user is authenticated: context.GetHttpContext().User.Identity?.IsAuthenticated == true
        // 2. Check for specific role: context.GetHttpContext().User.IsInRole("Admin")
        // 3. Check API key from headers
        // 4. Use your existing authentication middleware

        var httpContext = context.GetHttpContext();

        // Example: Check if user is authenticated (adjust based on your auth system)
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Optionally check for admin role
            // return httpContext.User.IsInRole("Admin");
            return true;
        }

        // For now, deny access in production until authentication is implemented
        // Remove this return false and implement proper auth above
        return false;
    }
}

