# Complete Deployment Guide - Step by Step

## 🎯 Goal
Deploy your AICalendar app to Azure with automated CI/CD (no manual Docker builds!)

---

## ✅ STEP 1: Wait for Redis (Currently Running)

**What:** Redis Cache is being created in Azure
**How long:** ~15-20 minutes total (started ~13 minutes ago)
**What to do:** Just wait. You can check status:

```bash
az redis show --resource-group aicalendar-rg --name aicalendar-redis-6a07c6 --query provisioningState -o tsv
```

**When it says "Succeeded", move to Step 2.**

---

## ✅ STEP 2: Complete Infrastructure Deployment

**What:** Finish creating all Azure resources
**How long:** ~5-10 minutes

```bash
cd c:\Users\Purple-Serve\Desktop\AICalendar\docs\deployment
./deploy-infrastructure.sh
```

**When prompted, type:** `yes`

**What this creates:**
- Redis Private Endpoint
- Container Apps Environment
- Container App (with placeholder image)
- Saves all credentials to a file

**When you see "Deployment completed successfully", move to Step 3.**

---

## ✅ STEP 3: Set Up Azure DevOps Project

**What:** Create Azure DevOps project and repository
**How long:** ~5 minutes

### 3.1 Go to Azure DevOps

Open browser: https://dev.azure.com

**Sign in** with your Azure account (same as Azure Portal)

### 3.2 Create Organization (if needed)

If you don't have an organization:
- Click **"+ New organization"**
- Name it anything (e.g., `YourName-DevOps`)
- Click **Continue**

### 3.3 Create Project

- Click **"+ New project"**
- **Project name:** `AICalendar`
- **Visibility:** Private
- Click **Create**

**When you see the project dashboard, move to Step 4.**

---

## ✅ STEP 4: Get Repository URL

**What:** Get the URL where you'll push your code

### In Azure DevOps:

1. Click **Repos** (left menu)
2. Click **Files**
3. Click **Clone** (top right)
4. Copy the **HTTPS** URL

It looks like:
```
https://dev.azure.com/YOUR_ORG/AICalendar/_git/AICalendar
```

**Save this URL! You'll need it in Step 6.**

---

## ✅ STEP 5: Create Service Connections

**What:** Give Azure DevOps permission to access your Azure resources
**How long:** ~5 minutes

### 5.1 Go to Service Connections

In Azure DevOps:
1. Click **Project Settings** (bottom left)
2. Click **Service connections** (under Pipelines)

### 5.2 Create Azure Service Connection

1. Click **New service connection**
2. Select **Azure Resource Manager** → Click **Next**
3. Select **Service principal (automatic)** → Click **Next**
4. Fill in:
   - **Scope level:** Subscription
   - **Subscription:** Azure for Students
   - **Resource group:** aicalendar-rg
   - **Service connection name:** `Azure-ServiceConnection`
   - ✅ Check **Grant access permission to all pipelines**
5. Click **Save**

### 5.3 Create ACR Service Connection

1. Click **New service connection** again
2. Select **Docker Registry** → Click **Next**
3. Select **Azure Container Registry**
4. Fill in:
   - **Subscription:** Azure for Students
   - **Azure container registry:** Select your ACR (starts with `aicalendaracr...`)
   - **Service connection name:** `ACR-ServiceConnection`
   - ✅ Check **Grant access permission to all pipelines**
5. Click **Save**

**When you have 2 service connections, move to Step 6.**

---

## ✅ STEP 6: Push Code to Azure Repos

**What:** Upload your code to Azure DevOps
**How long:** ~2 minutes

```bash
cd c:\Users\Purple-Serve\Desktop\AICalendar

# Add Azure Repos as remote (use URL from Step 4)
git remote add azure https://dev.azure.com/YOUR_ORG/AICalendar/_git/AICalendar

# Push your code
git push azure main
```

**If prompted for credentials:**
- Username: Your email
- Password: Use a Personal Access Token (PAT)

**To create PAT:**
1. Azure DevOps → User Settings (top right) → Personal access tokens
2. Click **New Token**
3. Name: `Git Access`
4. Scopes: **Code (Read & Write)**
5. Click **Create**
6. Copy the token and use it as password

**When push succeeds, move to Step 7.**

---

## ✅ STEP 7: Create Variable Group

**What:** Set up configuration variables for the pipeline
**How long:** ~2 minutes

### In Azure DevOps:

1. Click **Pipelines** (left menu)
2. Click **Library**
3. Click **+ Variable group**
4. Fill in:
   - **Variable group name:** `aicalendar-variables`
   - Click **+ Add** and add these variables:

   | Name | Value |
   |------|-------|
   | `resourceGroup` | `aicalendar-rg` |
   | `location` | `spaincentral` |
   | `containerAppName` | `aicalendar-api` |
   | `containerEnv` | `aicalendar-env` |

5. ✅ Check **Allow access to all pipelines**
6. Click **Save**

**When saved, move to Step 8.**

---

## ✅ STEP 8: Create Pipeline

**What:** Set up automated build and deployment
**How long:** ~3 minutes to create, ~10-15 minutes to run

### In Azure DevOps:

1. Click **Pipelines** → **Pipelines**
2. Click **New pipeline** (or **Create Pipeline**)
3. **Where is your code?** → Select **Azure Repos Git**
4. **Select a repository** → Select **AICalendar**
5. **Configure your pipeline** → Select **Existing Azure Pipelines YAML file**
6. **Select an existing YAML file:**
   - **Branch:** main
   - **Path:** `/docs/deployment/azure-pipelines.yml`
   - Click **Continue**
7. **Review** the pipeline
8. Click **Run**

**The pipeline will now:**
1. ✅ Build your Docker image (in Azure, not locally!)
2. ✅ Push to Azure Container Registry
3. ✅ Deploy to Container App
4. ✅ Configure App Configuration
5. ✅ Run health checks

**Watch the pipeline run! When it shows "Success", move to Step 9.**

---

## ✅ STEP 9: Test Your App!

**What:** Verify everything works
**How long:** ~2 minutes

```bash
# Get your app URL
az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-rg \
  --query properties.configuration.ingress.fqdn -o tsv
```

**Open in browser:**
- **Swagger:** `https://YOUR_APP_URL/swagger`
- **Config:** `https://YOUR_APP_URL/api/config/current`
- **Health:** `https://YOUR_APP_URL/api/config/health`

**If you see Swagger UI, YOU'RE DONE!** 🎉

---

## 🎯 Future Deployments (After Initial Setup)

Now deploying is super easy:

```bash
# 1. Make changes to your code
# ...

# 2. Commit and push
git add .
git commit -m "Added new feature"
git push azure main

# 3. Done! Pipeline auto-deploys in ~10 minutes
```

---

## 📋 Quick Checklist

```
⏳ Step 1: Wait for Redis
⬜ Step 2: ./deploy-infrastructure.sh
⬜ Step 3: Create Azure DevOps project
⬜ Step 4: Get repository URL
⬜ Step 5: Create service connections (2)
⬜ Step 6: git push azure main
⬜ Step 7: Create variable group
⬜ Step 8: Create pipeline
⬜ Step 9: Test your app
```

---

## ❓ Stuck? Common Issues

### "git push" asks for credentials
**Solution:** Create Personal Access Token (see Step 6)

### Pipeline fails at "Push to ACR"
**Solution:** Check ACR service connection in Step 5.2

### Pipeline fails at "Update Container App"
**Solution:** Check Azure service connection in Step 5.1

### Can't find ACR in service connection
**Solution:** Run this to get ACR name:
```bash
az acr list --resource-group aicalendar-rg --query "[0].name" -o tsv
```

---

**Start with Step 1 and go in order. Don't skip steps!** 🚀
