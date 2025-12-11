# AICalendar - Quick Reference Guide

## 🚀 Initial Deployment

```bash
# 1. Login and set subscription
az login
az account set --subscription "<subscription-id>"

# 2. Deploy infrastructure
chmod +x deploy-infrastructure.sh
./deploy-infrastructure.sh

# 3. Setup Azure DevOps
nano setup-azure-devops.sh  # Edit organization name
chmod +x setup-azure-devops.sh
./setup-azure-devops.sh

# 4. Save credentials file securely!
# File: deployment-credentials-YYYYMMDD-HHMMSS.txt
```

---

## 📝 Daily Development Workflow

### Working on Features

```bash
# Start development
git checkout dev
git pull origin dev

# Make changes
# ... edit files ...

# Commit and push (auto-deploys to dev)
git add .
git commit -m "Add feature X"
git push origin dev

# Check deployment
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --follow
```

### Deploying to Production

```bash
# 1. Ensure dev is tested and working
# 2. Go to Azure DevOps
# 3. Create Pull Request: dev → main
# 4. Get approval from team member
# 5. Complete PR (triggers production deployment)
# 6. Monitor deployment in Azure DevOps Pipelines
```

---

## 🔍 Monitoring Commands

### Check Application Status

```bash
# Get application URL
az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv

# Check if app is running
APP_URL=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv)
curl https://$APP_URL/health/live
```

### View Logs

```bash
# Live logs
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --follow

# Recent logs (last 100 lines)
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --tail 100
```

### Check Revisions

```bash
# List all revisions
az containerapp revision list \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --output table

# Show traffic distribution
az containerapp revision list \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query "[].{Name:name,Active:properties.active,Traffic:properties.trafficWeight,Created:properties.createdTime}" \
  --output table
```

---

## 🔐 Secrets Management

### View Secrets in Key Vault

```bash
# Get Key Vault name
KEYVAULT=$(az keyvault list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

# List all secrets
az keyvault secret list \
  --vault-name $KEYVAULT \
  --output table

# Get specific secret
az keyvault secret show \
  --vault-name $KEYVAULT \
  --name SqlConnectionString \
  --query value -o tsv
```

### Add New Secret

```bash
KEYVAULT=$(az keyvault list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

az keyvault secret set \
  --vault-name $KEYVAULT \
  --name "MyNewSecret" \
  --value "secret-value"
```

---

## 🔄 Rollback Procedures

### Instant Rollback (During Canary)

```bash
# If deployment is in canary phase (10% traffic)
# List revisions
az containerapp revision list \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query "[?properties.active==\`true\`].[name,properties.trafficWeight]" \
  --output table

# Switch back to previous version
az containerapp ingress traffic set \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --revision-weight <PREVIOUS_REVISION>=100 <NEW_REVISION>=0
```

### Full Rollback (After 100% Deployment)

```bash
# Find previous stable revision
az containerapp revision list \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --output table

# Activate and route traffic to old revision
az containerapp revision activate \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --revision <OLD_REVISION_NAME>

az containerapp ingress traffic set \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --revision-weight <OLD_REVISION_NAME>=100
```

---

## ⚙️ Scaling

### Manual Scaling

```bash
# Scale up
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --min-replicas 3 \
  --max-replicas 10

# Scale down
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --min-replicas 1 \
  --max-replicas 3
```

### Auto-scaling (HTTP Requests)

```bash
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 50
```

---

## 🗄️ Database Operations

### Connect to SQL Database

```bash
# Get connection details
KEYVAULT=$(az keyvault list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

SQL_CONN=$(az keyvault secret show \
  --vault-name $KEYVAULT \
  --name SqlConnectionString \
  --query value -o tsv)

# Note: Direct connection won't work from outside VNet
# Use Azure Portal Query Editor or connect from Container App
```

### Run Database Migrations

```bash
# SSH into running container (if enabled)
az containerapp exec \
  --name aicalendar-api \
  --resource-group aicalendar-rg

# Or create a job for migrations
az containerapp job create \
  --name db-migration \
  --resource-group aicalendar-rg \
  --environment aicalendar-env \
  --trigger-type Manual \
  --image <your-migration-image> \
  --cpu 0.5 \
  --memory 1.0Gi
```

---

## 🐳 Docker Operations

### Build and Push Manually

```bash
# Get ACR details
KEYVAULT=$(az keyvault list \
  --resource-group aicalendar-rg \
  --query "[0].name" -o tsv)

ACR_SERVER=$(az keyvault secret show \
  --vault-name $KEYVAULT \
  --name AcrLoginServer \
  --query value -o tsv)

ACR_USERNAME=$(az keyvault secret show \
  --vault-name $KEYVAULT \
  --name AcrUsername \
  --query value -o tsv)

ACR_PASSWORD=$(az keyvault secret show \
  --vault-name $KEYVAULT \
  --name AcrPassword \
  --query value -o tsv)

# Login to ACR
docker login $ACR_SERVER -u $ACR_USERNAME -p $ACR_PASSWORD

# Build and push
docker build -t $ACR_SERVER/aicalendar-api:manual .
docker push $ACR_SERVER/aicalendar-api:manual

# Update container app
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --image $ACR_SERVER/aicalendar-api:manual
```

---

## 🧪 Testing

### Health Check

```bash
APP_URL=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv)

# Liveness probe
curl https://$APP_URL/health/live

# Readiness probe (checks dependencies)
curl https://$APP_URL/health/ready
```

### Load Testing

```bash
# Simple load test with Apache Bench
ab -n 1000 -c 10 https://$APP_URL/

# Or use Azure Load Testing (recommended)
# Create load test in Azure Portal
```

---

## 🧹 Cleanup

### Stop Application (Keep Infrastructure)

```bash
# Scale to zero (stops billing for Container App)
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --min-replicas 0 \
  --max-replicas 0
```

### Delete Entire Deployment

```bash
# ⚠️ WARNING: This deletes EVERYTHING!
az group delete \
  --name aicalendar-rg \
  --yes \
  --no-wait

# Also delete Azure DevOps project
# Must be done through UI at:
# https://dev.azure.com/{org}/_settings/
```

---

## 🆘 Emergency Procedures

### Application Won't Start

```bash
# 1. Check logs
az containerapp logs show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --tail 200

# 2. Check replica status
az containerapp replica list \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --output table

# 3. Restart application
az containerapp revision restart \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --revision <REVISION_NAME>
```

### Database Connection Issues

```bash
# 1. Verify private endpoint
az network private-endpoint list \
  --resource-group aicalendar-rg \
  --output table

# 2. Check DNS resolution from container
az containerapp exec \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --command "nslookup <sql-server-name>.database.windows.net"

# 3. Verify Key Vault access
IDENTITY=$(az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query identity.principalId -o tsv)

echo "Managed Identity: $IDENTITY"
```

### Out of Memory

```bash
# Increase memory allocation
az containerapp update \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --cpu 1.0 \
  --memory 2.0Gi
```

---

## 📊 Useful Queries

### Resource Usage

```bash
# Get all resources in resource group
az resource list \
  --resource-group aicalendar-rg \
  --output table

# Get costs (last 30 days)
az consumption usage list \
  --start-date $(date -d "30 days ago" +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d) \
  | jq '.[] | select(.instanceName | contains("aicalendar"))'
```

### Network Configuration

```bash
# Show VNet details
az network vnet show \
  --resource-group aicalendar-rg \
  --name aicalendar-vnet \
  --output table

# List subnets
az network vnet subnet list \
  --resource-group aicalendar-rg \
  --vnet-name aicalendar-vnet \
  --output table

# Check private DNS zones
az network private-dns zone list \
  --resource-group aicalendar-rg \
  --output table
```

---

## 💡 Pro Tips

1. **Create aliases for common commands:**
   ```bash
   # Add to ~/.bashrc or ~/.zshrc
   alias acalogs='az containerapp logs show --name aicalendar-api --resource-group aicalendar-rg --follow'
   alias acastatus='az containerapp show --name aicalendar-api --resource-group aicalendar-rg'
   ```

2. **Set default resource group:**
   ```bash
   az configure --defaults group=aicalendar-rg
   # Now you can omit --resource-group in commands
   ```

3. **Use Azure Cloud Shell:**
   - Access from Azure Portal (icon in top bar)
   - Pre-configured with Azure CLI
   - Persistent storage for scripts

4. **Enable bash completion:**
   ```bash
   # Add to ~/.bashrc
   source <(az completion --shell bash)
   ```

---

## 📚 Documentation Links

- **Container Apps:** https://learn.microsoft.com/azure/container-apps/
- **Key Vault:** https://learn.microsoft.com/azure/key-vault/
- **Azure CLI:** https://learn.microsoft.com/cli/azure/
- **Azure DevOps:** https://learn.microsoft.com/azure/devops/

---

**Last Updated:** December 2024
