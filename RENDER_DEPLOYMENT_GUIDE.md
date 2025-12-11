# AICalendar - Render Deployment Guide

## Problem Analysis

Your application is crashing on Render with the error:
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server.
```

**Root Cause**: Your application requires SQL Server, but Render doesn't provide managed SQL Server databases. The app is trying to connect to a SQL Server instance that doesn't exist in the deployment environment.

---

## Architecture Overview

Your AICalendar application has the following dependencies:

| Component | Purpose | Required |
|-----------|---------|----------|
| **SQL Server** | Main database (EF Core + Hangfire) | ✅ Yes |
| **Redis** | Caching layer | ⚠️ Optional* |
| **Firebase** | Push notifications | ⚠️ Optional* |
| **AI Service** | Prediction generation | ⚠️ Optional* |

*Can be disabled in configuration, but core functionality requires SQL Server

---

## Deployment Options

### Option 1: Deploy to Azure (Recommended)

Azure provides native SQL Server support and is the best fit for your .NET application.

#### Prerequisites
- Azure account
- Azure CLI installed

#### Steps

**1. Create Azure Resources**

```bash
# Login to Azure
az login

# Create resource group
az group create --name aicalendar-rg --location eastus

# Create SQL Server
az sql server create \
  --name aicalendar-sql \
  --resource-group aicalendar-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password 'YourStrong@Password123'

# Create database
az sql db create \
  --resource-group aicalendar-rg \
  --server aicalendar-sql \
  --name AICalendarDb \
  --service-objective S0

# Configure firewall (allow Azure services)
az sql server firewall-rule create \
  --resource-group aicalendar-rg \
  --server aicalendar-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create Redis Cache (optional)
az redis create \
  --name aicalendar-redis \
  --resource-group aicalendar-rg \
  --location eastus \
  --sku Basic \
  --vm-size c0

# Create Container App Environment
az containerapp env create \
  --name aicalendar-env \
  --resource-group aicalendar-rg \
  --location eastus
```

**2. Get Connection Strings**

```bash
# SQL Server connection string
az sql server show \
  --name aicalendar-sql \
  --resource-group aicalendar-rg \
  --query fullyQualifiedDomainName -o tsv

# Redis connection string (if created)
az redis show \
  --name aicalendar-redis \
  --resource-group aicalendar-rg \
  --query hostName -o tsv

az redis list-keys \
  --name aicalendar-redis \
  --resource-group aicalendar-rg
```

**3. Configure Environment Variables**

Create a file `azure-env.txt` with your environment variables:

```bash
ConnectionStrings__DefaultConnection=Server=tcp:aicalendar-sql.database.windows.net,1433;Initial Catalog=AICalendarDb;Persist Security Info=False;User ID=sqladmin;Password=YourStrong@Password123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
ConnectionStrings__RedisConnection=aicalendar-redis.redis.cache.windows.net:6380,password=YOUR_REDIS_KEY,ssl=True,abortConnect=False
ASPNETCORE_ENVIRONMENT=Production
Cache__Enabled=true
Cache__Provider=Redis
```

**4. Deploy Container to Azure**

```bash
# Build and push Docker image
docker build -t aicalendar:latest .
docker tag aicalendar:latest <your-acr-registry>.azurecr.io/aicalendar:latest
docker push <your-acr-registry>.azurecr.io/aicalendar:latest

# Or use Container Apps direct deployment
az containerapp create \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --environment aicalendar-env \
  --image <your-acr-registry>.azurecr.io/aicalendar:latest \
  --target-port 8080 \
  --ingress external \
  --env-vars-file azure-env.txt \
  --min-replicas 1 \
  --max-replicas 3
```

**5. Add Firebase Credentials (Optional)**

```bash
# Upload firebase-credentials.json as a secret
az containerapp secret set \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --secrets firebase-creds=@firebase-credentials.json

# Mount as volume
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --secret-volume-mount /etc/secrets
```

---

### Option 2: Render with External SQL Server

Since Render doesn't provide SQL Server, you need to use an external database provider.

#### Prerequisites
- External SQL Server (Azure SQL, AWS RDS, or other provider)
- Render account

#### Steps

**1. Set Up External SQL Server**

Choose one of these providers:
- **Azure SQL Database**: Follow Option 1 steps 1-2 for SQL Server only
- **AWS RDS for SQL Server**: Use AWS Console or CLI
- **Clever Cloud SQL Server**: https://www.clever-cloud.com/
- **ElephantSQL** (PostgreSQL alternative - requires code changes)

**2. Update Your Code for Optional SQL Server**

To prevent the app from crashing immediately, modify [Program.cs](src/AICalendar.API/Program.cs):

```csharp
// Around line 213 - Make database migration optional
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("localhost"))
    {
        logger.LogWarning("⚠️  No production database configured. Skipping migrations.");
    }
    else
    {
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            logger.LogInformation("🔄 Applying EF Core migrations...");

            context.Database.Migrate();
            logger.LogInformation("✅ Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during database migration!");
            if (app.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }
}

// Around line 268 - Make Hangfire configuration optional
using (var scope = app.Services.CreateScope())
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("localhost"))
    {
        try
        {
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<Hangfire.IRecurringJobManager>();
            AICalendar.Infrastructure.BackgroundJobs.HangfireConfiguration.ConfigureRecurringJobs(configuration, recurringJobManager);
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("✅ Hangfire recurring jobs configured successfully");
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ Failed to configure Hangfire recurring jobs");
        }
    }
}
```

**3. Configure Render**

Create a `render.yaml` file in your project root:

```yaml
services:
  - type: web
    name: aicalendar-api
    env: docker
    dockerfilePath: ./Dockerfile
    healthCheckPath: /health
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ConnectionStrings__DefaultConnection
        sync: false # Set in Render dashboard
      - key: ConnectionStrings__RedisConnection
        value: localhost:6379,abortConnect=false
      - key: Cache__Enabled
        value: false # Disable Redis if not available
      - key: Cache__Provider
        value: Null
      - key: ASPNETCORE_URLS
        value: http://+:8080
    plan: standard # Choose appropriate plan
```

**4. Set Environment Variables in Render Dashboard**

1. Go to your Render service
2. Navigate to **Environment** tab
3. Add these variables:

```bash
# Required
ConnectionStrings__DefaultConnection=Server=tcp:your-sql-server.database.windows.net,1433;Initial Catalog=AICalendarDb;Persist Security Info=False;User ID=sqladmin;Password=YourPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;
ASPNETCORE_ENVIRONMENT=Production

# Optional (disable if not available)
Cache__Enabled=false
Cache__Provider=Null

# If you have Redis
ConnectionStrings__RedisConnection=your-redis-host:6379,password=your-password,ssl=True,abortConnect=False
Cache__Enabled=true
Cache__Provider=Redis
```

**5. Deploy to Render**

```bash
# Commit your changes
git add .
git commit -m "Configure for Render deployment with external SQL Server"
git push origin main

# Render will automatically deploy when you push
```

---

### Option 3: Switch to PostgreSQL (Major Refactor)

If you want to stay on Render with a managed database, you'll need to migrate from SQL Server to PostgreSQL.

#### Steps

**1. Install PostgreSQL EF Core Provider**

```bash
cd src/AICalendar.Infrastructure
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**2. Update `ApplicationDbContext` Configuration**

In [Program.cs](src/AICalendar.API/Program.cs) around line 101-113:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Change from UseSqlServer to UseNpgsql
    options.UseNpgsql(connectionString);

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    var outboxInterceptor = serviceProvider.GetRequiredService<AICalendar.Infrastructure.Persistence.Interceptors.OutboxInterceptor>();
    options.AddInterceptors(outboxInterceptor);
});
```

**3. Update Hangfire Storage**

In [HangfireServiceExtensions.cs](src/AICalendar.API/Extensions/HangfireServiceExtensions.cs):

```bash
# Remove SQL Server Hangfire
cd src/AICalendar.Infrastructure
dotnet remove package Hangfire.SqlServer
dotnet add package Hangfire.PostgreSql
```

Update the configuration:

```csharp
using Hangfire.PostgreSql;

// Replace UseSqlServerStorage with UsePostgreSqlStorage
.UsePostgreSqlStorage(hangfireSettings.ConnectionString, new PostgreSqlStorageOptions
{
    PrepareSchemaIfNecessary = true
})
```

**4. Recreate Migrations**

```bash
# Delete existing migrations
rm -rf src/AICalendar.Infrastructure/Persistence/Migrations/*

# Create new migrations for PostgreSQL
dotnet ef migrations add InitialCreate --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API

# Test locally with PostgreSQL
docker run --name postgres -e POSTGRES_PASSWORD=yourpassword -p 5432:5432 -d postgres
```

**5. Configure Render with PostgreSQL**

Update `render.yaml`:

```yaml
services:
  - type: web
    name: aicalendar-api
    env: docker
    dockerfilePath: ./Dockerfile
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ConnectionStrings__DefaultConnection
        fromDatabase:
          name: aicalendar-db
          property: connectionString
      - key: Cache__Enabled
        value: false

databases:
  - name: aicalendar-db
    plan: starter # Choose appropriate plan
    databaseName: aicalendar
    user: aicalendar_user
```

**6. Deploy**

```bash
git add .
git commit -m "Migrate to PostgreSQL for Render deployment"
git push origin main
```

---

## Configuration Reference

### Required Environment Variables

| Variable | Example | Required |
|----------|---------|----------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | ✅ Yes |
| `ASPNETCORE_ENVIRONMENT` | Production | ✅ Yes |
| `ASPNETCORE_URLS` | http://+:8080 | ✅ Yes |

### Optional Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__RedisConnection` | - | Redis connection string |
| `Cache__Enabled` | false | Enable/disable caching |
| `Cache__Provider` | Null | Redis or Null |
| `AIService__Url` | - | AI prediction service URL |
| `AIService__Key` | - | AI service API key |

### Firebase Configuration

Place `firebase-credentials.json` in one of these locations:
- **Local**: Project root directory
- **Docker/Render**: `/etc/secrets/firebase-credentials.json`
- **Azure Container Apps**: Mount as secret volume

---

## Health Check Endpoint

Your app should expose a health check endpoint for monitoring. Add this controller if it doesn't exist:

**File**: `src/AICalendar.API/Controllers/HealthController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AICalendar.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("ready")]
    public IActionResult Ready()
    {
        // Check if critical services are available
        return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }
}
```

---

## Troubleshooting

### Issue: "Could not open a connection to SQL Server"

**Solution**:
1. Verify your SQL Server connection string is correct
2. Check firewall rules allow connections from Render IP addresses
3. For Azure SQL: Enable "Allow Azure services and resources to access this server"

### Issue: "Hangfire schema migration failed"

**Solution**:
1. Make Hangfire initialization optional (see Option 2, Step 2)
2. Ensure `PrepareSchemaIfNecessary = true` in Hangfire configuration
3. Manually create Hangfire schema using SQL scripts

### Issue: "Firebase credentials not found"

**Solution**:
1. Upload `firebase-credentials.json` to `/etc/secrets/` in Render
2. Or use Render's secret files feature
3. Or disable notifications by removing Firebase initialization

### Issue: "Redis connection failed"

**Solution**:
1. Set `Cache__Enabled=false` to disable Redis
2. Set `Cache__Provider=Null` to use in-memory caching
3. Or provision Redis from Azure/AWS/Upstash

---

## Cost Estimates

### Azure (Recommended)

| Service | Plan | Monthly Cost |
|---------|------|--------------|
| Azure SQL Database | Basic (2GB) | ~$5 |
| Azure Container Apps | Consumption | $0-10 (pay-per-use) |
| Azure Redis Cache | Basic C0 | ~$17 |
| **Total** | - | **~$22-32/month** |

### Render + Azure SQL

| Service | Plan | Monthly Cost |
|---------|------|--------------|
| Render Web Service | Starter | $7 |
| Azure SQL Database | Basic (2GB) | ~$5 |
| **Total** | - | **~$12/month** |

### Render + PostgreSQL (All-in-One)

| Service | Plan | Monthly Cost |
|---------|------|--------------|
| Render Web Service | Starter | $7 |
| Render PostgreSQL | Starter | $7 |
| **Total** | - | **~$14/month** |

---

## Recommended Approach

**For fastest deployment**: Choose **Option 2** (Render + Azure SQL)
- Less code changes required
- Azure SQL Basic tier is cheap (~$5/month)
- Keep your existing SQL Server setup

**For staying on Render only**: Choose **Option 3** (PostgreSQL migration)
- Native Render database support
- Slightly cheaper ($14 vs $12/month)
- Requires refactoring and testing

**For production-grade deployment**: Choose **Option 1** (Full Azure)
- Best performance and integration
- Native .NET support
- Enterprise-grade monitoring and scaling

---

## Next Steps

1. **Choose your deployment option** based on budget and requirements
2. **Set up external database** (Azure SQL or PostgreSQL)
3. **Update connection strings** in environment variables
4. **Test locally** with production-like configuration
5. **Deploy to chosen platform**
6. **Monitor application logs** for any startup issues
7. **Run database migrations** manually if auto-migration fails
8. **Configure CI/CD pipeline** for automated deployments

---

## Support

For issues specific to:
- **Azure**: https://docs.microsoft.com/azure
- **Render**: https://render.com/docs
- **Entity Framework**: https://docs.microsoft.com/ef/core
- **Hangfire**: https://docs.hangfire.io

---

**Last Updated**: December 2025
