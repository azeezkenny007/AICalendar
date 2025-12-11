namespace AICalendar.Infrastructure.ExternalServices.Cache;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public bool Enabled { get; set; } = true;
    public CacheProvider Provider { get; set; } = CacheProvider.Redis;
    public int DefaultExpirationMinutes { get; set; } = 60;
    public RedisOptions Redis { get; set; } = new();
}

public class RedisOptions
{
    // Can be overridden by ConnectionStrings:RedisConnection
    public string? ConnectionString { get; set; }

    public bool AbortOnConnectFail { get; set; } = false;
    public int ConnectRetry { get; set; } = 3;
    public int ConnectTimeoutMs { get; set; } = 5000;
    public int Database { get; set; } = 0;
    public string KeyPrefix { get; set; } = "aicalendar:";
}

public enum CacheProvider
{
    Redis
}
