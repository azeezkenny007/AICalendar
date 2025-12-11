#!/bin/bash

RG="aicalendar-rg"
APP_NAME="aicalendar-api"
APP_CONFIG_NAME=$(az appconfig list -g $RG --query "[0].name" -o tsv)
KEYVAULT_NAME=$(az keyvault list -g $RG --query "[0].name" -o tsv)

echo "1. Enabling System-Assigned Identity..."
az containerapp identity assign -n $APP_NAME -g $RG --system-assigned

echo "2. Getting Principal ID..."
PRINCIPAL_ID=$(az containerapp show -n $APP_NAME -g $RG --query identity.principalId -o tsv)
echo "Principal ID: $PRINCIPAL_ID"

echo "3. Granting 'App Configuration Data Reader' role..."
APP_CONFIG_ID=$(az appconfig show -n $APP_CONFIG_NAME -g $RG --query id -o tsv)
az role assignment create \
    --assignee $PRINCIPAL_ID \
    --role "App Configuration Data Reader" \
    --scope $APP_CONFIG_ID

echo "4. Granting Key Vault Access (Get, List Secrets)..."
az keyvault set-policy \
    --name $KEYVAULT_NAME \
    --object-id $PRINCIPAL_ID \
    --secret-permissions get list

echo "✅ Permissions Granted! The app should restart and work now."
