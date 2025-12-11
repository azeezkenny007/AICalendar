# Azure App Configuration Setup Guide

## Overview

Your deployment now includes **Azure App Configuration** for runtime configuration management. This allows you to change application settings, feature flags, and other configurations **without rebuilding or redeploying** your application.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Container App                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Your Application (.NET 8)                        │  │
│  │  - Uses Managed Identity                          │  │
│  │  - Reads from App Configuration every 30s         │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                        ↓ ↓
        ┌───────────────┘ └───────────────┐
        ↓                                   ↓
┌───────────────────┐            ┌──────────────────────┐
│ App Configuration │            │     Key Vault        │
│                   │            │                      │
│ • Settings        │←─Reference─│ • SQL Connection     │
│ • Feature Flags   │            │ • Redis Connection   │
│ • Key References  │            │ • API Keys           │
└───────────────────┘            └──────────────────────┘
```

---

## What's Configured

### 1. Application Settings (in App Configuration)

**Tier:** Free (1,000 requests/day, 10 MB storage, $0/month)

| Key | Default Value | Purpose |
|-----|---------------|---------|
| `ApplicationSettings:MaxUploadSizeMB` | `100` | Maximum file upload size |
| `ApplicationSettings:SessionTimeoutMinutes` | `30` | User session timeout |
| `ApplicationSettings:EnableSwagger` | `true` | Enable/disable Swagger UI |
| `ApplicationSettings:MaintenanceMode` | `false` | Put app in maintenance mode |

### 2. Feature Flags (in App Configuration)

| Feature | Default State | Purpose |
|---------|---------------|---------|
| `NewDashboard` | Disabled | Toggle new dashboard UI |
| `BetaAccess` | Disabled | Enable beta features |

### 3. Secrets (in Key Vault, referenced by App Configuration)

| Secret | Purpose |
|--------|---------|
| `SqlConnectionString` | Database connection |
| `RedisConnectionString` | Redis cache connection |
| `AcrLoginServer` | Container registry URL |
| `AcrUsername` | Container registry username |
| `AcrPassword` | Container registry password |

---

## How It Works

### 1. Configuration Refresh

Your application automatically checks for configuration changes every **30 seconds**:

```csharp
// In your Program.cs or Startup.cs
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options
        .Connect(new Uri(builder.Configuration["AppConfigEndpoint"]),
                 new ManagedIdentityCredential())
        .ConfigureRefresh(refresh =>
        {
            refresh.Register("ApplicationSettings:*", refreshAll: true)
                   .SetCacheExpiration(TimeSpan.FromSeconds(30));
        })
        .UseFeatureFlags(featureFlagOptions =>
        {
            featureFlagOptions.CacheExpirationInterval = TimeSpan.FromSeconds(30);
        });
});
```

### 2. Managed Identity Authentication

The Container App uses its **System-Assigned Managed Identity** to authenticate:
- ✅ No passwords or connection strings needed
- ✅ Automatic credential rotation
- ✅ Secure by default

---

## Common Operations

### Change a Setting (No Rebuild Required!)

```bash
# Get your App Configuration name
APP_CONFIG=$(az appconfig list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# Change max upload size
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "200" \
  --yes

# Change takes effect in ~30 seconds!
```

### Enable a Feature Flag

```bash
# Enable new dashboard
az appconfig feature enable \
  --name $APP_CONFIG \
  --feature "NewDashboard" \
  --yes

# Disable it again
az appconfig feature disable \
  --name $APP_CONFIG \
  --feature "NewDashboard" \
  --yes
```

### Enable Maintenance Mode (Emergency!)

```bash
# Put app in maintenance mode immediately
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaintenanceMode" \
  --value "true" \
  --yes

# Users will see maintenance page in ~30 seconds

# Bring it back online
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaintenanceMode" \
  --value "false" \
  --yes
```

### Update a Secret (Database Password)

```bash
# Get your Key Vault name
KEYVAULT=$(az keyvault list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# Update SQL connection string
az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "SqlConnectionString" \
  --value "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=MyDb;User ID=admin;Password=NEW_PASSWORD;..."

# App automatically picks up new value in ~30 seconds!
```

---

## Verification

### Check Current Configuration

```bash
# List all settings
az appconfig kv list \
  --name $APP_CONFIG \
  --output table

# View specific setting
az appconfig kv show \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB"

# List feature flags
az appconfig feature list \
  --name $APP_CONFIG \
  --output table
```

### Test in Your Application

```bash
# Get your app URL
APP_URL=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv)

# Check if configuration is being read (if you exposed this endpoint)
curl https://$APP_URL/api/config/current
```

---

## Environment-Specific Configuration

You can use **labels** to maintain different configurations for different environments:

```bash
# Development configuration
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "50" \
  --label "dev" \
  --yes

# Production configuration
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "100" \
  --label "prod" \
  --yes

# Your app loads the appropriate label based on environment
```

---

## Security

### Who Can Change Configuration?

By default, only users/identities with the **App Configuration Data Owner** role can modify settings.

```bash
# Grant access to a user
az role assignment create \
  --assignee user@domain.com \
  --role "App Configuration Data Owner" \
  --scope $(az appconfig show --name $APP_CONFIG --query id -o tsv)

# Grant read-only access
az role assignment create \
  --assignee user@domain.com \
  --role "App Configuration Data Reader" \
  --scope $(az appconfig show --name $APP_CONFIG --query id -o tsv)
```

### Audit Trail

All configuration changes are logged:

```bash
# View recent changes
az monitor activity-log list \
  --resource-group aicalendar-rg \
  --query "[?contains(resourceId, 'AppConfiguration')]" \
  --output table
```

---

## Backup and Restore

### Export Configuration

```bash
# Backup current configuration
az appconfig kv export \
  --name $APP_CONFIG \
  --destination file \
  --path "backup-$(date +%Y%m%d).json" \
  --format json
```

### Restore Configuration

```bash
# Restore from backup
az appconfig kv import \
  --name $APP_CONFIG \
  --source file \
  --path "backup-20241208.json" \
  --format json
```

---

## Troubleshooting

### Configuration Not Updating

**Problem:** Changed value but app still shows old value.

**Solutions:**

1. **Verify change was applied:**
   ```bash
   az appconfig kv show --name $APP_CONFIG --key "YourKey"
   ```

2. **Check managed identity has access:**
   ```bash
   az role assignment list \
     --scope $(az appconfig show --name $APP_CONFIG --query id -o tsv) \
     --output table
   ```

3. **Check app logs:**
   ```bash
   az containerapp logs show \
     --name aicalendar-api \
     --resource-group aicalendar-rg \
     --tail 100
   ```

4. **Wait 30 seconds:** Configuration refresh happens every 30 seconds.

---

## Best Practices

### 1. Use Feature Flags for New Features

Instead of hardcoding:
```csharp
// ❌ BAD - requires rebuild
if (true)
{
    UseNewFeature();
}
```

Do this:
```csharp
// ✅ GOOD - change anytime
if (await _featureManager.IsEnabledAsync("NewFeature"))
{
    UseNewFeature();
}
```

### 2. Test Changes First

```bash
# 1. Update setting
az appconfig kv set --name $APP_CONFIG --key "..." --value "..." --yes

# 2. Wait 30 seconds
sleep 30

# 3. Test the change
curl https://$APP_URL/api/test

# 4. If bad, revert immediately
az appconfig kv set --name $APP_CONFIG --key "..." --value "original_value" --yes
```

### 3. Document Changes

Keep a changelog of configuration modifications:
```bash
echo "$(date): Changed MaxUploadSizeMB from 100 to 200 - Reason: Customer request" >> config-changelog.txt
```

### 4. Use Read-Only for Critical Settings

```bash
# Protect critical settings
az appconfig kv set \
  --name $APP_CONFIG \
  --key "CriticalSetting" \
  --value "value" \
  --read-only true \
  --yes
```

---

## Real-World Scenarios

### Scenario 1: Black Friday Traffic Spike

**Problem:** Expecting 10x traffic, need to increase limits.

**Solution:**
```bash
# Increase limits (takes 30 seconds)
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "500" --yes
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:SessionTimeoutMinutes" --value "120" --yes

# After Black Friday, revert
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "100" --yes
```

**Time:** 1 minute to change, no rebuild, no downtime!

### Scenario 2: Critical Bug Found at 2 AM

**Problem:** New feature has bug, need to disable immediately.

**Solution:**
```bash
# Disable feature from your phone!
az appconfig feature disable --name $APP_CONFIG --feature "NewPaymentProcessor" --yes

# Or enable maintenance mode
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaintenanceMode" --value "true" --yes
```

**Time:** 30 seconds from discovery to mitigation!

---

## Summary

✅ **What You Can Change:**
- Application settings (upload limits, timeouts, etc.)
- Feature flags (enable/disable features)
- API endpoints and URLs
- Maintenance mode
- Secrets (via Key Vault references)

✅ **How Fast:**
- Changes take effect in ~30 seconds
- No rebuild required
- No redeployment required
- No container restart required

✅ **How Secure:**
- Managed Identity authentication (no passwords)
- RBAC for access control
- Audit trail for all changes
- Secrets stored in Key Vault

---

**For detailed examples and automation scripts, see [RUNTIME-CONFIG-GUIDE.md](./RUNTIME-CONFIG-GUIDE.md)**
