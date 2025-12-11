# AICalendar - Professional Azure Deployment Guide

Complete infrastructure-as-code deployment for a production-ready financial application with enterprise security.

## 📋 Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Deployment Scripts](#deployment-scripts)
4. [Azure DevOps Setup](#azure-devops-setup)
5. [CI/CD Pipeline](#cicd-pipeline)
6. [Application Configuration](#application-configuration)
7. [Monitoring & Operations](#monitoring--operations)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools

- **Azure CLI** (latest version)
  ```bash
  # Install Azure CLI
  curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

  # Or on macOS
  brew install azure-cli

  # Verify installation
  az --version
  ```

- **Git**
  ```bash
  git --version
  ```

- **Docker** (for local development)
  ```bash
  docker --version
  ```

### Azure Account Requirements

- Active Azure subscription
- Permissions to create resources
- Azure DevOps organization (free tier available)

---

## Quick Start

### 1. Clone This Repository

```bash
git clone <your-repo-url>
cd aicalendar
```

### 2. Login to Azure

```bash
az login
```

### 3. Set Your Subscription

```bash
# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription "<subscription-id>"
```

### 4. Run Infrastructure Deployment

```bash
chmod +x deploy-infrastructure.sh
./deploy-infrastructure.sh
```

This will create:
- ✅ Resource Group
- ✅ Virtual Network with subnets
- ✅ Azure Container Registry
- ✅ Azure SQL Database (with private endpoint)
- ✅ Azure Redis Cache (with private endpoint)
- ✅ Azure Key Vault
- ✅ Container Apps Environment
- ✅ Container App (with managed identity)
- ✅ Log Analytics Workspace

**Duration:** ~20-30 minutes

### 5. Save the Credentials

The script will create a file: `deployment-credentials-YYYYMMDD-HHMMSS.txt`

**IMPORTANT:** Store these credentials securely and delete the file after saving elsewhere!

---

## Deployment Scripts

### Infrastructure Deployment (`deploy-infrastructure.sh`)

Creates all Azure resources with enterprise security configurations.

**Key Features:**
- Private endpoints for SQL and Redis
- Managed identity for Container App
- Secrets stored in Key Vault
- Network isolation with VNet
- Professional naming with random suffixes

**Customization:**

Edit these variables at the top of the script:

```bash
LOCATION="westeurope"           # Change to your preferred region
RESOURCE_GROUP="aicalendar-rg"  # Change resource group name
```

### What Gets Created

```
Resource Group: aicalendar-rg
├── Virtual Network (10.0.0.0/16)
│   ├── aca-subnet (10.0.0.0/21) - Container Apps
│   └── data-subnet (10.0.8.0/24) - SQL & Redis
├── Container Registry (aicalendaracrXXXXXX)
├── SQL Server & Database
│   └── Private Endpoint → data-subnet
├── Redis Cache
│   └── Private Endpoint → data-subnet
├── Key Vault (aicalendar-kvXXXXXX)
│   ├── SqlConnectionString
│   ├── RedisConnectionString
│   └── ACR credentials
├── Log Analytics Workspace
├── Container Apps Environment
└── Container App (with Managed Identity)
```

---

## Azure DevOps Setup

### 1. Create Azure DevOps Organization

1. Go to https://dev.azure.com
2. Sign in with your Azure account
3. Create organization (or use existing)
4. Note your organization name

### 2. Run DevOps Setup Script

```bash
# Edit the script with your organization name
nano setup-azure-devops.sh

# Update this line:
ORGANIZATION_NAME="your-org-name"

# Run the script
chmod +x setup-azure-devops.sh
./setup-azure-devops.sh
```

This creates:
- ✅ Azure DevOps project
- ✅ Git repository
- ✅ Branch structure (main, dev)
- ✅ Branch policies
- ✅ Variable groups

### 3. Create Service Connections (Manual)

Go to: `https://dev.azure.com/{org}/{project}/_settings/adminservices`

#### Azure Resource Manager Connection

1. Click **"New service connection"**
2. Select **"Azure Resource Manager"**
3. Choose **"Service principal (automatic)"**
4. Select your subscription
5. Resource group: `aicalendar-rg`
6. Name: `Azure-ServiceConnection`
7. Grant permissions to all pipelines: ✅
8. Click **"Save"**

#### Azure Container Registry Connection

1. Click **"New service connection"**
2. Select **"Docker Registry"**
3. Select **"Azure Container Registry"**
4. Choose your subscription
5. Select your ACR
6. Name: `ACR-ServiceConnection`
7. Grant permissions to all pipelines: ✅
8. Click **"Save"**

---

## CI/CD Pipeline

### 1. Add Pipeline YAML to Repository

Create `azure-pipelines.yml` in your repo root (content provided in artifact above).

### 2. Create the Pipeline

1. Go to Pipelines → New Pipeline
2. Select **Azure Repos Git**
3. Select your repository
4. Select **Existing Azure Pipelines YAML file**
5. Choose `azure-pipelines.yml`
6. Click **Run**

### 3. Pipeline Workflow

```
┌─────────────┐
│   Commit    │
└──────┬──────┘
       │
       ↓
┌─────────────────────┐
│   Build Stage       │
│ - Build Docker      │
│ - Push to ACR       │
│ - Security Scan     │
└──────┬──────────────┘
       │
       ├──→ [dev branch] ──→ Deploy to Dev Environment
       │                      - Update container app
       │                      - Run health checks
       │
       └──→ [main branch] ──→ Deploy to Production
                              - Create new revision
                              - Canary (10% traffic)
                              - Full deployment (100%)
                              - Deactivate old revisions
```

### Branch Strategy

**Development Branch (`dev`):**
- No restrictions
- Direct commits allowed
- Auto-deploys to dev environment
- Use for feature development

**Main Branch (`main`):**
- Protected branch
- Requires 1 PR approval
- Auto-deploys to production
- Only merge tested code

**Workflow:**

```bash
# Daily development
git checkout dev
# ... make changes ...
git add .
git commit -m "Add new feature"
git push origin dev
# ✓ Auto-deploys to dev environment

# Deploy to production
# 1. Create Pull Request: dev → main
# 2. Get approval
# 3. Complete PR
# ✓ Auto-deploys to production with canary
```

---

## Application Configuration

### Using Key Vault in Your App

Your Container App has a **Managed Identity** that can read from Key Vault.

#### ASP.NET Core Configuration

```csharp
// Program.cs
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Get Key Vault name from environment
var keyVaultName = Environment.GetEnvironmentVariable("KeyVaultName");
var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

// Add Key Vault to configuration
builder.Configuration.AddAzureKeyVault(
    keyVaultUri,
    new DefaultAzureCredential()
);

// Now you can access secrets
var sqlConnection = builder.Configuration["SqlConnectionString"];
var redisConnection = builder.Configuration["RedisConnectionString"];

// Configure services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(sqlConnection));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
});
```

### Health Endpoints

Create health check endpoints for Container Apps probes:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration["SqlConnectionString"],
        name: "database",
        tags: new[] { "ready" })
    .AddRedis(
        redisConnectionString: builder.Configuration["RedisConnectionString"],
        name: "redis",
        tags: new[] { "ready" });

var app = builder.Build();

// Liveness probe - just checks if app is running
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Don't check dependencies
});

// Readiness probe - checks if app can serve traffic
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Configure Health Probes in Container App

```bash
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --health-probe-type liveness \
  --health-probe-path /health/live \
  --health-probe-interval 30 \
  --health-probe-timeout 5
```

---

## Monitoring & Operations

### View Logs

```bash
# Stream live logs
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --follow

# View recent logs
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --tail 100
```

### Check Application Status

```bash
# Get app URL
az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv

# Check revision status
az containerapp revision list \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query "[].{Name:name,Active:properties.active,Traffic:properties.trafficWeight}" -o table
```

### Manual Rollback

If production deployment fails or has issues:

```bash
# 1. List active revisions
az containerapp revision list \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query "[?properties.active==\`true\`].[name,properties.trafficWeight]" -o table

# 2. Switch traffic to previous revision
az containerapp ingress traffic set \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --revision-weight <PREVIOUS_REVISION_NAME>=100
```

### Scale Application

```bash
# Manual scaling
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --min-replicas 2 \
  --max-replicas 10

# Configure autoscaling rules
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 50
```

---

## Troubleshooting

### Common Issues

#### 1. Deployment Script Fails: "Region Not Allowed"

**Problem:** Your subscription has region restrictions.

**Solution:**
```bash
# Check allowed regions
az policy assignment list --query "[?parameters.listOfAllowedLocations.value!=null].parameters.listOfAllowedLocations.value[]"

# Edit deploy-infrastructure.sh
LOCATION="<one-of-your-allowed-regions>"
```

#### 2. Container App Can't Connect to SQL/Redis

**Problem:** Private DNS not resolving correctly.

**Solution:**
```bash
# Verify private endpoints
az network private-endpoint list \
  --resource-group aicalendar-rg \
  --output table

# Check DNS zones
az network private-dns zone list \
  --resource-group aicalendar-rg \
  --output table
```

#### 3. Pipeline Fails: "Service Connection Not Found"

**Problem:** Service connections not created or not authorized.

**Solution:**
1. Go to Azure DevOps → Project Settings → Service Connections
2. Verify both connections exist
3. Check "Grant access to all pipelines"

#### 4. Key Vault Access Denied

**Problem:** Managed identity doesn't have Key Vault permissions.

**Solution:**
```bash
# Get managed identity
IDENTITY=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query identity.principalId -o tsv)

# Grant access
KEYVAULT=$(az keyvault list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

az keyvault set-policy \
  --name $KEYVAULT \
  --object-id $IDENTITY \
  --secret-permissions get list
```

---

## Cost Optimization

### Estimated Monthly Costs (Student/Dev)

| Service | SKU | Cost |
|---------|-----|------|
| Container Apps | Consumption | ~$10-20 |
| Container Registry | Basic | ~$5 |
| SQL Database | Basic | ~$5 |
| Redis Cache | Basic C0 | ~$16 |
| Key Vault | Standard | ~$0.03 |
| VNet | Standard | Free |
| **Total** | | **~$36-46/month** |

### Production Costs

| Service | SKU | Cost |
|---------|-----|------|
| Container Apps | Consumption | ~$50-100 |
| Container Registry | Standard | ~$20 |
| SQL Database | Standard S2 | ~$150 |
| Redis Cache | Standard C1 | ~$75 |
| Application Gateway | WAF V2 | ~$180 |
| **Total** | | **~$475-525/month** |

### Cost Saving Tips

1. **Use serverless tiers:**
   - SQL: Serverless compute
   - Container Apps: Consumption plan

2. **Delete when not in use:**
   ```bash
   # Stop Container App (keeps config)
   az containerapp update \
     --name aicalendar-api \
     --resource-group aicalendar-rg \
     --min-replicas 0 \
     --max-replicas 0
   ```

3. **Use dev/test pricing:**
   - SQL Dev/Test pricing (40% off)
   - Available with Visual Studio subscriptions

---

## Security Checklist

- ✅ All secrets in Key Vault (not in code/env vars)
- ✅ Managed Identity for authentication (no passwords)
- ✅ Private endpoints for data services
- ✅ Network isolation with VNet
- ✅ HTTPS enforced
- ✅ SQL: Firewall disabled, private-only
- ✅ Redis: TLS required, private-only
- ✅ Container Registry: Admin enabled (dev), use managed identity (prod)
- ✅ Health probes configured
- ✅ Logging enabled

---

## Next Steps

1. **Customize Application Code**
   - Add your business logic
   - Implement authentication
   - Add API endpoints

2. **Configure Custom Domain**
   ```bash
   az containerapp hostname add \
     --name aicalendar-api \
     --resource-group aicalendar-rg \
     --hostname yourdomain.com
   ```

3. **Set Up Application Insights**
   - Monitor performance
   - Track errors
   - User analytics

4. **Add Application Gateway** (Optional)
   - WAF protection
   - SSL offloading
   - Advanced routing

5. **Multi-Region Deployment**
   - Traffic Manager
   - Geo-redundancy
   - Disaster recovery

---

## Support & Documentation

- **Azure Container Apps:** https://learn.microsoft.com/azure/container-apps/
- **Azure DevOps:** https://learn.microsoft.com/azure/devops/
- **Key Vault:** https://learn.microsoft.com/azure/key-vault/
- **Course Materials:** See slide decks (Days 1-7)

---

## License

[Your License Here]

## Contributors

[Your Name/Team]
