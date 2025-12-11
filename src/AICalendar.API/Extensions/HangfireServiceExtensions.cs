using AICalendar.Application.Common.Interfaces;
using AICalendar.Infrastructure.BackgroundJobs;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Ensure Hangfire schema exists before initialization
        EnsureHangfireSchemaExists(hangfireSettings.ConnectionString);

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
                PrepareSchemaIfNecessary = true,
                SchemaName = "HangFire"
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

    private static void EnsureHangfireSchemaExists(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Check if HangFire schema exists
            var checkSchemaQuery = @"
                SELECT COUNT(*)
                FROM sys.schemas
                WHERE name = 'HangFire'";

            using var checkCommand = new SqlCommand(checkSchemaQuery, connection);
            var schemaExists = (int)checkCommand.ExecuteScalar() > 0;

            if (!schemaExists)
            {
                Console.WriteLine("HangFire schema does not exist. Creating schema...");

                // Create HangFire schema
                var createSchemaQuery = "CREATE SCHEMA [HangFire]";
                using var createCommand = new SqlCommand(createSchemaQuery, connection);
                createCommand.ExecuteNonQuery();

                Console.WriteLine("✅ HangFire schema created successfully!");
            }
            else
            {
                Console.WriteLine("✅ HangFire schema already exists.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Warning: Could not verify/create HangFire schema: {ex.Message}");
            Console.WriteLine("Hangfire will attempt to create schema automatically with PrepareSchemaIfNecessary=true");
        }
    }
}
