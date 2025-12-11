# Deployment Configuration Updates - Summary

## What Changed

Your Azure deployment configuration has been updated to include **Azure App Configuration** for runtime configuration management alongside the existing **Azure Key Vault** for secrets management.

---

## Files Modified

### 1. `deploy-infrastructure.sh`
**Changes:**
- ✅ Added Azure App Configuration creation (Step 7)
- ✅ Populated initial application settings
- ✅ Created feature flags (NewDashboard, BetaAccess)
- ✅ Set up Key Vault references in App Configuration
- ✅ Granted Container App managed identity access to App Configuration
- ✅ Added `AppConfigEndpoint` environment variable to Container App
- ✅ Updated deployment summary to include App Configuration details

**New Variable:**
```bash
APP_CONFIG_NAME="aicalendar-config-$(openssl rand -hex 3)"
```

---

## New Capabilities

### Before (Key Vault Only)
```
✅ Secrets stored securely
❌ Had to redeploy to change app settings
❌ No feature flags
❌ Settings hardcoded in appsettings.json
```

### After (Key Vault + App Configuration)
```
✅ Secrets stored securely in Key Vault
✅ Change settings at runtime (no rebuild!)
✅ Feature flags for gradual rollouts
✅ Maintenance mode toggle
✅ All changes take effect in ~30 seconds
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                Container App (Your .NET 8 App)           │
│                                                          │
│  Uses Managed Identity to access:                       │
│  ├─ App Configuration (settings, feature flags)         │
│  └─ Key Vault (secrets via App Config references)       │
└─────────────────────────────────────────────────────────┘
                    ↓                    ↓
        ┌───────────────────┐  ┌──────────────────┐
        │ App Configuration │  │   Key Vault      │
        │                   │  │                  │
        │ • Settings        │←─│ • SQL Password   │
        │ • Feature Flags   │  │ • Redis Key      │
        │ • KV References   │  │ • API Keys       │
        └───────────────────┘  └──────────────────┘
```

---

## What Gets Created

### Azure App Configuration
- **Name:** `aicalendar-config-XXXXXX` (random suffix)
- **SKU:** Standard
- **Location:** Same as resource group
- **Access:** Container App managed identity has "App Configuration Data Reader" role

### Initial Configuration Values

#### Application Settings
| Key | Value |
|-----|-------|
| `ApplicationSettings:MaxUploadSizeMB` | `100` |
| `ApplicationSettings:SessionTimeoutMinutes` | `30` |
| `ApplicationSettings:EnableSwagger` | `true` |
| `ApplicationSettings:MaintenanceMode` | `false` |

#### Feature Flags
| Feature | State |
|---------|-------|
| `NewDashboard` | Disabled |
| `BetaAccess` | Disabled |

#### Key Vault References
| Key | Reference |
|-----|-----------|
| `ConnectionStrings:DefaultConnection` | `@Microsoft.KeyVault(SecretUri=https://VAULT.vault.azure.net/secrets/SqlConnectionString)` |

---

## How to Use

### 1. Change a Setting (Runtime - No Rebuild!)

```bash
# Get App Configuration name
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Change max upload size
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "200" \
  --yes

# Takes effect in ~30 seconds!
```

### 2. Enable a Feature Flag

```bash
# Enable new dashboard
az appconfig feature enable \
  --name $APP_CONFIG \
  --feature "NewDashboard" \
  --yes
```

### 3. Emergency Maintenance Mode

```bash
# Put app in maintenance mode
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaintenanceMode" \
  --value "true" \
  --yes

# Users see maintenance page in ~30 seconds
```

---

## Application Code Requirements

Your .NET application needs to be configured to use App Configuration. Here's what you need:

### 1. Install NuGet Packages

```bash
dotnet add package Azure.Identity
dotnet add package Microsoft.Extensions.Configuration.AzureAppConfiguration
dotnet add package Microsoft.FeatureManagement.AspNetCore
```

### 2. Update Program.cs

```csharp
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add Azure App Configuration
builder.Configuration.AddAzureAppConfiguration(options =>
{
    var endpoint = builder.Configuration["AppConfigEndpoint"];

    options
        .Connect(new Uri(endpoint), new ManagedIdentityCredential())
        .ConfigureRefresh(refresh =>
        {
            // Refresh all settings that start with "ApplicationSettings:"
            refresh.Register("ApplicationSettings:*", refreshAll: true)
                   .SetCacheExpiration(TimeSpan.FromSeconds(30));
        })
        .UseFeatureFlags(featureFlagOptions =>
        {
            featureFlagOptions.CacheExpirationInterval = TimeSpan.FromSeconds(30);
        });
});

// Add Feature Management
builder.Services.AddFeatureManagement();

// Add Azure App Configuration middleware
builder.Services.AddAzureAppConfiguration();

var app = builder.Build();

// Use Azure App Configuration middleware (enables refresh)
app.UseAzureAppConfiguration();

app.Run();
```

### 3. Use Configuration in Your Code

```csharp
// Inject IConfiguration
public class FileUploadController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IFeatureManager _featureManager;

    public FileUploadController(
        IConfiguration configuration,
        IFeatureManager featureManager)
    {
        _configuration = configuration;
        _featureManager = featureManager;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        // Read setting (automatically refreshed every 30s)
        var maxSize = _configuration.GetValue<int>("ApplicationSettings:MaxUploadSizeMB");

        if (file.Length > maxSize * 1024 * 1024)
        {
            return BadRequest($"File too large. Max size: {maxSize}MB");
        }

        // Check feature flag
        if (await _featureManager.IsEnabledAsync("NewDashboard"))
        {
            // Use new upload logic
        }
        else
        {
            // Use old upload logic
        }

        return Ok();
    }
}
```

### 4. Maintenance Mode Middleware (Optional)

```csharp
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public MaintenanceModeMiddleware(
        RequestDelegate next,
        IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var maintenanceMode = _configuration
            .GetValue<bool>("ApplicationSettings:MaintenanceMode");

        if (maintenanceMode)
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync(
                "Application is currently under maintenance. Please try again later.");
            return;
        }

        await _next(context);
    }
}

// In Program.cs
app.UseMiddleware<MaintenanceModeMiddleware>();
```

---

## Testing the Setup

### 1. Deploy the Infrastructure

```bash
cd docs/deployment
chmod +x deploy-infrastructure.sh
./deploy-infrastructure.sh
```

### 2. Verify App Configuration

```bash
# Get App Configuration name
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# List all settings
az appconfig kv list --name $APP_CONFIG --output table

# List feature flags
az appconfig feature list --name $APP_CONFIG --output table
```

### 3. Test Runtime Changes

```bash
# Change a setting
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "200" \
  --yes

# Wait 30 seconds
sleep 30

# Verify in your app (if you exposed a config endpoint)
curl https://YOUR_APP_URL/api/config/current
```

---

## Security Considerations

### Access Control

The Container App's managed identity has:
- ✅ **App Configuration Data Reader** role (can read configuration)
- ✅ **Key Vault Secrets Get/List** permissions (can read secrets)
- ❌ **Cannot modify** configuration (read-only)

To grant modification access to a user:
```bash
az role assignment create \
  --assignee user@domain.com \
  --role "App Configuration Data Owner" \
  --scope $(az appconfig show --name $APP_CONFIG --query id -o tsv)
```

### Audit Trail

All configuration changes are logged in Azure Activity Log:
```bash
az monitor activity-log list \
  --resource-group aicalendar-rg \
  --query "[?contains(resourceId, 'AppConfiguration')]" \
  --output table
```

---

## Cost Impact

### Azure App Configuration Pricing (Free Tier)

| Component | Cost |
|-----------|------|
| Base fee | **$0/month** ✅ |
| Requests | 1,000 requests/day (sufficient for dev/test) |
| Storage | 10 MB (plenty for configuration) |

**Monthly cost for this setup:** **$0** 🎉

**Free tier limits:**
- ✅ 1,000 requests per day (your app checks config every 30s = ~2,880 requests/day per instance)
- ✅ 10 MB storage (configuration is typically < 1 MB)
- ✅ Perfect for development, testing, and small production workloads

**Note:** If you need more than 1,000 requests/day (e.g., multiple app instances or very frequent refreshes), you can upgrade to Standard tier (~$36/month) later.

---

## Migration from Existing Setup

If you already have a deployed application:

### Option 1: Fresh Deployment (Recommended)
```bash
# Delete old resources
az group delete --name aicalendar-rg --yes

# Run updated script
./deploy-infrastructure.sh
```

### Option 2: Add to Existing Deployment
```bash
# Create App Configuration
az appconfig create \
  --resource-group aicalendar-rg \
  --name aicalendar-config-XXXXX \
  --location westeurope \
  --sku Standard

# Grant access to existing Container App
PRINCIPAL_ID=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query identity.principalId -o tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "App Configuration Data Reader" \
  --scope $(az appconfig show --name aicalendar-config-XXXXX --query id -o tsv)

# Update Container App with endpoint
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --set-env-vars "AppConfigEndpoint=https://aicalendar-config-XXXXX.azconfig.io"
```

---

## Troubleshooting

### Issue: Configuration not updating

**Check 1:** Verify managed identity has access
```bash
az role assignment list \
  --scope $(az appconfig show --name $APP_CONFIG --query id -o tsv) \
  --output table
```

**Check 2:** Verify app is using App Configuration
```bash
az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query "properties.template.containers[0].env" \
  --output table
```

**Check 3:** Check app logs
```bash
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --tail 100
```

---

## Next Steps

1. ✅ **Deploy Infrastructure:** Run `./deploy-infrastructure.sh`
2. ✅ **Update Application Code:** Add App Configuration NuGet packages and configuration
3. ✅ **Test Runtime Changes:** Modify settings and verify they take effect
4. ✅ **Set Up Monitoring:** Configure alerts for configuration changes
5. ✅ **Document Settings:** Maintain a list of all configuration keys and their purposes

---

## Additional Resources

- **[APP-CONFIG-SETUP.md](./APP-CONFIG-SETUP.md)** - Detailed setup guide
- **[RUNTIME-CONFIG-GUIDE.md](./RUNTIME-CONFIG-GUIDE.md)** - Complete runtime configuration examples
- **[Azure App Configuration Docs](https://learn.microsoft.com/en-us/azure/azure-app-configuration/)** - Official documentation

---

**Summary:** Your deployment now supports runtime configuration changes without rebuilding or redeploying. Change settings in ~30 seconds using Azure CLI, Portal, or REST API!
