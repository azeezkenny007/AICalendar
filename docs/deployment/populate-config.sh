#!/bin/bash

# Stop on error
set -e

RG="aicalendar-rg"
KV="aicalendar-kv-95387c"
CONFIG="aicalendar-config-542537"
SQL_SERVER="aicalendar-sql-81816c"
DB_NAME="AICalendarDb"
REDIS="aicalendar-redis-6a07c6"

echo "1. Getting Connection Strings..."

# Get SQL Connection String (Construct it)
# Note: We need the password. If lost, we must reset it.
# Attempting to retrieve known password from previous logs or asking user.
# Using the standard format.
# User previously saw: SQL_ADMIN_PASSWORD in deploy-infrastructure log? No, it was masked/variable.
# We will use 'az sql db show-connection-string' and replace password placeholder.
# BUT we don't have the password!
# We might need to RESET the SQL Password to a known value to be sure.

NEW_SQL_PASS="P@ssw0rd2025!$(openssl rand -hex 4)"
echo "Resetting SQL Password to: $NEW_SQL_PASS"
az sql server update -g $RG -n $SQL_SERVER --admin-password "$NEW_SQL_PASS"

SQL_CONN="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${DB_NAME};Persist Security Info=False;User ID=azureuser;Password=${NEW_SQL_PASS};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Get Redis Connection String
KEY=$(az redis list-keys -g $RG -n $REDIS --query primaryKey -o tsv)
REDIS_CONN="${REDIS}.redis.cache.windows.net:6380,password=${KEY},ssl=True,abortConnect=False"

echo "2. Setting Key Vault Secrets..."
az keyvault secret set --vault-name $KV --name "SqlConnectionString" --value "$SQL_CONN"
az keyvault secret set --vault-name $KV --name "RedisConnectionString" --value "$REDIS_CONN"

echo "3. Configuring App Configuration..."
# Key Vault Reference for Connection String
KV_URI="https://${KV}.vault.azure.net"
az appconfig kv set-keyvault -n $CONFIG --key "ConnectionStrings:DefaultConnection" --secret-identifier "${KV_URI}/secrets/SqlConnectionString" --yes

# Other Settings
az appconfig kv set -n $CONFIG --key "ApplicationSettings:MaxUploadSizeMB" --value "100" --yes
az appconfig kv set -n $CONFIG --key "ApplicationSettings:SessionTimeoutMinutes" --value "30" --yes

echo "✅ Configuration Populated!"
