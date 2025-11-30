using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using AICalendar.Application.Common.Interfaces;

namespace AICalendar.Infrastructure.ExternalServices.Cache;

/// <summary>
/// Extension methods for configuring cache services
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Adds cache services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddCacheServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind cache options from configuration
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        // Only register cache if enabled
        if (!cacheOptions.Enabled)
        {
            services.AddScoped<ICacheService, NullCacheService>();
            return services;
        }

        // Register based on provider type
        switch (cacheOptions.Provider)
        {
            case CacheProvider.Redis:
                AddRedisCache(services, configuration, cacheOptions);
                break;

            default:
                throw new InvalidOperationException($"Unsupported cache provider: {cacheOptions.Provider}");
        }

        return services;
    }

    private static void AddRedisCache(
        IServiceCollection services,
        IConfiguration configuration,
        CacheOptions cacheOptions)
    {
        // Get Redis connection string from ConnectionStrings section or Cache options
        var redisConnectionString = configuration.GetConnectionString("RedisConnection")
            ?? cacheOptions.Redis.ConnectionString;

        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new InvalidOperationException(
                "Redis connection string is required when using Redis cache provider. " +
                "Configure it in ConnectionStrings:RedisConnection or Cache:Redis:ConnectionString");
        }

        // Register Redis connection multiplexer
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
            try
            {
                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                configurationOptions.AbortOnConnectFail = cacheOptions.Redis.AbortOnConnectFail;
                configurationOptions.ConnectRetry = cacheOptions.Redis.ConnectRetry;
                configurationOptions.ConnectTimeout = cacheOptions.Redis.ConnectTimeoutMs;
                configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(1000, 10000);
                configurationOptions.DefaultDatabase = cacheOptions.Redis.Database;

                var connection = ConnectionMultiplexer.Connect(configurationOptions);

                connection.ConnectionFailed += (sender, e) =>
                {
                    logger.LogWarning("Redis connection failed: {Exception}", e.Exception?.Message);
                };

                connection.ConnectionRestored += (sender, e) =>
                {
                    logger.LogInformation("Redis connection restored");
                };

                logger.LogInformation("Redis cache initialized successfully on database {Database}",
                    cacheOptions.Redis.Database);
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Redis connection. Error: {Error}", ex.Message);
                throw;
            }
        });

        // Register Redis cache service
        services.AddScoped<ICacheService, RedisCacheService>();
    }
}

/// <summary>
/// Null object pattern implementation for when caching is disabled
/// </summary>
internal class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan expiration) => Task.CompletedTask;

    public Task RemoveAsync(string key) => Task.CompletedTask;

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        // Always execute the factory since we're not caching
        return await factory();
    }
}
