# Cache Service Configuration

This directory contains the cache service implementation for AICalendar.

## Features


- **Configuration-Based**: Easy switching between providers via appsettings.json
- **Key Prefixing**: Automatic key prefixing to avoid collisions
- **Resilient**: Graceful fallback when cache is unavailable
- **Logging**: Debug logging for cache hits/misses

## Configuration

### appsettings.json

```json
{
  "Cache": {
    "Enabled": true,
    "Provider": "Redis",
    "DefaultExpirationMinutes": 60,
    "Redis": {
      "AbortOnConnectFail": false,
      "ConnectRetry": 3,
      "ConnectTimeoutMs": 5000,
      "Database": 0,
      "KeyPrefix": "aicalendar:"
    }
  },
  "ConnectionStrings": {
    "RedisConnection": "localhost:6379,password=devpassword,abortConnect=false"
  }
}
```

### Cache Providers

#### Redis (Production)
- Distributed cache for multi-instance deployments
- Requires Redis server
- Configure via `ConnectionStrings:RedisConnection`

#### InMemory (Development)
- Fast, local cache
- No external dependencies
- Not shared across instances
- Recommended for local development

#### Null (Disabled)
- Cache is disabled
- All operations are no-ops
- Useful for debugging

## Usage

### Basic Usage

```csharp
public class MyService
{
    private readonly ICacheService _cache;

    public MyService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<MyData> GetDataAsync(string id)
    {
        var cacheKey = $"mydata:{id}";

        // Try to get from cache
        var cached = await _cache.GetAsync<MyData>(cacheKey);
        if (cached != null)
            return cached;

        // Fetch from database
        var data = await FetchFromDatabaseAsync(id);

        // Cache for 1 hour
        await _cache.SetAsync(cacheKey, data, TimeSpan.FromHours(1));

        return data;
    }
}
```

### GetOrSet Pattern

```csharp
public async Task<MyData> GetDataAsync(string id)
{
    var cacheKey = $"mydata:{id}";

    return await _cache.GetOrSetAsync(
        cacheKey,
        async () => await FetchFromDatabaseAsync(id),
        TimeSpan.FromHours(1)
    );
}
```

### Cache Invalidation

```csharp
public async Task UpdateDataAsync(string id, MyData data)
{
    await UpdateDatabaseAsync(id, data);

    // Invalidate cache
    await _cache.RemoveAsync($"mydata:{id}");
}
```

## Files

- **CacheOptions.cs**: Configuration model
- **CacheServiceExtensions.cs**: DI registration extensions
- **RedisCacheService.cs**: Redis implementation
- **InMemoryCacheService.cs**: In-memory implementation
- **README.md**: This file

## Environment-Specific Configuration

### Development
Use in-memory cache for simplicity:
```json
{
  "Cache": {
    "Provider": "InMemory"
  }
}
```

### Production
Use Redis for distributed caching:
```json
{
  "Cache": {
    "Provider": "Redis"
  },
  "ConnectionStrings": {
    "RedisConnection": "your-redis-host:6379,password=yourpassword"
  }
}
```

## Best Practices

1. **Use meaningful cache keys**: Include entity type and ID
2. **Set appropriate expiration**: Balance freshness vs. performance
3. **Handle cache misses gracefully**: Always have a fallback
4. **Invalidate on updates**: Remove stale data when entities change
5. **Use key prefixes**: Avoid collisions with other applications
