using AICalendar.API.Filters;
using Hangfire;
using Hangfire.Dashboard;

namespace AICalendar.API.Extensions;

public static class HangfireDashboardExtensions
{
    public static IApplicationBuilder UseHangfireDashboardWithAuth(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "AICalendar Job Dashboard",
            DisplayStorageConnectionString = false,
            Authorization = new[] { new HangfireAuthorizationFilter() },
            StatsPollingInterval = 5000, // 5 seconds
            AppPath = "/", // Back to site URL
            IgnoreAntiforgeryToken = true
        });

        return app;
    }
}
