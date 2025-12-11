# Using Azure App Configuration in Your Code

## Overview

Your application is now configured to use Azure App Configuration for runtime settings and feature flags. This guide shows you how to use these features in your code.

---

## ✅ What Was Added

### NuGet Packages
```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Microsoft.Extensions.Configuration.AzureAppConfiguration" Version="8.0.0" />
<PackageReference Include="Microsoft.FeatureManagement.AspNetCore" Version="4.0.0" />
```

### Program.cs Changes
1. ✅ Azure App Configuration connection (with Managed Identity)
2. ✅ 5-minute refresh interval (Free tier friendly)
3. ✅ Feature Management services
4. ✅ Configuration refresh middleware
5. ✅ Maintenance mode middleware

### New Files
1. ✅ `Controllers/ConfigController.cs` - Example configuration endpoint
2. ✅ `Middleware/MaintenanceModeMiddleware.cs` - Maintenance mode handler

---

## 📖 How to Use Configuration

### 1. Reading Configuration Values

Inject `IConfiguration` into your controller/service:

```csharp
public class MyController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public MyController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("upload")]
    public IActionResult Upload(IFormFile file)
    {
        // Read setting from App Configuration
        var maxSizeMB = _configuration.GetValue<int>("ApplicationSettings:MaxUploadSizeMB", 100);

        if (file.Length > maxSizeMB * 1024 * 1024)
        {
            return BadRequest($"File too large. Max size: {maxSizeMB}MB");
        }

        // Process upload...
        return Ok();
    }
}
```

### 2. Using Feature Flags

Inject `IFeatureManager` to check feature flags:

```csharp
using Microsoft.FeatureManagement;

public class DashboardController : ControllerBase
{
    private readonly IFeatureManager _featureManager;

    public DashboardController(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        // Check if new dashboard is enabled
        if (await _featureManager.IsEnabledAsync("NewDashboard"))
        {
            return Ok(new { version = "v2", features = "enhanced" });
        }

        return Ok(new { version = "v1", features = "standard" });
    }
}
```

### 3. Using Feature Filters (Attribute-Based)

```csharp
using Microsoft.FeatureManagement.Mvc;

public class BetaController : ControllerBase
{
    // This endpoint only works if "BetaAccess" feature is enabled
    [FeatureGate("BetaAccess")]
    [HttpGet("beta-feature")]
    public IActionResult GetBetaFeature()
    {
        return Ok(new { message = "Welcome to beta features!" });
    }
}
```

If the feature is disabled, the endpoint returns `404 Not Found`.

---

## 🎯 Real-World Examples

### Example 1: File Upload with Dynamic Size Limit

```csharp
[HttpPost("upload")]
public IActionResult UploadFile(IFormFile file)
{
    // This value can be changed in App Configuration without redeploying!
    var maxSizeMB = _configuration.GetValue<int>("ApplicationSettings:MaxUploadSizeMB", 100);
    var maxSizeBytes = maxSizeMB * 1024 * 1024;

    if (file.Length > maxSizeBytes)
    {
        return BadRequest(new
        {
            error = "File too large",
            maxSizeMB = maxSizeMB,
            yourSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2)
        });
    }

    // Process file...
    return Ok();
}
```

**Change the limit at runtime:**
```bash
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "200" \
  --yes
```

### Example 2: Session Timeout

```csharp
// In your authentication/session service
public class SessionService
{
    private readonly IConfiguration _configuration;

    public SessionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TimeSpan GetSessionTimeout()
    {
        var minutes = _configuration.GetValue<int>("ApplicationSettings:SessionTimeoutMinutes", 30);
        return TimeSpan.FromMinutes(minutes);
    }

    public bool IsSessionExpired(DateTime lastActivity)
    {
        return DateTime.UtcNow - lastActivity > GetSessionTimeout();
    }
}
```

### Example 3: Gradual Feature Rollout

```csharp
[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboard()
{
    var useNewDashboard = await _featureManager.IsEnabledAsync("NewDashboard");

    if (useNewDashboard)
    {
        _logger.LogInformation("Serving new dashboard to user");
        return Ok(await _newDashboardService.GetDashboardAsync());
    }

    _logger.LogInformation("Serving old dashboard to user");
    return Ok(await _oldDashboardService.GetDashboardAsync());
}
```

**Gradual rollout:**
```bash
# Day 1: Enable for testing (configure targeting in Portal)
az appconfig feature enable --name $APP_CONFIG --feature "NewDashboard" --yes

# Day 7: Full rollout (remove targeting rules in Portal)
```

### Example 4: Maintenance Mode

The middleware automatically handles this, but you can also check manually:

```csharp
[HttpPost("critical-operation")]
public IActionResult PerformCriticalOperation()
{
    var maintenanceMode = _configuration.GetValue<bool>("ApplicationSettings:MaintenanceMode", false);

    if (maintenanceMode)
    {
        return StatusCode(503, new
        {
            error = "Service unavailable",
            message = "System is under maintenance"
        });
    }

    // Perform operation...
    return Ok();
}
```

**Enable maintenance mode instantly:**
```bash
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaintenanceMode" \
  --value "true" \
  --yes

# Users get 503 in ~5 minutes (or less if they make a new request)
```

---

## 🔧 Configuration Options

### Available Settings (from App Configuration)

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `ApplicationSettings:MaxUploadSizeMB` | int | 100 | Maximum file upload size |
| `ApplicationSettings:SessionTimeoutMinutes` | int | 30 | User session timeout |
| `ApplicationSettings:EnableSwagger` | bool | true | Enable/disable Swagger UI |
| `ApplicationSettings:MaintenanceMode` | bool | false | Put app in maintenance mode |

### Available Feature Flags

| Feature | Default | Purpose |
|---------|---------|---------|
| `NewDashboard` | Disabled | Toggle new dashboard UI |
| `BetaAccess` | Disabled | Enable beta features |

---

## 🧪 Testing Configuration Changes

### 1. Test Locally (Development)

```bash
# Set environment variable
export AppConfigEndpoint="https://aicalendar-config-XXXXX.azconfig.io"

# Run app
dotnet run --project src/AICalendar.API

# App will connect to Azure App Configuration using your Azure CLI credentials
```

### 2. Test in Azure (Production)

```bash
# Get your app URL
APP_URL=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv)

# Check current configuration
curl https://$APP_URL/api/config/current

# Change a setting
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "200" \
  --yes

# Wait 5 minutes (or make a request to trigger refresh)
sleep 300

# Verify change
curl https://$APP_URL/api/config/current
```

---

## 🎨 Advanced Usage

### 1. Strongly-Typed Configuration

Create a settings class:

```csharp
public class ApplicationSettings
{
    public int MaxUploadSizeMB { get; set; } = 100;
    public int SessionTimeoutMinutes { get; set; } = 30;
    public bool EnableSwagger { get; set; } = true;
    public bool MaintenanceMode { get; set; } = false;
}
```

Register it in `Program.cs`:

```csharp
builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection("ApplicationSettings"));
```

Use it in your code:

```csharp
public class MyController : ControllerBase
{
    private readonly IOptionsSnapshot<ApplicationSettings> _settings;

    public MyController(IOptionsSnapshot<ApplicationSettings> settings)
    {
        _settings = settings;
    }

    [HttpPost("upload")]
    public IActionResult Upload(IFormFile file)
    {
        var maxSize = _settings.Value.MaxUploadSizeMB * 1024 * 1024;

        if (file.Length > maxSize)
        {
            return BadRequest($"File too large. Max: {_settings.Value.MaxUploadSizeMB}MB");
        }

        return Ok();
    }
}
```

**Important:** Use `IOptionsSnapshot<T>` (not `IOptions<T>`) to get updated values on each request!

### 2. Feature Flag Filters (Advanced Targeting)

You can configure feature flags to target specific users/groups in the Azure Portal:

```csharp
// In Azure Portal, configure "NewDashboard" feature with targeting:
// - 10% of users
// - Specific user IDs
// - Specific groups

// Your code stays the same:
if (await _featureManager.IsEnabledAsync("NewDashboard"))
{
    // This will automatically respect the targeting rules
}
```

### 3. Custom Refresh Triggers

Instead of time-based refresh, use a sentinel value:

```csharp
// In Program.cs
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options
        .Connect(new Uri(appConfigEndpoint), new ManagedIdentityCredential())
        .ConfigureRefresh(refresh =>
        {
            // Only refresh when "Sentinel" value changes
            refresh.Register("Sentinel", refreshAll: true);
        });
});
```

Then trigger refresh manually:

```bash
# 1. Update your settings
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "200" --yes

# 2. Update sentinel to trigger refresh
az appconfig kv set --name $APP_CONFIG --key "Sentinel" --value "$(date +%s)" --yes
```

---

## 📊 Monitoring Configuration Usage

### Check Current Values

```bash
# Via API
curl https://YOUR_APP_URL/api/config/current

# Via Azure CLI
az appconfig kv list --name $APP_CONFIG --output table
```

### View Configuration History

```bash
# View who changed what
az monitor activity-log list \
  --resource-group aicalendar-rg \
  --query "[?contains(resourceId, 'AppConfiguration')]" \
  --output table
```

---

## 🚨 Troubleshooting

### Configuration Not Updating

**Problem:** Changed value in App Configuration but app still shows old value.

**Solutions:**

1. **Wait 5 minutes:** Configuration refreshes every 5 minutes.

2. **Make a new request:** Refresh happens on incoming requests.

3. **Check managed identity:**
   ```bash
   az role assignment list \
     --scope $(az appconfig show --name $APP_CONFIG --query id -o tsv) \
     --output table
   ```

4. **Check app logs:**
   ```bash
   az containerapp logs show \
     --name aicalendar-api \
     --resource-group aicalendar-rg \
     --tail 100
   ```

### Feature Flag Not Working

**Check if feature exists:**
```bash
az appconfig feature list --name $APP_CONFIG --output table
```

**Verify feature is enabled:**
```bash
az appconfig feature show --name $APP_CONFIG --feature "NewDashboard"
```

---

## 📝 Summary

✅ **Configuration is now dynamic!**
- Change settings without rebuilding
- Toggle features without redeploying
- Enable maintenance mode instantly
- All changes take effect in ~5 minutes

✅ **How to use in code:**
- Inject `IConfiguration` for settings
- Inject `IFeatureManager` for feature flags
- Use `IOptionsSnapshot<T>` for strongly-typed config

✅ **Testing:**
- `/api/config/current` - View current configuration
- `/api/config/health` - Check maintenance mode

---

**Your application is now ready to use Azure App Configuration!** 🎉
