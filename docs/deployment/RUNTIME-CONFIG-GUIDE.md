# Runtime Configuration Management Guide

Complete guide for changing configuration after deployment without rebuilding or redeploying.

---

## 🎯 Quick Reference

| Task | Method | Takes Effect | Requires Restart? |
|------|--------|--------------|-------------------|
| Change max upload size | Update App Config | ~30 seconds | ❌ No |
| Toggle feature flag | Update App Config | ~30 seconds | ❌ No |
| Enable maintenance mode | Update App Config | ~30 seconds | ❌ No |
| Update API endpoint | Update App Config | ~30 seconds | ❌ No |
| Change database password | Update Key Vault | ~30 seconds | ❌ No |
| Update API key | Update Key Vault | ~30 seconds | ❌ No |

---

## 📋 Common Operations

### 1. Change Application Settings

#### Via Azure Portal (GUI):
1. Go to Azure Portal → **App Configuration**
2. Select your App Configuration store
3. Click **Configuration explorer**
4. Find the key (e.g., `ApplicationSettings:MaxUploadSizeMB`)
5. Click **Edit**
6. Change value (e.g., from `100` to `200`)
7. Click **Apply**
8. **Done!** Change takes effect in ~30 seconds

#### Via Azure CLI:
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

# Change session timeout
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:SessionTimeoutMinutes" \
  --value "60" \
  --yes

# Enable/disable Swagger
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:EnableSwagger" \
  --value "true" \
  --yes
```

#### Via REST API (for automation):
```bash
# Get access token
ACCESS_TOKEN=$(az account get-access-token --resource https://azconfig.io --query accessToken -o tsv)

# Update setting
curl -X PUT "https://$APP_CONFIG.azconfig.io/kv/ApplicationSettings:MaxUploadSizeMB?api-version=1.0" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"value": "200"}'
```

---

### 2. Toggle Feature Flags

#### Enable a feature:
```bash
APP_CONFIG=$(az appconfig list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# Enable new dashboard
az appconfig feature enable \
  --name $APP_CONFIG \
  --feature "NewDashboard" \
  --yes

# Enable beta access
az appconfig feature enable \
  --name $APP_CONFIG \
  --feature "BetaAccess" \
  --yes
```

#### Disable a feature:
```bash
# Disable new dashboard
az appconfig feature disable \
  --name $APP_CONFIG \
  --feature "NewDashboard" \
  --yes
```

#### List all feature flags:
```bash
az appconfig feature list \
  --name $APP_CONFIG \
  --output table
```

---

### 3. Enable Maintenance Mode

**Emergency! Need to take app offline immediately:**

```bash
APP_CONFIG=$(az appconfig list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# Enable maintenance mode
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaintenanceMode" \
  --value "true" \
  --yes

# Your app will show maintenance page in ~30 seconds!
```

**Back online:**

```bash
# Disable maintenance mode
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaintenanceMode" \
  --value "false" \
  --yes
```

---

### 4. Update Secrets (Database, API Keys, etc.)

Secrets are stored in Key Vault but referenced by App Configuration.

```bash
KEYVAULT=$(az keyvault list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# Update database password
# (You'll need to update SQL server first, then this)
az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "SqlConnectionString" \
  --value "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=MyDb;User ID=admin;Password=NEW_PASSWORD_HERE;..."

# Update SendGrid API key
az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "SendGridApiKey" \
  --value "SG.NEW_API_KEY_HERE"

# Update Stripe API key
az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "StripeSecretKey" \
  --value "sk_live_NEW_KEY_HERE"
```

**Your app automatically gets the new values in ~30 seconds!**

---

### 5. Update External API Configuration

```bash
APP_CONFIG=$(az appconfig list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# Change API base URL
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ExternalApi:BaseUrl" \
  --value "https://api.newprovider.com" \
  --yes

# Change timeout
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ExternalApi:TimeoutSeconds" \
  --value "60" \
  --yes
```

---

### 6. View All Current Configuration

```bash
APP_CONFIG=$(az appconfig list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# List all settings
az appconfig kv list \
  --name $APP_CONFIG \
  --output table

# Export to JSON
az appconfig kv list \
  --name $APP_CONFIG \
  --output json > current-config.json

# View specific setting
az appconfig kv show \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB"
```

---

### 7. Test Configuration in Your App

```bash
# Get your app URL
APP_URL=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv)

# Check current config (if you exposed this endpoint)
curl https://$APP_URL/api/fileupload/config

# Example response:
# {
#   "maxUploadSizeMB": 200,
#   "sessionTimeoutMinutes": 60,
#   "maintenanceMode": false,
#   "enableSwagger": true
# }
```

---

## 🎬 Real-World Scenarios

### Scenario 1: Black Friday - Temporarily Increase Limits

**Problem:** Expecting 10x traffic, need to increase upload limits and session timeouts.

**Solution:**
```bash
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Increase limits
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "500" --yes
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:SessionTimeoutMinutes" --value "120" --yes

# After Black Friday, revert
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "100" --yes
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:SessionTimeoutMinutes" --value "30" --yes
```

**Time:** 1 minute to change, 30 seconds to take effect
**No rebuild, no redeploy!**

---

### Scenario 2: Security Breach - Rotate All API Keys

**Problem:** Third-party service was compromised, need to rotate API key immediately.

**Solution:**
```bash
KEYVAULT=$(az keyvault list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Update compromised API key
az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "ThirdPartyApiKey" \
  --value "NEW_SECURE_KEY_HERE"

# App automatically picks up new key in ~30 seconds
```

**Time:** 30 seconds
**No rebuild, no redeploy, no downtime!**

---

### Scenario 3: Gradual Feature Rollout (A/B Testing)

**Problem:** Testing new dashboard with 10% of users, then gradually increase.

**Solution:**
```bash
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Day 1: Enable for internal testing
az appconfig feature enable --name $APP_CONFIG --feature "NewDashboard" --yes
# (Add targeting rules in Azure Portal for specific users)

# Day 3: Looks good, expand to 25% of users
# (Update targeting rules in Azure Portal)

# Day 5: Expand to 50%
# (Update targeting rules)

# Day 7: Full rollout
# (Remove targeting rules, enable for everyone)
```

---

### Scenario 4: Emergency - Critical Bug Found

**Problem:** New feature has critical bug, need to disable immediately at 2 AM.

**Solution:**
```bash
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Disable problematic feature instantly
az appconfig feature disable \
  --name $APP_CONFIG \
  --feature "NewPaymentProcessor" \
  --yes

# Or enable maintenance mode for entire app
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaintenanceMode" \
  --value "true" \
  --yes
```

**Time:** 30 seconds from discovery to mitigation
**Can be done from your phone!**

---

## 🔄 Configuration Versioning & Rollback

### Label Configuration for Different Environments

```bash
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Create labeled configuration for staging
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "50" \
  --label "staging" \
  --yes

# Create labeled configuration for production
az appconfig kv set \
  --name $APP_CONFIG \
  --key "ApplicationSettings:MaxUploadSizeMB" \
  --value "100" \
  --label "production" \
  --yes
```

### Snapshot Configuration (Backup)

```bash
# Export current configuration
az appconfig kv export \
  --name $APP_CONFIG \
  --destination file \
  --path "backup-$(date +%Y%m%d).json" \
  --format json

# Restore from backup if needed
az appconfig kv import \
  --name $APP_CONFIG \
  --source file \
  --path "backup-20241208.json" \
  --format json
```

---

## 🎯 Best Practices

### 1. **Always Test Changes First**

```bash
# Update setting
az appconfig kv set --name $APP_CONFIG --key "..." --value "..." --yes

# Wait 30 seconds
sleep 30

# Test the change
curl https://$APP_URL/api/test-endpoint

# If good, keep it. If bad, revert immediately.
```

### 2. **Use Feature Flags for New Features**

Instead of:
```csharp
// BAD - requires rebuild to change
if (true) // hardcoded
{
    UseNewFeature();
}
```

Do this:
```csharp
// GOOD - change anytime via App Configuration
if (await _featureManager.IsEnabledAsync("NewFeature"))
{
    UseNewFeature();
}
```

### 3. **Document Changes**

Keep a log of configuration changes:
```bash
# Create change log
cat >> config-changelog.md <<EOF
## $(date)
- Changed MaxUploadSizeMB: 100 → 200
- Reason: Customer request for larger file support
- Changed by: John Doe
EOF
```

### 4. **Monitor Configuration Changes**

Set up alerts in Azure Monitor:
```bash
# Create alert rule for configuration changes
az monitor metrics alert create \
  --name "config-change-alert" \
  --resource-group aicalendar-rg \
  --scopes $APP_CONFIG_ID \
  --condition "count ModificationEvent > 0" \
  --description "Alert when configuration is modified"
```

---

## 🛡️ Security Considerations

### 1. **Who Can Change Configuration?**

```bash
# Grant access to specific user
az role assignment create \
  --assignee user@domain.com \
  --role "App Configuration Data Owner" \
  --scope $APP_CONFIG_ID

# Grant read-only access
az role assignment create \
  --assignee user@domain.com \
  --role "App Configuration Data Reader" \
  --scope $APP_CONFIG_ID
```

### 2. **Audit Trail**

```bash
# View who changed what
az monitor activity-log list \
  --resource-group aicalendar-rg \
  --query "[?contains(resourceId, 'AppConfiguration')]" \
  --output table
```

### 3. **Protect Sensitive Keys**

```bash
# Mark keys as read-only
az appconfig kv set \
  --name $APP_CONFIG \
  --key "CriticalSetting" \
  --value "value" \
  --read-only true \
  --yes

# Must unlock before changing
az appconfig kv set \
  --name $APP_CONFIG \
  --key "CriticalSetting" \
  --read-only false \
  --yes
```

---

## 🚀 Automation Examples

### PowerShell Script: Batch Update

```powershell
# Update multiple settings at once
$AppConfig = (az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

$settings = @{
    "ApplicationSettings:MaxUploadSizeMB" = "200"
    "ApplicationSettings:SessionTimeoutMinutes" = "60"
    "Email:FromEmail" = "noreply@newdomain.com"
}

foreach ($key in $settings.Keys) {
    az appconfig kv set `
        --name $AppConfig `
        --key $key `
        --value $settings[$key] `
        --yes
    Write-Host "Updated $key"
}
```

### Python Script: Dynamic Configuration

```python
#!/usr/bin/env python3
from azure.identity import DefaultAzureCredential
from azure.appconfiguration import AzureAppConfigurationClient
import os

# Connect to App Configuration
endpoint = os.environ["AppConfigEndpoint"]
credential = DefaultAzureCredential()
client = AzureAppConfigurationClient(endpoint, credential)

# Update setting
client.set_configuration_setting(
    key="ApplicationSettings:MaxUploadSizeMB",
    value="200"
)

print("Configuration updated!")
```

---

## 📊 Monitoring Configuration Changes

### View Recent Changes

```bash
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Get recent configuration changes
az monitor activity-log list \
  --resource-group aicalendar-rg \
  --offset 24h \
  --query "[?contains(resourceId, '$APP_CONFIG')].[eventTimestamp, operationName, caller]" \
  --output table
```

### Check Current Refresh Status

```bash
# Check if app is receiving updates
curl https://$APP_URL/api/fileupload/config

# Should show current values from App Configuration
```

---

## 🆘 Troubleshooting

### Configuration Not Updating

**Problem:** Changed value in App Configuration but app still shows old value.

**Solution:**
```bash
# 1. Verify change was applied
az appconfig kv show \
  --name $APP_CONFIG \
  --key "YourKey"

# 2. Check app configuration endpoint is set
az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query "properties.configuration.secrets"

# 3. Check managed identity has access
az role assignment list \
  --scope $APP_CONFIG_ID \
  --output table

# 4. Check app logs
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --tail 100
```

---

## 💡 Pro Tips

1. **Use Labels for Environments:**
   ```bash
   # Dev environment
   --label "dev"

   # Prod environment
   --label "prod"
   ```

2. **Set up Configuration Change Notifications:**
   - Use Azure Event Grid
   - Trigger webhooks on changes
   - Send notifications to Teams/Slack

3. **Create Configuration Profiles:**
   - Export configs for different scenarios
   - Quick restore for known good states

4. **Test Configuration Locally:**
   ```bash
   # Set environment variable
   export AppConfigEndpoint="https://your-appconfig.azconfig.io"

   # Run app locally - it will use Azure App Configuration!
   dotnet run
   ```

---

## 📝 Summary

**What You Can Change at Runtime:**
- ✅ All application settings (upload limits, timeouts, etc.)
- ✅ Feature flags (enable/disable features instantly)
- ✅ API endpoints and URLs
- ✅ Email settings
- ✅ Maintenance mode
- ✅ Secrets (database passwords, API keys)

**What Takes Effect Immediately:**
- Changes propagate in ~30 seconds
- No rebuild required
- No redeployment required
- No container restart required (with IOptionsSnapshot)

**How to Make Changes:**
1. Azure Portal (easiest for one-off changes)
2. Azure CLI (best for scripting)
3. REST API (best for automation)
4. SDKs (Python, .NET, Java, Node.js)

---

**The power is now in your hands - change configuration anytime, anywhere, without touching code!**
