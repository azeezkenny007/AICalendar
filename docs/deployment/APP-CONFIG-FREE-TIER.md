# Azure App Configuration - Free Tier Information

## ✅ Good News: $0/Month!

The deployment script has been updated to use the **Free tier** of Azure App Configuration, which means:

**Cost: $0/month** 🎉

---

## Free Tier Limits

| Feature | Limit | Is This Enough? |
|---------|-------|-----------------|
| **Requests** | 1,000/day | ✅ Yes for most dev/test scenarios |
| **Storage** | 10 MB | ✅ Yes (config is typically < 1 MB) |
| **Configuration Keys** | Unlimited | ✅ |
| **Feature Flags** | Unlimited | ✅ |
| **Key Vault References** | Unlimited | ✅ |

---

## Will 1,000 Requests/Day Be Enough?

### How Requests Are Counted

Your app checks for configuration updates every **30 seconds** (configurable).

**Calculation:**
- 1 app instance: 2,880 requests/day (24 hours × 120 checks/hour)
- Free tier: 1,000 requests/day

### ⚠️ Important Note

With the default 30-second refresh interval, you'll **exceed** the free tier limit if you run continuously.

### Solutions

**Option 1: Increase Refresh Interval (Recommended for Free Tier)**

Change from 30 seconds to 5 minutes:

```csharp
// In Program.cs
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options
        .Connect(new Uri(endpoint), new ManagedIdentityCredential())
        .ConfigureRefresh(refresh =>
        {
            refresh.Register("ApplicationSettings:*", refreshAll: true)
                   .SetCacheExpiration(TimeSpan.FromMinutes(5)); // Changed from 30 seconds
        })
        .UseFeatureFlags(featureFlagOptions =>
        {
            featureFlagOptions.CacheExpirationInterval = TimeSpan.FromMinutes(5); // Changed from 30 seconds
        });
});
```

**With 5-minute refresh:**
- 1 app instance: 288 requests/day (24 hours × 12 checks/hour)
- Free tier: 1,000 requests/day
- ✅ **You can run 3 app instances within free tier!**

**Option 2: Use Free Tier for Development, Standard for Production**

```bash
# Development (Free tier)
az appconfig create \
  --name aicalendar-config-dev \
  --sku Free

# Production (Standard tier - when needed)
az appconfig create \
  --name aicalendar-config-prod \
  --sku Standard
```

**Option 3: Manual Refresh (Most Efficient)**

Only refresh when a sentinel value changes:

```csharp
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options
        .Connect(new Uri(endpoint), new ManagedIdentityCredential())
        .ConfigureRefresh(refresh =>
        {
            // Only refresh when this specific key changes
            refresh.Register("Sentinel", refreshAll: true);
        });
});
```

Then, when you change configuration:
```bash
# 1. Update your settings
az appconfig kv set --name $APP_CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "200" --yes

# 2. Update sentinel to trigger refresh
az appconfig kv set --name $APP_CONFIG --key "Sentinel" --value "$(date +%s)" --yes
```

**Requests:** Only when you manually trigger (could be < 10/day!)

---

## Recommended Configuration for Free Tier

### For Development/Testing
```csharp
// 5-minute refresh interval
.SetCacheExpiration(TimeSpan.FromMinutes(5))
```
- **Requests/day:** ~288 per instance
- **Cost:** $0
- **Latency:** Config changes take up to 5 minutes

### For Production (Small Scale)
```csharp
// Sentinel-based refresh
.Register("Sentinel", refreshAll: true)
```
- **Requests/day:** < 10 (only when you trigger)
- **Cost:** $0
- **Latency:** Instant when you trigger sentinel

### For Production (High Scale)
```bash
# Upgrade to Standard tier
az appconfig update --name $APP_CONFIG --sku Standard
```
- **Requests/day:** 200,000 free, then $0.06/10K
- **Cost:** ~$36/month base + usage
- **Latency:** 30 seconds (or whatever you configure)

---

## Current Deployment Script Configuration

The script creates App Configuration with:
- ✅ **SKU:** Free
- ✅ **Cost:** $0/month
- ⚠️ **Refresh Interval:** 30 seconds (in your app code)

**Recommendation:** Update your app code to use 5-minute refresh interval to stay within free tier limits.

---

## How to Check Your Usage

```bash
# Get your App Configuration name
APP_CONFIG=$(az appconfig list --resource-group aicalendar-rg --query "[0].name" -o tsv)

# View metrics in Azure Portal
# Go to: App Configuration → Monitoring → Metrics
# Select metric: "Total Requests"
```

---

## When to Upgrade to Standard

Consider upgrading when:
- ✅ You have multiple production app instances
- ✅ You need < 30 second refresh intervals
- ✅ You exceed 1,000 requests/day consistently
- ✅ You need 99.9% SLA

**Upgrade command:**
```bash
az appconfig update \
  --name $APP_CONFIG \
  --sku Standard
```

**Cost:** ~$36/month + usage

---

## Summary

| Scenario | Tier | Refresh Interval | Instances | Cost |
|----------|------|------------------|-----------|------|
| **Development** | Free | 5 minutes | 1-3 | $0 |
| **Small Production** | Free | Sentinel-based | 1-5 | $0 |
| **Large Production** | Standard | 30 seconds | Unlimited | ~$36/mo |

---

## Your Current Setup

✅ **Free tier** ($0/month)
✅ All features available (settings, feature flags, Key Vault references)
⚠️ **Action Required:** Update app code to use 5-minute refresh interval

**No charges will be incurred for App Configuration!** 🎉

---

## Additional Resources

- [Azure App Configuration Pricing](https://azure.microsoft.com/en-us/pricing/details/app-configuration/)
- [Free Tier Limits](https://learn.microsoft.com/en-us/azure/azure-app-configuration/faq#are-there-any-limits-on-the-number-of-requests-made-to-app-configuration)
