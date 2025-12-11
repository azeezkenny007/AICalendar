# Quick Reference: Runtime Configuration

## 🚀 Common Commands

### Get Resource Names
```bash
# Get App Configuration name
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Get Key Vault name
KEYVAULT=$(az keyvault list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# Get Container App URL
APP_URL=$(az containerapp show --name aicalendar-api --resource-group aicalendar-rg --query properties.configuration.ingress.fqdn -o tsv)
```

---

## ⚙️ Change Settings (No Rebuild!)

```bash
# Change max upload size
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "200" --yes

# Change session timeout
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:SessionTimeoutMinutes" --value "60" --yes

# Enable/disable Swagger
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:EnableSwagger" --value "false" --yes
```

---

## 🎚️ Feature Flags

```bash
# Enable feature
az appconfig feature enable --name $APP_CONFIG --feature "NewDashboard" --yes

# Disable feature
az appconfig feature disable --name $APP_CONFIG --feature "NewDashboard" --yes

# List all features
az appconfig feature list --name $APP_CONFIG --output table
```

---

## 🚨 Emergency Operations

```bash
# Enable maintenance mode (app goes offline in 30s)
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaintenanceMode" --value "true" --yes

# Disable maintenance mode (app comes back online in 30s)
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaintenanceMode" --value "false" --yes

# Disable problematic feature immediately
az appconfig feature disable --name $APP_CONFIG --feature "ProblematicFeature" --yes
```

---

## 🔐 Update Secrets

```bash
# Update database password (in Key Vault)
az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "SqlConnectionString" \
  --value "Server=tcp:server.database.windows.net,1433;Initial Catalog=db;User ID=user;Password=NEW_PASSWORD;..."

# Update API key
az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "ThirdPartyApiKey" \
  --value "NEW_API_KEY_HERE"
```

---

## 📋 View Configuration

```bash
# List all settings
az appconfig kv list --name $APP_CONFIG --output table

# View specific setting
az appconfig kv show --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB"

# Export all configuration to file
az appconfig kv export \
  --name $APP_CONFIG \
  --destination file \
  --path "config-backup-$(date +%Y%m%d).json" \
  --format json
```

---

## 🔄 Backup & Restore

```bash
# Backup
az appconfig kv export \
  --name $APP_CONFIG \
  --destination file \
  --path "backup.json" \
  --format json

# Restore
az appconfig kv import \
  --name $APP_CONFIG \
  --source file \
  --path "backup.json" \
  --format json
```

---

## 📊 Monitoring

```bash
# View recent configuration changes
az monitor activity-log list \
  --resource-group aicalendar-rg \
  --query "[?contains(resourceId, 'AppConfiguration')]" \
  --output table

# Check app logs
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --tail 100
```

---

## ⏱️ Timing

| Action | Time to Take Effect |
|--------|---------------------|
| Change setting | ~30 seconds |
| Toggle feature flag | ~30 seconds |
| Update secret | ~30 seconds |
| Enable maintenance mode | ~30 seconds |

**No rebuild, no redeploy, no restart required!**

---

## 🎯 Real-World Examples

### Black Friday: Increase Limits
```bash
# Before Black Friday
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "500" --yes
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:SessionTimeoutMinutes" --value "120" --yes

# After Black Friday
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "100" --yes
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:SessionTimeoutMinutes" --value "30" --yes
```

### Security Breach: Rotate API Key
```bash
# Immediately rotate compromised key
az keyvault secret set --vault-name $KEYVAULT --name "ThirdPartyApiKey" --value "NEW_SECURE_KEY"
# Takes effect in 30 seconds!
```

### A/B Testing: Gradual Rollout
```bash
# Day 1: Enable for testing
az appconfig feature enable --name $APP_CONFIG --feature "NewDashboard" --yes
# (Configure targeting rules in Portal for specific users)

# Day 7: Full rollout
# (Remove targeting rules, enable for everyone)
```

---

## 📱 From Your Phone!

All these commands work from:
- ✅ Azure Cloud Shell (portal.azure.com)
- ✅ Azure Mobile App
- ✅ Any terminal with Azure CLI

**You can change configuration from anywhere, anytime!**

---

## 🔗 Quick Links

- **Azure Portal:** https://portal.azure.com
- **App Configuration:** Search for "App Configuration" in Portal
- **Key Vault:** Search for "Key Vaults" in Portal
- **Container App:** Search for "Container Apps" in Portal

---

## 📚 Documentation

- **[APP-CONFIG-SETUP.md](./APP-CONFIG-SETUP.md)** - Setup guide
- **[RUNTIME-CONFIG-GUIDE.md](./RUNTIME-CONFIG-GUIDE.md)** - Detailed examples
- **[DEPLOYMENT-UPDATES.md](./DEPLOYMENT-UPDATES.md)** - What changed

---

**Remember:** Changes take effect in ~30 seconds. No rebuild, no redeploy, no downtime!
