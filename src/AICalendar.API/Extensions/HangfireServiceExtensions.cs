using AICalendar.Application.Common.Interfaces;
using AICalendar.Infrastructure.BackgroundJobs;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AICalendar.API.Extensions;

public static class HangfireServiceExtensions
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireSettings = configuration.GetSection("Hangfire").Get<HangfireSettings>();

        // Fallback if settings are not present in "Hangfire" section, try to use DefaultConnection
        if (string.IsNullOrEmpty(hangfireSettings?.ConnectionString))
        {
            hangfireSettings = new HangfireSettings
            {
                ConnectionString = configuration.GetConnectionString("DefaultConnection"),
                WorkerCount = configuration.GetValue<int>("Hangfire:WorkerCount", 20),
                RetryAttempts = configuration.GetValue<int>("Hangfire:RetryAttempts", 3)
            };
        }

        // Register your filter first
        services.AddSingleton<LogJobFilter>();

        // Use the overload that provides IServiceProvider
        services.AddHangfire((provider, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(hangfireSettings.ConnectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                PrepareSchemaIfNecessary = true
            })
            .UseFilter(new AutomaticRetryAttribute { Attempts = hangfireSettings.RetryAttempts })
            .UseFilter(provider.GetRequiredService<LogJobFilter>())); // Resolve from DI

        // Add Hangfire background processing
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireSettings.WorkerCount;
            options.Queues = new[] { "default", "critical", "low" };
            options.ServerName = $"AICalendar-{Environment.MachineName}";
        });

        services.AddScoped<IHangfireService, HangfireService>();

        return services;
    }
}
