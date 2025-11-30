# Redis Cache in Development Environment

## Overview

Yes, **Redis cache IS configured and running in your development environment** via Docker Compose. Here's how it all works together:

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Docker Compose Network                    │
│                    (aicalendar-network)                      │
│                                                              │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────┐  │
│  │              │      │              │      │          │  │
│  │  API Service │─────▶│  SQL Server  │      │  Redis   │  │
│  │  (Port 8080) │      │  (Port 1433) │      │ (Port    │  │
│  │              │      │              │      │  6379)   │  │
│  │              │──────┼──────────────┼─────▶│          │  │
│  └──────────────┘      └──────────────┘      └──────────┘  │
│         │                                           │       │
└─────────┼───────────────────────────────────────────┼───────┘
          │                                           │
    Host: localhost:8080                    Host: localhost:6379
```

## Redis Configuration Details

### 1. Docker Compose Setup (docker-compose.yml)

**Lines 98-114** define the Redis service:

```yaml
redis:
  image: redis:7-alpine                    # Official Redis 7 (Alpine Linux)
  container_name: aicalendar-redis
  ports:
    - "6379:6379"                          # Exposed to host machine
  command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD:-devpassword}
  volumes:
    - redis_data:/data                     # Persistent storage
  healthcheck:
    test: [ "CMD", "redis-cli", "--raw", "incr", "ping" ]
    interval: 10s
    timeout: 3s
    retries: 5
    start_period: 10s
  restart: unless-stopped
  networks:
    - aicalendar-network
```

**Key Features:**
- ✅ **Persistent Storage**: Data survives container restarts via `redis_data` volume
- ✅ **Password Protected**: Uses `devpassword` (configurable via environment variable)
- ✅ **Health Checks**: Ensures Redis is ready before API starts
- ✅ **Auto-Restart**: Automatically restarts if it crashes

### 2. API Connection Configuration

**In docker-compose.yml (Line 31):**
```yaml
- ConnectionStrings__RedisConnection=redis:6379,password=${REDIS_PASSWORD:-devpassword},abortConnect=false
```

**In appsettings.json (Line 10):**
```json
"RedisConnection": "localhost:6379,password=devpassword,abortConnect=false"
```

**Important Difference:**
- **Inside Docker**: Uses hostname `redis:6379` (container-to-container communication)
- **Outside Docker**: Uses `localhost:6379` (host machine access)

### 3. Cache Configuration (appsettings.json)

**Lines 12-23:**
```json
"Cache": {
  "Enabled": true,
  "Provider": "Redis",                    // Using Redis provider
  "DefaultExpirationMinutes": 60,
  "Redis": {
    "AbortOnConnectFail": false,          // Don't crash if Redis is down
    "ConnectRetry": 3,                    // Retry 3 times
    "ConnectTimeoutMs": 5000,             // 5 second timeout
    "Database": 0,                        // Use Redis DB 0
    "KeyPrefix": "aicalendar:"            // All keys prefixed
  }
}
```

## How It Works

### When You Run `docker compose up`:

1. **Redis Container Starts**
   - Pulls `redis:7-alpine` image
   - Creates persistent volume `aicalendar_redis_data`
   - Starts Redis server with password `devpassword`
   - Waits for health check to pass

2. **API Container Waits**
   - Defined in `depends_on` (line 59-60)
   - Won't start until Redis health check passes

3. **API Connects to Redis**
   - Uses connection string: `redis:6379,password=devpassword`
   - `CacheServiceExtensions.AddCacheServices()` registers Redis
   - `RedisCacheService` connects to Redis via `IConnectionMultiplexer`

4. **Cache Operations**
   - All cache keys are prefixed: `aicalendar:user:123`
   - Data is serialized to JSON and stored in Redis
   - Cached data persists in the `redis_data` volume

## Verification

### Check if Redis is Running

```bash
# See all running containers
docker ps

# Should show:
# - aicalendar-redis (port 6379)
# - aicalendar-db (port 1433)
# - aicalendar-api (port 8080)
```

### Connect to Redis CLI

```bash
# Connect to Redis container
docker exec -it aicalendar-redis redis-cli

# Authenticate
AUTH devpassword

# List all keys
KEYS aicalendar:*

# Get a specific key
GET aicalendar:user:123

# Monitor all commands in real-time
MONITOR
```

### Test from Your Application

```bash
# Check API logs for Redis connection
docker logs aicalendar-api | grep -i redis

# Should see:
# "Redis cache initialized successfully on database 0"
```

## Development Scenarios

### Scenario 1: Running with Docker Compose (Current Setup)
✅ **Redis is ENABLED and RUNNING**
- Connection: `redis:6379`
- Provider: `Redis`
- Persistent: Yes

### Scenario 2: Running Locally (dotnet run)
If you run the API outside Docker:
```bash
cd src/AICalendar.API
dotnet run
```

The API will try to connect to `localhost:6379`:
- ✅ **Works if Redis container is running**: `docker compose up redis`
- ❌ **Fails if Redis is not running**: Connection error (but app still starts due to `AbortOnConnectFail: false`)

### Scenario 3: Development Without Redis
If you want to develop without Redis, create `appsettings.Development.json`:

```json
{
  "Cache": {
    "Provider": "InMemory"  // Switch to in-memory cache
  }
}
```

## Environment Variables

The `setup.sh` script **automatically generates secure passwords** for both SQL Server and Redis:

```bash
# Run the setup script
./setup.sh

# Output:
# ✅ Created .env file with generated passwords
#    📊 SQL Server password: Strong@xK9mP2vQ7wR81
#    🔐 Redis password: Redis@aB3cD4eF5gH6iJ7k2
#
#    💡 Passwords are saved in .env file (gitignored)
```

The generated `.env` file will contain:
```bash
# .env (auto-generated)
ASPNETCORE_ENVIRONMENT=Development
MSSQL_SA_PASSWORD=Strong@xK9mP2vQ7wR81      # Auto-generated
REDIS_PASSWORD=Redis@aB3cD4eF5gH6iJ7k2      # Auto-generated
```

### Manual Password Configuration (Optional)

If you need to set custom passwords, edit the `.env` file directly:

```bash
# .env
REDIS_PASSWORD=your-custom-secure-password
MSSQL_SA_PASSWORD=your-custom-sql-password
```

Then restart:
```bash
docker compose down
docker compose up
```

## Data Persistence

Redis data is stored in a Docker volume:
```bash
# View volumes
docker volume ls | grep redis

# Inspect volume
docker volume inspect aicalendar_redis_data

# Clear Redis data (WARNING: Deletes all cached data)
docker compose down
docker volume rm aicalendar_redis_data
docker compose up
```

## Monitoring Cache Usage

### Application Logs
Enable debug logging in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "AICalendar.Infrastructure.ExternalServices.Cache": "Debug"
    }
  }
}
```

You'll see:
```
[Debug] Cache hit for key: user:123
[Debug] Cache miss for key: user:456
[Debug] Cached key: user:456 with expiration: 01:00:00
```

### Redis Stats
```bash
# Connect to Redis
docker exec -it aicalendar-redis redis-cli -a devpassword

# Get stats
INFO stats

# Get memory usage
INFO memory

# Get number of keys
DBSIZE
```

## Summary

✅ **YES, Redis is configured and running in development**
- Runs in Docker container `aicalendar-redis`
- Accessible at `localhost:6379` from host
- Accessible at `redis:6379` from API container
- Password: `devpassword`
- Persistent storage via Docker volume
- Health checks ensure it's ready before API starts
- Configured in `appsettings.json` with `Provider: "Redis"`

The cache service is production-ready and fully functional in your development environment! 🚀
