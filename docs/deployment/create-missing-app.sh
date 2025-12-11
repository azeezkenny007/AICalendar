#!/bin/bash

RG="aicalendar-rg"
ENV_NAME="aicalendar-env"
APP_NAME="aicalendar-api"
LOCATION="spaincentral"

# 1. Check if Environment exists
echo "Checking for existing environment '$ENV_NAME'..."
ENV_ID=$(az containerapp env show --name $ENV_NAME --resource-group $RG --query id -o tsv 2>/dev/null)

if [ -n "$ENV_ID" ]; then
    echo "✅ Found existing environment: $ENV_NAME"
else
    echo "⚠️ Environment not found. Creating it..."
    # Get Log Analytics Workspace ID (required for env)
    LOG_WS=$(az monitor log-analytics workspace list -g $RG --query "[0].customerId" -o tsv)
    LOG_KEY=$(az monitor log-analytics workspace get-shared-keys -g $RG -n $(az monitor log-analytics workspace list -g $RG --query "[0].name" -o tsv) --query primarySharedKey -o tsv)

    az containerapp env create \
      --name $ENV_NAME \
      --resource-group $RG \
      --location $LOCATION \
      --logs-workspace-id $LOG_WS \
      --logs-workspace-key $LOG_KEY
fi

# 2. Get ACR Server
ACR_SERVER=$(az acr list -g $RG --query "[0].loginServer" -o tsv)

# 3. Create the App
echo "Creating/Updating App '$APP_NAME'..."
az containerapp create \
  --name $APP_NAME \
  --resource-group $RG \
  --environment $ENV_NAME \
  --image mcr.microsoft.com/k8se/quickstart:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server $ACR_SERVER \
  --query properties.configuration.ingress.fqdn

echo "✅ Ready! Run the pipeline now."
