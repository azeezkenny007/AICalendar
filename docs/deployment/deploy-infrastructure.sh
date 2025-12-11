#!/bin/bash

#######################################
# Azure Infrastructure Deployment Script
# Professional Setup for AICalendar Application
#######################################

set -e  # Exit on any error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Configuration Variables
RESOURCE_GROUP="aicalendar-rg"
LOCATION="spaincentral"  # Changed to Spain Central (allowed region)
VNET_NAME="aicalendar-vnet"
ACA_SUBNET_NAME="aca-subnet"
DATA_SUBNET_NAME="data-subnet"
ACR_NAME="aicalendaracr$(openssl rand -hex 3)"  # Random suffix for uniqueness
SQL_SERVER_NAME="aicalendar-sql-$(openssl rand -hex 3)"
SQL_DB_NAME="AICalendarDb"
SQL_ADMIN_USER="azureuser"
REDIS_NAME="aicalendar-redis-$(openssl rand -hex 3)"
KEYVAULT_NAME="aicalendar-kv-$(openssl rand -hex 3)"
APP_CONFIG_NAME="aicalendar-config-$(openssl rand -hex 3)"
CONTAINER_ENV_NAME="aicalendar-env"
CONTAINER_APP_NAME="aicalendar-api"
LOG_ANALYTICS_NAME="aicalendar-logs"

# Generate secure passwords
SQL_ADMIN_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)

#######################################
# Pre-flight Checks
#######################################
log_info "Starting pre-flight checks..."

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    log_error "Azure CLI is not installed. Please install it first."
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    log_error "Not logged in to Azure. Please run: az login"
    exit 1
fi

# Get current subscription
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
log_info "Using subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"

# Confirm before proceeding
read -p "Continue with deployment? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    log_warning "Deployment cancelled by user"
    exit 0
fi

#######################################
# Step 1: Create Resource Group
#######################################
log_info "Creating resource group: $RESOURCE_GROUP"
az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION \
    --output none

log_success "Resource group created"

#######################################
# Step 2: Create Virtual Network
#######################################
log_info "Creating virtual network: $VNET_NAME"
az network vnet create \
    --resource-group $RESOURCE_GROUP \
    --name $VNET_NAME \
    --location $LOCATION \
    --address-prefix 10.0.0.0/16 \
    --output none

log_success "Virtual network created"

# Wait for VNet to be fully provisioned and accessible
log_info "Waiting for VNet to be fully provisioned..."
for i in {1..30}; do
    if az network vnet show --resource-group $RESOURCE_GROUP --name $VNET_NAME &>/dev/null; then
        log_info "VNet is ready!"
        break
    fi
    if [ $i -eq 30 ]; then
        log_error "VNet failed to become accessible after 30 seconds"
        exit 1
    fi
    sleep 1
done

# Create ACA subnet (Container Apps)
log_info "Creating ACA subnet: $ACA_SUBNET_NAME"
az network vnet subnet create \
    --resource-group $RESOURCE_GROUP \
    --vnet-name $VNET_NAME \
    --name $ACA_SUBNET_NAME \
    --address-prefixes 10.0.0.0/21 \
    --output none

# Delegate ACA subnet to Container Apps
log_info "Delegating subnet to Microsoft.App/environments"
az network vnet subnet update \
    --resource-group $RESOURCE_GROUP \
    --vnet-name $VNET_NAME \
    --name $ACA_SUBNET_NAME \
    --delegations Microsoft.App/environments \
    --output none

log_success "ACA subnet created and delegated"

# Wait for subnet to be fully provisioned
log_info "Waiting for ACA subnet to be accessible..."
for i in {1..20}; do
    if az network vnet subnet show --resource-group $RESOURCE_GROUP --vnet-name $VNET_NAME --name $ACA_SUBNET_NAME &>/dev/null; then
        break
    fi
    sleep 1
done

# Create Data subnet (SQL, Redis)
log_info "Creating data subnet: $DATA_SUBNET_NAME"
az network vnet subnet create \
    --resource-group $RESOURCE_GROUP \
    --vnet-name $VNET_NAME \
    --name $DATA_SUBNET_NAME \
    --address-prefixes 10.0.8.0/24 \
    --output none

log_success "Data subnet created"

#######################################
# Step 3: Create Log Analytics Workspace
#######################################
log_info "Creating Log Analytics workspace: $LOG_ANALYTICS_NAME"
az monitor log-analytics workspace create \
    --resource-group $RESOURCE_GROUP \
    --workspace-name $LOG_ANALYTICS_NAME \
    --location $LOCATION \
    --output none

LOG_ANALYTICS_ID=$(az monitor log-analytics workspace show \
    --resource-group $RESOURCE_GROUP \
    --workspace-name $LOG_ANALYTICS_NAME \
    --query customerId -o tsv)

LOG_ANALYTICS_KEY=$(az monitor log-analytics workspace get-shared-keys \
    --resource-group $RESOURCE_GROUP \
    --workspace-name $LOG_ANALYTICS_NAME \
    --query primarySharedKey -o tsv)

log_success "Log Analytics workspace created"

#######################################
# Step 4: Create Azure Container Registry
#######################################
log_info "Creating Azure Container Registry: $ACR_NAME"
az acr create \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --location $LOCATION \
    --sku Basic \
    --admin-enabled true \
    --output none

# Wait for ACR to be fully provisioned
log_info "Waiting for ACR to be fully provisioned..."
for i in {1..60}; do
    if az acr show --resource-group $RESOURCE_GROUP --name $ACR_NAME &>/dev/null; then
        log_info "ACR is ready!"
        break
    fi
    if [ $i -eq 60 ]; then
        log_error "ACR failed to become accessible after 60 seconds"
        exit 1
    fi
    sleep 1
done

# Get ACR credentials
ACR_LOGIN_SERVER=$(az acr show \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --query loginServer -o tsv)

ACR_USERNAME=$(az acr credential show \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --query username -o tsv)

ACR_PASSWORD=$(az acr credential show \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --query passwords[0].value -o tsv)

log_success "Azure Container Registry created: $ACR_LOGIN_SERVER"

#######################################
# Step 5: Create Azure Key Vault
#######################################
log_info "Creating Azure Key Vault: $KEYVAULT_NAME"
az keyvault create \
    --resource-group $RESOURCE_GROUP \
    --name $KEYVAULT_NAME \
    --location $LOCATION \
    --enable-rbac-authorization false \
    --output none

log_success "Key Vault created"

#######################################
# Step 6: Create Azure SQL Database
#######################################
log_info "Creating Azure SQL Server: $SQL_SERVER_NAME"
az sql server create \
    --resource-group $RESOURCE_GROUP \
    --name $SQL_SERVER_NAME \
    --location $LOCATION \
    --admin-user $SQL_ADMIN_USER \
    --admin-password "$SQL_ADMIN_PASSWORD" \
    --enable-public-network false \
    --output none

log_success "SQL Server created"

# Create SQL Database
log_info "Creating SQL Database: $SQL_DB_NAME"
az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name $SQL_DB_NAME \
    --edition GeneralPurpose \
    --compute-model Serverless \
    --family Gen5 \
    --capacity 2 \
    --auto-pause-delay 60 \
    --min-capacity 0.5 \
    --backup-storage-redundancy Local \
    --output none

log_success "SQL Database created"

# Get subnet ID for private endpoint
DATA_SUBNET_ID=$(az network vnet subnet show \
    --resource-group $RESOURCE_GROUP \
    --vnet-name $VNET_NAME \
    --name $DATA_SUBNET_NAME \
    --query id -o tsv)

# Create private endpoint for SQL
log_info "Creating SQL private endpoint"

# Get SQL Server resource ID (store in variable to avoid Git Bash path conversion)
SQL_SERVER_ID=$(az sql server show \
    --resource-group $RESOURCE_GROUP \
    --name $SQL_SERVER_NAME \
    --query id -o tsv)

# Create private endpoint (use MSYS_NO_PATHCONV to prevent Git Bash path conversion)
MSYS_NO_PATHCONV=1 az network private-endpoint create \
    --resource-group $RESOURCE_GROUP \
    --name sql-private-endpoint \
    --location $LOCATION \
    --vnet-name $VNET_NAME \
    --subnet $DATA_SUBNET_NAME \
    --private-connection-resource-id "$SQL_SERVER_ID" \
    --group-id sqlServer \
    --connection-name sql-connection \
    --output none

# Create private DNS zone for SQL
log_info "Configuring SQL private DNS"
az network private-dns zone create \
    --resource-group $RESOURCE_GROUP \
    --name privatelink.database.windows.net \
    --output none

az network private-dns link vnet create \
    --resource-group $RESOURCE_GROUP \
    --zone-name privatelink.database.windows.net \
    --name sql-dns-link \
    --virtual-network $VNET_NAME \
    --registration-enabled false \
    --output none

# Get private endpoint NIC ID
SQL_PE_NIC_ID=$(az network private-endpoint show \
    --resource-group $RESOURCE_GROUP \
    --name sql-private-endpoint \
    --query 'networkInterfaces[0].id' -o tsv)

SQL_PRIVATE_IP=$(MSYS_NO_PATHCONV=1 az network nic show \
    --ids "$SQL_PE_NIC_ID" \
    --query 'ipConfigurations[0].privateIPAddress' -o tsv)

# Create DNS record
az network private-dns record-set a create \
    --resource-group $RESOURCE_GROUP \
    --zone-name privatelink.database.windows.net \
    --name $SQL_SERVER_NAME \
    --output none

az network private-dns record-set a add-record \
    --resource-group $RESOURCE_GROUP \
    --zone-name privatelink.database.windows.net \
    --record-set-name $SQL_SERVER_NAME \
    --ipv4-address $SQL_PRIVATE_IP \
    --output none

log_success "SQL private endpoint configured"

# Store SQL connection string in Key Vault
SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=${SQL_DB_NAME};Persist Security Info=False;User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set \
    --vault-name $KEYVAULT_NAME \
    --name "SqlConnectionString" \
    --value "$SQL_CONNECTION_STRING" \
    --output none

log_success "SQL connection string stored in Key Vault"

#######################################
# Step 7: Create Azure App Configuration
#######################################
log_info "Creating Azure App Configuration: $APP_CONFIG_NAME"
az appconfig create \
    --resource-group $RESOURCE_GROUP \
    --name $APP_CONFIG_NAME \
    --location $LOCATION \
    --sku Free \
    --enable-public-network true \
    --output none

log_success "App Configuration created"

# Get App Configuration endpoint
APP_CONFIG_ENDPOINT=$(az appconfig show \
    --resource-group $RESOURCE_GROUP \
    --name $APP_CONFIG_NAME \
    --query endpoint -o tsv)

log_info "App Configuration endpoint: $APP_CONFIG_ENDPOINT"

# Populate initial configuration values
log_info "Populating App Configuration with initial settings"

# Application Settings
az appconfig kv set \
    --name $APP_CONFIG_NAME \
    --key "ApplicationSettings:MaxUploadSizeMB" \
    --value "100" \
    --yes \
    --output none

az appconfig kv set \
    --name $APP_CONFIG_NAME \
    --key "ApplicationSettings:SessionTimeoutMinutes" \
    --value "30" \
    --yes \
    --output none

az appconfig kv set \
    --name $APP_CONFIG_NAME \
    --key "ApplicationSettings:EnableSwagger" \
    --value "true" \
    --yes \
    --output none

az appconfig kv set \
    --name $APP_CONFIG_NAME \
    --key "ApplicationSettings:MaintenanceMode" \
    --value "false" \
    --yes \
    --output none

# Feature Flags
az appconfig feature set \
    --name $APP_CONFIG_NAME \
    --feature "NewDashboard" \
    --yes \
    --output none

az appconfig feature set \
    --name $APP_CONFIG_NAME \
    --feature "BetaAccess" \
    --yes \
    --output none

az appconfig feature disable \
    --name $APP_CONFIG_NAME \
    --feature "NewDashboard" \
    --yes \
    --output none

az appconfig feature disable \
    --name $APP_CONFIG_NAME \
    --feature "BetaAccess" \
    --yes \
    --output none

# Create Key Vault references in App Configuration
log_info "Creating Key Vault references in App Configuration"

KEYVAULT_URI="https://${KEYVAULT_NAME}.vault.azure.net"

az appconfig kv set-keyvault \
    --name $APP_CONFIG_NAME \
    --key "ConnectionStrings:DefaultConnection" \
    --secret-identifier "${KEYVAULT_URI}/secrets/SqlConnectionString" \
    --yes \
    --output none

log_success "App Configuration populated with initial settings and Key Vault references"

#######################################
# Step 8: Create Azure Redis Cache
#######################################
log_info "Creating Azure Redis Cache: $REDIS_NAME (this may take 10-15 minutes)"
az redis create \
    --resource-group $RESOURCE_GROUP \
    --name $REDIS_NAME \
    --location $LOCATION \
    --sku Basic \
    --vm-size c0 \
    --output none

log_success "Redis Cache created"

# Create private endpoint for Redis
log_info "Creating Redis private endpoint"

# Get Redis resource ID (store in variable to avoid Git Bash path conversion)
REDIS_ID=$(az redis show \
    --resource-group $RESOURCE_GROUP \
    --name $REDIS_NAME \
    --query id -o tsv)

# Create private endpoint (use MSYS_NO_PATHCONV to prevent Git Bash path conversion)
MSYS_NO_PATHCONV=1 az network private-endpoint create \
    --resource-group $RESOURCE_GROUP \
    --name redis-private-endpoint \
    --location $LOCATION \
    --vnet-name $VNET_NAME \
    --subnet $DATA_SUBNET_NAME \
    --private-connection-resource-id "$REDIS_ID" \
    --group-id redisCache \
    --connection-name redis-connection \
    --output none

# Create private DNS zone for Redis
log_info "Configuring Redis private DNS"
az network private-dns zone create \
    --resource-group $RESOURCE_GROUP \
    --name privatelink.redis.cache.windows.net \
    --output none

az network private-dns link vnet create \
    --resource-group $RESOURCE_GROUP \
    --zone-name privatelink.redis.cache.windows.net \
    --name redis-dns-link \
    --virtual-network $VNET_NAME \
    --registration-enabled false \
    --output none

# Get Redis private IP
REDIS_PE_NIC_ID=$(az network private-endpoint show \
    --resource-group $RESOURCE_GROUP \
    --name redis-private-endpoint \
    --query 'networkInterfaces[0].id' -o tsv)

REDIS_PRIVATE_IP=$(MSYS_NO_PATHCONV=1 az network nic show \
    --ids "$REDIS_PE_NIC_ID" \
    --query 'ipConfigurations[0].privateIPAddress' -o tsv)

# Create DNS record
az network private-dns record-set a create \
    --resource-group $RESOURCE_GROUP \
    --zone-name privatelink.redis.cache.windows.net \
    --name $REDIS_NAME \
    --output none

az network private-dns record-set a add-record \
    --resource-group $RESOURCE_GROUP \
    --zone-name privatelink.redis.cache.windows.net \
    --record-set-name $REDIS_NAME \
    --ipv4-address $REDIS_PRIVATE_IP \
    --output none

log_success "Redis private endpoint configured"

# Disable public network access on Redis (now that private endpoint is configured)
log_info "Disabling public network access on Redis"
az redis update \
    --resource-group $RESOURCE_GROUP \
    --name $REDIS_NAME \
    --set publicNetworkAccess=Disabled \
    --output none

# Get Redis access key and create connection string
REDIS_ACCESS_KEY=$(az redis list-keys \
    --resource-group $RESOURCE_GROUP \
    --name $REDIS_NAME \
    --query primaryKey -o tsv)

REDIS_CONNECTION_STRING="${REDIS_NAME}.redis.cache.windows.net:6380,password=${REDIS_ACCESS_KEY},ssl=True,abortConnect=False"

# Store Redis connection string in Key Vault
az keyvault secret set \
    --vault-name $KEYVAULT_NAME \
    --name "RedisConnectionString" \
    --value "$REDIS_CONNECTION_STRING" \
    --output none

log_success "Redis connection string stored in Key Vault"

#######################################
# Step 8: Create Container Apps Environment
#######################################
log_info "Creating Container Apps Environment: $CONTAINER_ENV_NAME"

# Get subnet ID
ACA_SUBNET_ID=$(az network vnet subnet show \
    --resource-group $RESOURCE_GROUP \
    --vnet-name $VNET_NAME \
    --name $ACA_SUBNET_NAME \
    --query id -o tsv)

az containerapp env create \
    --resource-group $RESOURCE_GROUP \
    --name $CONTAINER_ENV_NAME \
    --location $LOCATION \
    --infrastructure-subnet-resource-id $ACA_SUBNET_ID \
    --logs-workspace-id $LOG_ANALYTICS_ID \
    --logs-workspace-key $LOG_ANALYTICS_KEY \
    --output none

log_success "Container Apps Environment created"

#######################################
# Step 9: Create Container App
#######################################
log_info "Creating Container App: $CONTAINER_APP_NAME"

# Create the container app with system-assigned managed identity
az containerapp create \
    --resource-group $RESOURCE_GROUP \
    --name $CONTAINER_APP_NAME \
    --environment $CONTAINER_ENV_NAME \
    --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
    --target-port 80 \
    --ingress external \
    --min-replicas 1 \
    --max-replicas 5 \
    --cpu 0.5 \
    --memory 1.0Gi \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_USERNAME \
    --registry-password $ACR_PASSWORD \
    --system-assigned \
    --output none

log_success "Container App created with quickstart image"

# Get the managed identity principal ID
MANAGED_IDENTITY_PRINCIPAL_ID=$(az containerapp show \
    --resource-group $RESOURCE_GROUP \
    --name $CONTAINER_APP_NAME \
    --query identity.principalId -o tsv)

log_info "Managed Identity Principal ID: $MANAGED_IDENTITY_PRINCIPAL_ID"

# Grant Key Vault access to managed identity
log_info "Granting Key Vault access to Container App managed identity"
az keyvault set-policy \
    --name $KEYVAULT_NAME \
    --object-id $MANAGED_IDENTITY_PRINCIPAL_ID \
    --secret-permissions get list \
    --output none

log_success "Key Vault access granted"

# Grant App Configuration access to managed identity
log_info "Granting App Configuration access to Container App managed identity"

APP_CONFIG_ID=$(az appconfig show \
    --resource-group $RESOURCE_GROUP \
    --name $APP_CONFIG_NAME \
    --query id -o tsv)

az role assignment create \
    --assignee $MANAGED_IDENTITY_PRINCIPAL_ID \
    --role "App Configuration Data Reader" \
    --scope $APP_CONFIG_ID \
    --output none

log_success "App Configuration access granted"

# Update Container App with App Configuration endpoint
log_info "Configuring Container App to use App Configuration"

az containerapp update \
    --resource-group $RESOURCE_GROUP \
    --name $CONTAINER_APP_NAME \
    --set-env-vars "AppConfigEndpoint=$APP_CONFIG_ENDPOINT" \
    --output none

log_success "Container App configured with App Configuration endpoint"

# Get Container App URL
CONTAINER_APP_URL=$(az containerapp show \
    --resource-group $RESOURCE_GROUP \
    --name $CONTAINER_APP_NAME \
    --query properties.configuration.ingress.fqdn -o tsv)

log_success "Container App URL: https://$CONTAINER_APP_URL"

#######################################
# Step 10: Store ACR credentials in Key Vault
#######################################
log_info "Storing ACR credentials in Key Vault"

az keyvault secret set \
    --vault-name $KEYVAULT_NAME \
    --name "AcrLoginServer" \
    --value "$ACR_LOGIN_SERVER" \
    --output none

az keyvault secret set \
    --vault-name $KEYVAULT_NAME \
    --name "AcrUsername" \
    --value "$ACR_USERNAME" \
    --output none

az keyvault secret set \
    --vault-name $KEYVAULT_NAME \
    --name "AcrPassword" \
    --value "$ACR_PASSWORD" \
    --output none

log_success "ACR credentials stored in Key Vault"

#######################################
# Step 11: Configure Health Probes
#######################################
log_info "Configuring health probes for Container App"

# Note: Health probes will be configured when you deploy your actual application
# The current quickstart image doesn't have custom health endpoints
log_warning "Health probes should be configured after deploying your application with /health endpoints"

#######################################
# Deployment Summary
#######################################
echo ""
echo "========================================="
echo "  DEPLOYMENT COMPLETED SUCCESSFULLY!"
echo "========================================="
echo ""
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo ""
echo "--- Networking ---"
echo "Virtual Network: $VNET_NAME"
echo "ACA Subnet: $ACA_SUBNET_NAME (10.0.0.0/21)"
echo "Data Subnet: $DATA_SUBNET_NAME (10.0.8.0/24)"
echo ""
echo "--- Container Infrastructure ---"
echo "Container Registry: $ACR_LOGIN_SERVER"
echo "Container App Environment: $CONTAINER_ENV_NAME"
echo "Container App: $CONTAINER_APP_NAME"
echo "Container App URL: https://$CONTAINER_APP_URL"
echo ""
echo "--- Data Services ---"
echo "SQL Server: $SQL_SERVER_NAME.database.windows.net"
echo "SQL Database: $SQL_DB_NAME"
echo "SQL Admin User: $SQL_ADMIN_USER"
echo "Redis Cache: $REDIS_NAME.redis.cache.windows.net"
echo ""
echo "--- Security ---"
echo "Key Vault: $KEYVAULT_NAME"
echo "App Configuration: $APP_CONFIG_NAME"
echo "App Config Endpoint: $APP_CONFIG_ENDPOINT"
echo "Managed Identity ID: $MANAGED_IDENTITY_PRINCIPAL_ID"
echo ""
echo "--- Secrets Stored in Key Vault ---"
echo "- SqlConnectionString"
echo "- RedisConnectionString"
echo "- AcrLoginServer"
echo "- AcrUsername"
echo "- AcrPassword"
echo ""
echo "--- Configuration in App Configuration ---"
echo "- ApplicationSettings:MaxUploadSizeMB = 100"
echo "- ApplicationSettings:SessionTimeoutMinutes = 30"
echo "- ApplicationSettings:EnableSwagger = true"
echo "- ApplicationSettings:MaintenanceMode = false"
echo "- Feature Flags: NewDashboard (disabled), BetaAccess (disabled)"
echo "- ConnectionStrings:DefaultConnection (Key Vault reference)"
echo ""
echo "========================================="
echo "  IMPORTANT: Save These Credentials!"
echo "========================================="
echo ""
echo "SQL Admin Password: $SQL_ADMIN_PASSWORD"
echo ""
echo "To retrieve secrets from Key Vault:"
echo "az keyvault secret show --vault-name $KEYVAULT_NAME --name SqlConnectionString --query value -o tsv"
echo ""
echo "Next Steps:"
echo "1. Push your Docker image to ACR: $ACR_LOGIN_SERVER"
echo "2. Update Container App to use your image"
echo "3. Configure additional settings in App Configuration (see RUNTIME-CONFIG-GUIDE.md)"
echo "4. Test runtime configuration changes without redeployment"
echo "5. Set up Azure DevOps pipeline for CI/CD"
echo ""
echo "To change configuration at runtime (NO rebuild needed):"
echo "  az appconfig kv set --name $APP_CONFIG_NAME --key 'ApplicationSettings:MaxUploadSizeMB' --value '200' --yes"
echo ""
echo "To enable a feature flag:"
echo "  az appconfig feature enable --name $APP_CONFIG_NAME --feature 'NewDashboard' --yes"
echo ""
echo "========================================="

# Save credentials to file
CREDENTIALS_FILE="deployment-credentials-$(date +%Y%m%d-%H%M%S).txt"
cat > $CREDENTIALS_FILE <<EOF
===========================================
Azure Infrastructure Deployment Credentials
===========================================
Deployment Date: $(date)
Resource Group: $RESOURCE_GROUP
Location: $LOCATION

--- Container Registry ---
Login Server: $ACR_LOGIN_SERVER
Username: $ACR_USERNAME
Password: $ACR_PASSWORD

--- SQL Database ---
Server: $SQL_SERVER_NAME.database.windows.net
Database: $SQL_DB_NAME
Admin User: $SQL_ADMIN_USER
Admin Password: $SQL_ADMIN_PASSWORD

--- Redis Cache ---
Name: $REDIS_NAME.redis.cache.windows.net
Access Key: $REDIS_ACCESS_KEY

--- Key Vault ---
Name: $KEYVAULT_NAME
URL: https://$KEYVAULT_NAME.vault.azure.net/

--- App Configuration ---
Name: $APP_CONFIG_NAME
Endpoint: $APP_CONFIG_ENDPOINT

--- Container App ---
Name: $CONTAINER_APP_NAME
URL: https://$CONTAINER_APP_URL
Managed Identity: $MANAGED_IDENTITY_PRINCIPAL_ID

--- Connection Strings (Stored in Key Vault) ---
SQL: $SQL_CONNECTION_STRING
Redis: $REDIS_CONNECTION_STRING

===========================================
IMPORTANT: Keep this file secure and delete after saving credentials elsewhere!
===========================================
EOF

log_success "Credentials saved to: $CREDENTIALS_FILE"
log_warning "IMPORTANT: Keep this file secure and delete after saving credentials elsewhere!"
