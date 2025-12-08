using AICalendar.API.Filters;
using AICalendar.Application.Feedback.AI.Jobs;
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

        RecurringJob.AddOrUpdate<SendFeedbackToAIJob>(
            "send-feedback-to-ai",
            job => job.Execute(CancellationToken.None),
            "0 2 * * *" // 2am everyday

        );

        return app;
    }
}

