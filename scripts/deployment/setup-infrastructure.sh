#!/bin/bash

# ==============================================================================
# Azure Deployment Script for AICalendar (VNet Integrated)
# Deploys: VNet, ACA (Internal/External), Azure SQL, Azure Redis
# Usage: Run this in Azure Cloud Shell (shell.azure.com) if you don't have CLI locally.
# ==============================================================================

# Exit on error
set -e

# --- Configuration -----------------------------------------------------------
PREFIX="aicalendar"
LOCATION="southafricanorth" # Primary: South Africa North. Alternative: "westeurope" (often better latency for Lagos via MainOne cable)
RESOURCE_GROUP="${PREFIX}-rg"
VNET_NAME="${PREFIX}-vnet"
SUBNET_ACA_NAME="aca-subnet"
ACR_NAME="${PREFIX}acr${RANDOM}"
SQL_SERVER_NAME="${PREFIX}sql${RANDOM}"
SQL_DB_NAME="AICalendarDb"
REDIS_NAME="${PREFIX}redis${RANDOM}"
ACA_ENV_NAME="${PREFIX}-env"
APP_NAME="${PREFIX}-api"

# Passwords (In production, use Key Vault!)
SQL_ADMIN_USER="azureuser"
SQL_ADMIN_PASS="P@ssw0rd${RANDOM}${RANDOM}!"

echo "🚀 Starting Secure Deployment for $PREFIX..."
echo "📍 Location: $LOCATION"
echo "📦 Resource Group: $RESOURCE_GROUP"

# 1. Create Resource Group
echo "Creating Resource Group..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# 2. Network Infrastructure (VNet & Subnets)
echo "Creating Virtual Network and Subnets..."
# Create VNet 10.0.0.0/16
az network vnet create \
    --name $VNET_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --address-prefix 10.0.0.0/16

# Create Subnet for Container Apps (Needs /23 minimum usually, using /21 for safety)
# This subnet is delegated to Microsoft.App/environments
az network vnet subnet create \
    --name $SUBNET_ACA_NAME \
    --resource-group $RESOURCE_GROUP \
    --vnet-name $VNET_NAME \
    --address-prefix 10.0.0.0/21 \
    --delegations Microsoft.App/environments

ACA_SUBNET_ID=$(az network vnet subnet show --resource-group $RESOURCE_GROUP --vnet-name $VNET_NAME --name $SUBNET_ACA_NAME --query id --output tsv)

# 3. Create Azure Container Registry (ACR)
echo "Creating Container Registry..."
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Basic --admin-enabled true

# Get ACR Credentials
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username --output tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query passwords[0].value --output tsv)

# 4. Build and Push (Assumes you are in the source root!)
# If running in Cloud Shell, you need to upload the code or git clone first.
# If no source present, this step will fail.
if [ -f "Dockerfile" ]; then
    echo "Building and Pushing Image to ACR..."
    az acr build --registry $ACR_NAME --image $APP_NAME:latest .
else
    echo "⚠️ Dockerfile not found. Skipping build step. Ensure $APP_NAME:latest exists in ACR manually."
fi

# 5. Create Azure SQL Database
echo "Creating SQL Server..."
az sql server create \
    --name $SQL_SERVER_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --admin-user $SQL_ADMIN_USER \
    --admin-password $SQL_ADMIN_PASS

echo "Creating SQL Database..."
az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name $SQL_DB_NAME \
    --service-objective S0

# Construct SQL Connection String
SQL_CONN_STR="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Initial Catalog=$SQL_DB_NAME;Persist Security Info=False;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASS;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# 6. Create Azure Redis Cache
echo "Creating Redis Cache..."
az redis create \
    --resource-group $RESOURCE_GROUP \
    --name $REDIS_NAME \
    --location $LOCATION \
    --sku Basic \
    --vm-size C0

REDIS_KEY=$(az redis list-keys --resource-group $RESOURCE_GROUP --name $REDIS_NAME --query primaryKey --output tsv)
REDIS_HOST=$(az redis show --resource-group $RESOURCE_GROUP --name $REDIS_NAME --query hostName --output tsv)
REDIS_CONN_STR="$REDIS_HOST:6380,password=$REDIS_KEY,ssl=True,abortConnect=False"

# 7. Create Container Apps Environment (VNet Integrated)
echo "Creating Container Apps Environment (VNet Integrated)..."
# We need a Log Analytics Workspace first
az monitor log-analytics workspace create \
    --resource-group $RESOURCE_GROUP \
    --workspace-name "${PREFIX}-logs"

LOG_WS_ID=$(az monitor log-analytics workspace show --resource-group $RESOURCE_GROUP --workspace-name "${PREFIX}-logs" --query customerId --output tsv)
LOG_WS_KEY=$(az monitor log-analytics workspace get-shared-keys --resource-group $RESOURCE_GROUP --workspace-name "${PREFIX}-logs" --query primarySharedKey --output tsv)

az containerapp env create \
    --name $ACA_ENV_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --logs-workspace-id $LOG_WS_ID \
    --logs-workspace-key $LOG_WS_KEY \
    --infrastructure-subnet-resource-id $ACA_SUBNET_ID

# 8. Deploy Container App
echo "Deploying Container App..."
az containerapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --environment $ACA_ENV_NAME \
    --image "$ACR_NAME.azurecr.io/$APP_NAME:latest" \
    --registry-server "$ACR_NAME.azurecr.io" \
    --registry-username $ACR_USERNAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress 'external' \
    --min-replicas 1 \
    --max-replicas 10 \
    --cpu 0.5 --memory 1.0Gi \
    --env-vars \
        ASPNETCORE_ENVIRONMENT=Production \
        ConnectionStrings__DefaultConnection="$SQL_CONN_STR" \
        ConnectionStrings__RedisConnection="$REDIS_CONN_STR"

echo "✅ Secure Deployment Complete!"
echo "API URL: https://$(az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn --output tsv)"
