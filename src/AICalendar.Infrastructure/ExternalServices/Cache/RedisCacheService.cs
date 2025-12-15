using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using AICalendar.Application.Common.Interfaces;

namespace AICalendar.Infrastructure.ExternalServices.Cache;


public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IDatabase _db;
    private readonly CacheOptions _options;
    private readonly string _keyPrefix;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;
        _keyPrefix = _options.Redis.KeyPrefix;
        _db = _redis.GetDatabase(_options.Redis.Database);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var value = await _db.StringGetAsync(prefixedKey);
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key {Key} from Redis", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(prefixedKey, json, expiration);
            _logger.LogDebug("Cached key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} in Redis", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            await _db.KeyDeleteAsync(prefixedKey);
            _logger.LogDebug("Removed key: {Key} from cache", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from Redis", key);
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var newValue = await factory();
        if (newValue != null)
        {
            await SetAsync(key, newValue, expiration);
        }

        return newValue;
    }

    public async Task FlushAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushDatabaseAsync(_options.Redis.Database);
            _logger.LogWarning("Flushed all cache entries from Redis database {Database}", _options.Redis.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing Redis cache");
        }
    }

    private string GetPrefixedKey(string key)
    {
        return string.IsNullOrEmpty(_keyPrefix) ? key : $"{_keyPrefix}{key}";
    }
}
