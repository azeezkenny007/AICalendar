 # Azure Manual Deployment Guide (GUI)

This guide walks you through deploying the AICalendar application using the Azure Portal.

## 1. Create a Resource Group
1. Search for **Resource groups**.
2. Click **+ Create**.
3. **Region**: Select **France Central** (Recommended) or **Poland Central**.
   *   *Reason*: Your subscription restricts deployment to these specific regions. Do not pick West Europe or South Africa.
4. **Name**: `aicalendar-rg` (or `aicalendar-rg-fr`).
5. Click **Review + create**, then **Create**.

---

## 2. Set Up Networking (Virtual Network)
*Azure Container Apps requires a specific subnet setup.*

1. Search for **Virtual networks**.
2. Click **+ Create**.
3. **Resource Group**: `aicalendar-rg`.
4. **Name**: `aicalendar-vnet`.
5. Use default IP addresses (e.g., `10.0.0.0/16`).
6. Go to the **Security** tab (skip if not relevant), then **IP Addresses**.
7. Delete the default subnet if you want, or edit it. We need a specific subnet for Container Apps.
8. Click **+ Add subnet**:
   - **Name**: `aca-subnet`
   - **Subnet address range**: `10.0.0.0/21` (Must be /23 or larger)
   - **Subnet purpose**: Select **Default**.
     *   *Note: Since "Container Apps" is not in the "Subnet purpose" dropdown (as seen in your screenshot), you MUST choose "Default" here and then perform the manual "Delegate Subnet" step below.*
   - **NAT Gateway**: None (unless you need static outbound IP)
   - **Service Endpoints**: Select `Microsoft.Sql` and `Microsoft.Web` (optional, for security).

### Reference: Subnet Fields Explained
Since you are looking at the form, here is what every option actually does:

| Field | What it does | Best Practice / Recommendation |
| :--- | :--- | :--- |
| **Subnet purpose** | A template that auto-fills settings. | **Default** (since we are configuring manually). |
| **Name** | Just a label. | Use **`data-subnet`** for this one. |
| **IPv4 address range** | The "seating capacity" (IPs). | **Critical**: Must NOT overlap with your first subnet. If `aca-subnet` was `10.0.0.0/21`, start this one at **`10.0.8.0`**. Size `/24` (256 IPs) is plenty for data. |
| **IPv6** | Next-gen IP addressing (long alphanumeric). | **Unchecked**. Most Azure PaaS services fully support IPv4, and mixing dual-stack (IPv4+IPv6) adds huge complexity for zero benefit in this project. |
| **Private Subnet** | **Security**. Blocks all outbound internet access by default. | **Enable** for high security (Enterprise), but ensure you don't need to download updates directly. **Leave unchecked** if unsure. |
| **NAT Gateway** | **Connectivity**. Static Public IP (~$30/mo). | **None**. Waste of money unless you explicitly need a Static IP for the API. Useless for Data. |
| **Network Security Group** | **Firewall**. Rules for "Allow Port 80, Block Port 22". | **Create New** (or select existing). Always good to have a firewall wrapper, even if empty initially. |
| **Route Table** | **Traffic Control**. Forces traffic to go through a specific appliance (like a corporate firewall). | **None**. (Unless your IT team enforces it). |
| **Service Endpoints** | **Identity Badge**. Allows Azure services (like SQL) to see "This request came from Subnet X". Enables firewall rules. | **None**. (Because we are using Private Endpoints, which are even better). |
| **Subnet Delegation** | **Handover**. Officially gives the subnet to an Azure Service (like Container Apps) to manage. | **Microsoft.App/environments** (For `aca-subnet` ONLY). **None** (For `data-subnet`). |
| **Network Policy** | **Control**. Decides if NSGs apply to Private Endpoints. | **Default**. (Leave as is). |

### Configuration 1: API Subnet (`aca-subnet`)
*Fill this in first:*

*   **Subnet purpose**: `Default`
*   **Name**: `aca-subnet`
*   **Starting address**: `10.0.0.0`
*   **Size**: `/21` (2048 addresses) - *Crucial for scaling!*
*   **NAT Gateway**: `None`
*   **Network Security Group**: `None` (Let Container Apps manage traffic for now).
*   **Route Table**: `None`
*   **Service Endpoints**: `None` (Not needed for Private Endpoints).
*   **Subnet Delegation** (Most Important):
    *   **Service**: `Microsoft.App/environments`
    *   *Note: You must select this so ACA can control the IPs.*

### Why "None" for the API Subnet NSG?
Use "None" for the Container App subnet to prevent "Deployment Failed" errors.

1.  **ACA is a "Managed Service"**:
    *   Even though the code runs in your subnet, Microsoft needs to "reach into" that subnet to manage it (start/stop containers, apply updates, scale up/down).
    *   If you create a custom NSG and accidentally block these management connections, your deployment will fail or get stuck.
2.  **How ACA protects itself**:
    *   **Ingress Controller (Envoy)**: ACA comes with a built-in "Application Firewall" (Envoy) that sits in front of your code.
    *   **Public/Private Toggle**: You control access at the **App Level**. When you create the Container App later, you will flip a switch called "Ingress" to either "External" (Public) or "Internal" (VNet Only). This is safer and easier than messing with subnet rules.

**Summary**: Let the Platform (ACA) handle the complexity. Only add a subnet NSG if your security team explicitly forces you to.

### Critical Question: Why do I need a Subnet for the API?
You might ask: *"If I'm not putting a firewall (NSG) on it, why not just let it float in the cloud?"*

**The Answer: Connectivity.**

1.  **Access to your "Secret" Data**:
    *   Remember: You put your SQL and Redis in a **Private Endpoint** (the `data-subnet`). This completely blocks the public internet.
    *   **If your API is NOT in a subnet**: It is "on the internet". It is locked out. It cannot reach your database. The app will crash.
    *   **If your API IS in a subnet**: It automatically gets a "Key to the House" (a private IP in the VNet). It can now talk directly to the SQL/Redis private IPs.
2.  **It's the "Bridge"**: The subnet is the physical bridge that connects your code to your data. Without it, they are on two different islands.

### Configuration 2: Data Subnet (`data-subnet`)
*Fill this in second (Enterprise Only):*

*   **Subnet purpose**: `Default`
*   **Name**: `data-subnet`
*   **Starting address**: `10.0.8.0` (Must not overlap with above!)
*   **Size**: `/24` (256 addresses)
*   **Private Subnet**: **Unchecked**.
    *   *Why?* See "Deep Dive" below.
*   **NAT Gateway**: `None`
*   **Network Security Group**: `Create new` -> `data-nsg`
*   **Subnet Delegation**: `None` (Crucial!)

### Deep Dive: What is a "Private Subnet"?
The checkbox **"Enable private subnet (no default outbound access)"** is a specific security toggle.

*   **What it does**: It creates an invisible wall that blocks all **outbound** traffic.
    *   *Normal Subnet*: Your app can send a request to `google.com` (Outbound) and get a reply.
    *   *Private Subnet*: Your app tries to call `google.com` and the connection is dropped instantly.
*   **The Catch**: If you enable this, you **must** buy a **NAT Gateway** (which costs ~$30/month) if you want your app to ever reach the internet (e.g., to call a 3rd party API, send an email, or download a software update).
*   **Our Recommendation**: **Leave it Unchecked**.
    *   We want to save costs.
    *   Your Database (in `data-subnet`) doesn't make outbound calls anyway, so it doesn't matter much.
    *   Your App (in `aca-subnet`) probably *needs* outbound access to function (e.g., to reach KeyVault or external APIs).

### Deep Dive: Security Options (NAT, NSG, Routes)
You asked about the "Security" section. Here is the plain English translation:

#### 1. NAT Gateway (Outbound Traffic)
*   **Verdict**: **NO! (Save your money)**
    *   **Data Subnet**: Completely useless. Private Endpoints (SQL/Redis) don't browse the internet.
    *   **API Subnet**: Expensive (~$30/month). Only needed if you require a **Static IP** for external partners.
*   **What is it?** It is a "Funnel" that forces all outbound traffic to use ONE static IP.

#### 2. Network Security Group / NSG (The Firewall)
*   **What is it?** It is a "Bouncer". It is a list of rules like "Allow traffic on Port 80 (Web)" or "Block Traffic on Port 22 (SSH)".
*   **Why use it?** It is the standard way to protect a subnet. Even if your Database has a weak password, an NSG can block anyone from even trying to guess it from the internet.
*   **Verdict**: **Create New**. It's free and good practice. Name it `api-nsg` (for app subnet) or `data-nsg` (for data subnet). You can leave the rules default for now.

#### 3. Route Table (Traffic Control)
*   **What is it?** Modifications to the GPS. Normally, traffic takes the fastest path. A Route Table forces traffic to take a detour (e.g., "Go through my corporate firewall appliance first").
*   **Verdict**: **None**. This is only for complex enterprise networks where all traffic must be scanned by a central security appliance.



### Important: Delegate Subnet
1. Go to the newly created `aicalendar-vnet` resource.
2. Click **Subnets** in the left menu.
3. Click on `aca-subnet`.
4. Under **Subnet delegation**, select `Microsoft.App/environments`.
5. Click **Save**.

### Understanding Subnet Addresses (CIDR)
You might be wondering about the "Subnet address range" (e.g., `/21` vs `/23`):

*   **What is it?**
    It defines the *pool of private IP addresses* available for your application.
    *   `/24` = 256 addresses (Standard small network)
    *   `/23` = 512 addresses (Double)
    *   `/21` = 2,048 addresses (Large)

*   **Who uses these IPs?**
    Azure Container Apps is built on Kubernetes. Every time you scale up (add a new "replica" or copy of your app), it needs its own private IP address. Additionally, the underlying system ("nodes") and load balancers also reserve IPs from this pool.

*   **Why does usage matter?**
    If you pick a pool that is too small (like `/24`), you might run out of IP addresses if your app scales up to handle heavy traffic. Azure requires a minimum of `/23` (512 IPs) to ensure you never crash due to "IP exhaustion."

*   **Summary**:
    Think of the Subnet Address as the **seating capacity** of your restaurant. You need enough "tables" (IPs) for all your "guests" (app instances) and "staff" (infrastructure) to sit comfortably.

*   **When does it become "too much"?**
    Technically, private IPs are free, so a huge subnet won't cost you extra money directly. However, it "costs" you space in your Virtual Network.
    *   If your VNet is `10.0.0.0/16` (65,536 total IPs) and you assign *all* of it to this one subnet, you won't have any room left to create other subnets for things like your Database, Redis, or a VPN Gateway later.
    *   **Rule of Thumb**: Allocate what you need plus a safety buffer (like `/21` or `/23`), but don't eat up your entire network space unnecessarily.

    *   **Rule of Thumb**: Allocate what you need plus a safety buffer (like `/21` or `/23`), but don't eat up your entire network space unnecessarily.

### Can I put everything (DB, Redis, App) in one Subnet?
**No**, and here is why:

1.  **Container Apps are Picky**: The subnet for Azure Container Apps must be **Delegated** (dedicated) only to them. You literally *cannot* put an Azure SQL Database or Redis Cache into that specific subnet. Azure won't let you.
2.  **The VNet is the "House"**:
    *   Think of the **Virtual Network (VNet)** as your **House**.
    *   Think of **Subnets** as **Rooms** in that house.
    *   **Room A (App Subnet)**: Your Container App lives here.
    *   **Room B (Data Subnet)**: Your SQL and Redis Private Endpoints live here.
3.  **Connectivity**:
    *   Even though they are in different rooms (subnets), because they are in the **same house (VNet)**, they can talk to each other freely using private IPs!
    *   They typically don't need to go out to the "street" (Internet) to talk.

**Conclusion**: Resources in the *same VNet* (even different subnets) have full private access to each other by default. You don't need to cram them into one subnet.

### Do I need a separate Subnet for EVERY service?
**No**. You typically group them by **"Tier"** or function:

1.  **Frontend/App Tier** (`aca-subnet`):
    *   **Who lives here?** All your Container Apps (API, Background Worker, Dashboard).
    *   **Why?** They all share the same "compute" needs and Kubernetes environment.
2.  **Data Tier** (Optional `data-subnet`):
    *   **Who lives here?** Your SQL Database, Redis Cache, and KeyVault (via "Private Endpoints").
    *   **Why?** You can put all your "backend" data services into *one* shared subnet using Private Endpoints. You do **not** need a separate subnet for SQL and another for Redis.
    *   *Note: If you use "Service Endpoints" (the simpler setup in step 2), you don't even need this second subnet! Your DB and Redis just live in Azure PaaS space but are firewalled to permit your VNet.*

**Summary**: You usually only need **2 subnets max**: one for your Apps, and (optionally) one for your Data.

### Deep Dive: What is a Private Endpoint? (Crucial Concept)
You asked for a better explanation. Think of it like this:

*   **Public Endpoint (The Standard Way)**:
    *   **Analogy**: Your Database is a **Bank on Main Street**.
    *   **Access**: Anyone on the internet can see the building. It has a public address.
    *   **Security**: You hire a Bouncer (Firewall) to stand at the door and only let people in if they show a specific ID card (IP Address).
    *   **Risk**: The Bouncer might get tired (misconfiguration). The usage is visible to attackers.

*   **Private Endpoint (The Enterprise Way)**:
    *   **Analogy**: Your Database is a **Secret Vault in your Basement**.
    *   **Access**: It has **NO door** to the outside street. It is invisible to the public internet.
    *   **How you get in**: There is a **Secret Tunnel** (Network Interface) that connects the Vault directly to your Living Room (VNet).
    *   **Result**: To access the database, you MUST be inside the house (VNet). Hackers on the internet can't even "ping" it because it effectively doesn't exist for them.

**This is why we created the `data-subnet`**: It is the "Basement" where these Secret Tunnels live.

### Service Endpoints vs Private Endpoints: Which is Better?
Now that you understand the concept, here is the comparison:

| Feature | Option A: Service Endpoints | Option B: Private Endpoints |
| :--- | :--- | :--- |
| **How it works** | Acts like a **VIP ID Card**. The DB sees your VNet traffic coming and says "Ah, you are on the Guest List, come in." Traffic stays on Azure backbone. | Acts like a **Secret Door**. Your DB literally gets a private IP address (e.g., `10.0.1.5`) inside your VNet. It effectively becomes *part* of your network. |
| **Public IP?** | Yes, the DB still has a public IP, but the firewall blocks everyone except your VNet. | No, the public access can be completely disabled. |
| **Cost** | **Free**. | **$$$**. You pay an hourly rate (~$7/month) + data processing fees per endpoint. |
| **Complexity** | **Easy**. Just 1 click in the "Networking" tab. | **Hard**. Requires managing DNS zones, link resources, and extra subnets. |
| **Recommendation** | **🏆 Winner for You**. It provides excellent security (blocking the internet) with zero cost and zero complexity. | **Enterprise Only**. Use this only if you have strict compliance rules (e.g., banking/healthcare) that ban ALL public IPs. |

**What we are using**: This guide uses **Service Endpoints** (Option A) because it's much easier to set up manually and saves you money.
*   **Infrastructure Management**: Azure Container Apps runs on Kubernetes. It needs this delegated subnet to automatically assign private IP addresses to the underlying pods and nodes that run your code.
*   **Performance**: Traffic between your App and valid Azure services (like KeyVault or SQL) stays on the Microsoft backbone network, avoiding the public internet.

---

## 3. Create Dependencies

### A. Azure Container Registry (ACR)
1. Search for **Container registries**.
2. Click **+ Create**.
3. **Resource Group**: `aicalendar-rg`.
4. **Name**: `aicalendaracr` (must be unique globally).
5. **SKU**: *Basic*.
6. Click **Review + create**, then **Create**.
7. Once created, go to the resource -> **Access keys** (left menu).
8. Enable **Admin user**.
9. **Copy**: Login server, Username, and password (you'll need these).

### B. Azure SQL Database (Enterprise Setup)
1. Search for **SQL databases**.
2. Click **+ Create**.
3. **Resource Group**: `aicalendar-rg`.
4. **Database name**: `AICalendarDb`.
5. **Server**: Create new (`aicalendar-sql`). Use SQL Auth (`azureuser`).
6. **Workload**: Development.
7. **Compute**: Standard/Basic.
8. **Networking** Tab (Crucial!):
   - **Connectivity method**: Select **Private endpoint**.
   - **Add private endpoint**:
     - **Name**: `sql-pe`
     - **Subscription/Resource Group**: Same as above.
   - **Connectivity method**: Select **Private endpoint**.
   - **Add private endpoint**:
     - **Name**: `redis-pe`
     - **Subnet**: `data-subnet`.
     - **Private DNS integration**: **Yes** (`privatelink.redis.cache.windows.net`).
5. Click **Review + create**, then **Create**.

### C. Azure Redis Cache (Enterprise Setup)
1. Search for **Azure Cache for Redis**.
2. Click **+ Create**.
3. **Resource Group**: `aicalendar-rg`.
4. **DNS Name**: `aicalendar-redis` (must be unique).
5. **Location**: Same as VNet (Spain Central).
6. **Cache type**: **Basic C0** (Cheapest, ~$16/mo).
7. **Networking** Tab:
   - **Connectivity method**: Select **Private endpoint**.
   - **Add private endpoint**:
     - **Name**: `redis-pe`
     - **Location**: same as VNet (Spain Central).
     - **Subnet**: `data-subnet`.
     - **Private DNS integration**: **Yes** (`privatelink.redis.cache.windows.net`).
8. Click **Review + create**, then **Create**.
   *   *Note: Redis takes about 15-20 minutes to create. Be patient!*

---

## 4. Deploy Infrastructure (Container Apps)

### A. Create Environment
1. Search for **Container Apps**.
2. Click **+ Create**.
3. **Resource Group**: `aicalendar-rg`.
4. **Name**: `aicalendar-api`.
5. **Environment**: Click **Create new**.
   - **Environment name**: `aicalendar-env`.
   - **Networking** tab:
     - **Use your own virtual network**: Yes.
     - **Virtual network**: `aicalendar-vnet`.
     - **Infrastructure subnet**: `aca-subnet`.
   - Click **Create**.

### B. Configure App Settings
1. Back on the "Create Container App" screen:
2. Uncheck "Use quickstart image".
3. **Name**: `aicalendar-api`.
4. **Image Source**: *Azure Container Registry*.
   - **Registry**: `aicalendaracr` (from step 3A).
   - **Image**: Select your image (you must push it first! see below).
   - *Note: If you haven't pushed an image yet, use "Quickstart image" for now and update it later.*
5. **Ingress** (Networking):
   - **Enabled**: Yes.
   - **Traffic**: *Accepting traffic from anywhere*.
   - **Target port**: `8080`.

### C. Environment Variables (Connection Strings)
1. Go to the **Tags** tab (skip), then **Review + create**.
2. Wait for deployment to finish.
3. Go to the new Container App (`aicalendar-api`).
4. In left menu, select **Containers**.
5. Click **Edit and deploy**.
6. Select the container image -> **Environment variables**.
7. Add the following secrets/env vars:

| Name | Value |
|------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Copy from SQL Db -> Connection strings (ADO.NET). Replace `{your_password}`. |
| `ConnectionStrings__RedisConnection` | Copy from Redis -> Access keys -> Primary connection string. |

8. Click **Save**, then **Create** to update the app.

---

## 5. Build & Push Image (Local Step)
You still need to push your code to the registry created in Step 3A.

**If you have Docker Desktop installed locally:**
1. Login: `docker login aicalendaracr.azurecr.io -u <username> -p <password>`
2. Build: `docker build -t aicalendaracr.azurecr.io/aicalendar-api:latest .`
3. Push: `docker push aicalendaracr.azurecr.io/aicalendar-api:latest`
