#!/bin/bash

#######################################
# Azure DevOps Setup Script
# Creates project, repo, and configures branch policies
#######################################

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

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

# Configuration
ORGANIZATION_NAME="YOUR_AZURE_DEVOPS_ORG"  # e.g., "mycompany"
PROJECT_NAME="AICalendar"
REPO_NAME="AICalendar"
AZURE_SUBSCRIPTION_NAME="YOUR_AZURE_SUBSCRIPTION_NAME"

#######################################
# Pre-flight Checks
#######################################
log_info "Starting Azure DevOps setup..."

# Check if Azure DevOps CLI extension is installed
if ! az extension list | grep -q "azure-devops"; then
    log_info "Installing Azure DevOps CLI extension..."
    az extension add --name azure-devops
fi

# Check if logged in
if ! az account show &> /dev/null; then
    log_error "Not logged in to Azure. Please run: az login"
    exit 1
fi

# Prompt for organization name if default is used
if [ "$ORGANIZATION_NAME" = "YOUR_AZURE_DEVOPS_ORG" ]; then
    read -p "Enter your Azure DevOps organization name: " ORGANIZATION_NAME
fi

# Set default organization
az devops configure --defaults organization=https://dev.azure.com/$ORGANIZATION_NAME

log_info "Using organization: $ORGANIZATION_NAME"

#######################################
# Step 1: Create Project
#######################################
log_info "Creating Azure DevOps project: $PROJECT_NAME"

# Check if project exists
if az devops project show --project $PROJECT_NAME &> /dev/null; then
    log_warning "Project $PROJECT_NAME already exists, skipping creation"
    PROJECT_ID=$(az devops project show --project $PROJECT_NAME --query id -o tsv)
else
    PROJECT_ID=$(az devops project create \
        --name $PROJECT_NAME \
        --description "AI-powered calendar application with enterprise security" \
        --visibility private \
        --source-control git \
        --query id -o tsv)

    log_success "Project created: $PROJECT_NAME"
fi

# Wait for project to be fully provisioned
log_info "Waiting for project provisioning..."
sleep 10

# Clear any default project configuration to avoid stale cache
az devops configure --defaults project=""

#######################################
# Step 2: Configure Repository
#######################################
log_info "Configuring repository..."

# Get repository ID - explicitly passing project to avoid context errors
# First try exact match
REPO_ID=$(az repos list --project $PROJECT_NAME --query "[?name=='$PROJECT_NAME'].id" -o tsv)

# If not found, just get the first repository in the project (default repo)
if [ -z "$REPO_ID" ]; then
    log_info "Default repository not found by name, fetching first available repository..."
    REPO_ID=$(az repos list --project $PROJECT_NAME --query "[0].id" -o tsv)
fi

if [ -z "$REPO_ID" ]; then
    log_error "Could not find any repository in project $PROJECT_NAME"
    exit 1
fi

log_success "Repository ID: $REPO_ID"

#######################################
# Step 3: Create Branch Policies
#######################################
log_info "Creating branch policy for main branch"

# Get main branch ref
MAIN_BRANCH_REF="refs/heads/main"

# Create branch policy configuration JSON
cat > branch-policy.json <<EOF
{
  "isEnabled": true,
  "isBlocking": true,
  "type": {
    "id": "fa4e907d-c16b-4a4c-9dfa-4906e5d171dd"
  },
  "settings": {
    "minimumApproverCount": 1,
    "creatorVoteCounts": false,
    "allowDownvotes": false,
    "resetOnSourcePush": true,
    "requireVoteOnLastIteration": true,
    "resetRejectionsOnSourcePush": false,
    "blockLastPusherVote": false,
    "requireVoteOnEachIteration": false,
    "scope": [
      {
        "repositoryId": "$REPO_ID",
        "refName": "$MAIN_BRANCH_REF",
        "matchKind": "exact"
      }
    ]
  }
}
EOF

# Apply branch policy
log_info "Applying minimum approvers policy..."
az repos policy create \
    --config branch-policy.json \
    --org https://dev.azure.com/$ORGANIZATION_NAME \
    --project $PROJECT_NAME

log_success "Branch policy created: Minimum 1 approver required"

# Create build validation policy (will be updated after pipeline creation)
log_info "Branch policies configured"

# Cleanup temp file
rm branch-policy.json

#######################################
# Step 4: Create Service Connections
#######################################
log_info "Creating service connections..."

log_warning "Service connections must be created manually in Azure DevOps UI due to security requirements"
log_info "Please create the following service connections:"
echo ""
echo "1. Azure Resource Manager Service Connection:"
echo "   - Name: Azure-ServiceConnection"
echo "   - Subscription: $AZURE_SUBSCRIPTION_NAME"
echo "   - Resource Group: aicalendar-rg"
echo ""
echo "2. Docker Registry Service Connection:"
echo "   - Name: ACR-ServiceConnection"
echo "   - Registry Type: Azure Container Registry"
echo "   - Select your ACR from deployment"
echo ""
echo "Steps:"
echo "1. Go to: https://dev.azure.com/$ORGANIZATION_NAME/$PROJECT_NAME/_settings/adminservices"
echo "2. Click 'New service connection'"
echo "3. Follow the prompts to create each connection"

#######################################
# Step 5: Create Pipeline Variables
#######################################
log_info "Creating pipeline variable group..."

# Create variable group
VARIABLE_GROUP_ID=$(az pipelines variable-group create \
    --name "aicalendar-variables" \
    --variables \
        resourceGroup="aicalendar-rg" \
        location="spaincentral" \
        containerAppName="aicalendar-api" \
        containerEnv="aicalendar-env" \
    --authorize true \
    --project $PROJECT_NAME \
    --query id -o tsv)

log_success "Variable group created: aicalendar-variables"

#######################################
# Step 6: Initialize Repository Structure
#######################################
log_info "Setting up repository structure..."

# # Clone the repository if not already in it
# REPO_URL="https://dev.azure.com/$ORGANIZATION_NAME/$PROJECT_NAME/_git/$PROJECT_NAME"

# if [ ! -d ".git" ]; then
#     log_info "Cloning repository..."
#     git clone $REPO_URL temp-repo
#     cd temp-repo
# else
#     log_info "Already in a git repository"
# fi

# Create branch structure
log_info "Creating branch structure..."

# Ensure we're on main
git checkout -B main 2>/dev/null || git checkout main

# Create dev branch
git checkout -b dev 2>/dev/null || git checkout dev

# Push branches
git push origin main --set-upstream 2>/dev/null || true
git push origin dev --set-upstream 2>/dev/null || true

log_success "Branch structure created: main (protected), dev"

#######################################
# Completion Summary
#######################################
echo ""
echo "========================================="
echo "  AZURE DEVOPS SETUP COMPLETED!"
echo "========================================="
echo ""
echo "Organization: $ORGANIZATION_NAME"
echo "Project: $PROJECT_NAME"
echo "Repository: $REPO_NAME"
echo ""
echo "--- Branch Structure ---"
echo "main - Protected branch (requires 1 approval)"
echo "dev  - Development branch (no restrictions)"
echo ""
echo "--- Repository URL ---"
echo "HTTPS: $REPO_URL"
echo "SSH: git@ssh.dev.azure.com:v3/$ORGANIZATION_NAME/$PROJECT_NAME/$PROJECT_NAME"
echo ""
echo "--- Next Steps ---"
echo "1. Create service connections (see instructions above)"
echo "2. Create azure-pipelines.yml in your repository"
echo "3. Push your application code"
echo "4. Create and run the pipeline"
echo ""
echo "--- Git Workflow ---"
echo "Development:"
echo "  git checkout dev"
echo "  git commit -m 'Your changes'"
echo "  git push origin dev"
echo ""
echo "Production:"
echo "  Create Pull Request: dev -> main"
echo "  Get 1 approval"
echo "  Complete PR to trigger production deployment"
echo ""
echo "========================================="
