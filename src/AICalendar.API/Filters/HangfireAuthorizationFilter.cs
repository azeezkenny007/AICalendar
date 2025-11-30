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
        // For this demo project, we allow access in all environments including Production
        // In a real production app, you would uncomment the authentication logic below
        return true;

        /*
        var httpContext = context.GetHttpContext();

        // Example: Check if user is authenticated (adjust based on your auth system)
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Optionally check for admin role
            // return httpContext.User.IsInRole("Admin");
            return true;
        }

        return false;
        */
    }
}

