# AICalendar - Multi-Container Deployment Guide

## System Architecture Overview

Your AICalendar application consists of **7 interconnected containers**:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Load Balancer / Ingress                  │
└─────────────────────────────────────────────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        │                        │                        │
┌───────▼────────┐      ┌───────▼────────┐      ┌───────▼────────┐
│  API Service   │      │    Grafana     │      │      Seq       │
│  (.NET 8 API)  │      │  (Monitoring)  │      │   (Logging)    │
│   Port: 8080   │      │   Port: 3000   │      │   Port: 5341   │
└───────┬────────┘      └───────┬────────┘      └────────────────┘
        │                        │
        ├────────┬───────────────┘
        │        │
┌───────▼────────┐      ┌────────▼───────┐
│   SQL Server   │      │   Prometheus   │
│   (Database)   │      │   (Metrics)    │
│   Port: 1433   │      │   Port: 9090   │
└────────────────┘      └────────────────┘
        │
┌───────▼────────┐      ┌────────────────┐
│     Redis      │      │ Node Exporter  │
│    (Cache)     │      │ (System Stats) │
│   Port: 6379   │      │   Port: 9100   │
└────────────────┘      └────────────────┘
```

### Container Breakdown

| Container | Image | Purpose | Port(s) | Persistent Data |
|-----------|-------|---------|---------|-----------------|
| **api** | Custom (.NET 8) | Main API application | 8080, 8081 | None (stateless) |
| **db** | mssql/server:2022 | SQL Server database | 1433 | ✅ `/var/opt/mssql` |
| **redis** | redis:7-alpine | Caching layer | 6379 | ✅ `/data` |
| **prometheus** | prom/prometheus | Metrics collection | 9090 | ✅ `/prometheus` |
| **grafana** | grafana/grafana | Monitoring dashboards | 3000 | ✅ `/var/lib/grafana` |
| **seq** | datalust/seq | Structured logging | 5341 | ✅ `/data` |
| **node-exporter** | prom/node-exporter | System metrics | 9100 | None (read-only) |

---

## Deployment Options

### Option 1: Azure Container Apps with Azure Resources (Recommended)

Deploy all containers to Azure using managed services for production-grade reliability.

#### Architecture

```
Azure Container Apps Environment
├── API Container (Auto-scaling)
├── Grafana Container
├── Prometheus Container
└── Seq Container

External Managed Services
├── Azure SQL Database (replaces db container)
├── Azure Cache for Redis (replaces redis container)
└── Azure Monitor (optional, replaces node-exporter)
```

#### Prerequisites

```bash
# Install Azure CLI
# Windows: https://aka.ms/installazurecliwindows
# Mac: brew install azure-cli
# Linux: curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

az login
az account set --subscription "Your-Subscription-Name"
```

#### Step 1: Create Resource Group

```bash
az group create \
  --name aicalendar-prod-rg \
  --location eastus
```

#### Step 2: Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name aicalendar-sql-prod \
  --resource-group aicalendar-prod-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password 'YourStrong@Password123!'

# Create database
az sql db create \
  --resource-group aicalendar-prod-rg \
  --server aicalendar-sql-prod \
  --name AICalendarDb \
  --service-objective S1 \
  --backup-storage-redundancy Local

# Allow Azure services to access
az sql server firewall-rule create \
  --resource-group aicalendar-prod-rg \
  --server aicalendar-sql-prod \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Get connection string
echo "Server=tcp:aicalendar-sql-prod.database.windows.net,1433;Initial Catalog=AICalendarDb;Persist Security Info=False;User ID=sqladmin;Password=YourStrong@Password123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

#### Step 3: Create Azure Cache for Redis

```bash
az redis create \
  --name aicalendar-redis-prod \
  --resource-group aicalendar-prod-rg \
  --location eastus \
  --sku Basic \
  --vm-size c1

# Get Redis connection info
az redis show \
  --name aicalendar-redis-prod \
  --resource-group aicalendar-prod-rg \
  --query [hostName,sslPort] -o tsv

az redis list-keys \
  --name aicalendar-redis-prod \
  --resource-group aicalendar-prod-rg \
  --query primaryKey -o tsv
```

#### Step 4: Create Container Registry

```bash
az acr create \
  --resource-group aicalendar-prod-rg \
  --name aicalendarprodacr \
  --sku Basic \
  --admin-enabled true

# Get login credentials
az acr credential show \
  --name aicalendarprodacr \
  --query "[username, passwords[0].value]" -o tsv
```

#### Step 5: Build and Push Images

```bash
# Login to registry
az acr login --name aicalendarprodacr

# Build and push API image
docker build -t aicalendarprodacr.azurecr.io/aicalendar-api:latest \
  -f Dockerfile \
  --target final .
docker push aicalendarprodacr.azurecr.io/aicalendar-api:latest

# Push monitoring images (pull from Docker Hub, tag, push to ACR)
docker pull grafana/grafana:latest
docker tag grafana/grafana:latest aicalendarprodacr.azurecr.io/grafana:latest
docker push aicalendarprodacr.azurecr.io/grafana:latest

docker pull prom/prometheus:latest
docker tag prom/prometheus:latest aicalendarprodacr.azurecr.io/prometheus:latest
docker push aicalendarprodacr.azurecr.io/prometheus:latest

docker pull datalust/seq:latest
docker tag datalust/seq:latest aicalendarprodacr.azurecr.io/seq:latest
docker push aicalendarprodacr.azurecr.io/seq:latest
```

#### Step 6: Create Container Apps Environment

```bash
az containerapp env create \
  --name aicalendar-env \
  --resource-group aicalendar-prod-rg \
  --location eastus
```

#### Step 7: Deploy API Container

```bash
az containerapp create \
  --name aicalendar-api \
  --resource-group aicalendar-prod-rg \
  --environment aicalendar-env \
  --image aicalendarprodacr.azurecr.io/aicalendar-api:latest \
  --registry-server aicalendarprodacr.azurecr.io \
  --registry-username $(az acr credential show --name aicalendarprodacr --query username -o tsv) \
  --registry-password $(az acr credential show --name aicalendarprodacr --query "passwords[0].value" -o tsv) \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 5 \
  --cpu 1.0 \
  --memory 2.0Gi \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ASPNETCORE_URLS=http://+:8080" \
    "ConnectionStrings__DefaultConnection=Server=tcp:aicalendar-sql-prod.database.windows.net,1433;Initial Catalog=AICalendarDb;User ID=sqladmin;Password=YourStrong@Password123!;Encrypt=True;" \
    "ConnectionStrings__RedisConnection=aicalendar-redis-prod.redis.cache.windows.net:6380,password=YOUR_REDIS_KEY,ssl=True,abortConnect=False" \
    "Cache__Enabled=true" \
    "Cache__Provider=Redis"
```

#### Step 8: Deploy Monitoring Containers

**Deploy Prometheus:**

```bash
# Create storage account for Prometheus data
az storage account create \
  --name aicalendarprodstore \
  --resource-group aicalendar-prod-rg \
  --location eastus \
  --sku Standard_LRS

az storage share create \
  --name prometheus-config \
  --account-name aicalendarprodstore

# Upload prometheus.yml
az storage file upload \
  --account-name aicalendarprodstore \
  --share-name prometheus-config \
  --source ./monitoring/prometheus.yml \
  --path prometheus.yml

az containerapp create \
  --name prometheus \
  --resource-group aicalendar-prod-rg \
  --environment aicalendar-env \
  --image aicalendarprodacr.azurecr.io/prometheus:latest \
  --target-port 9090 \
  --ingress internal \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.5 \
  --memory 1.0Gi
```

**Deploy Grafana:**

```bash
az containerapp create \
  --name grafana \
  --resource-group aicalendar-prod-rg \
  --environment aicalendar-env \
  --image aicalendarprodacr.azurecr.io/grafana:latest \
  --target-port 3000 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    "GF_SECURITY_ADMIN_USER=admin" \
    "GF_SECURITY_ADMIN_PASSWORD=YourGrafanaPassword123!"
```

**Deploy Seq:**

```bash
az containerapp create \
  --name seq \
  --resource-group aicalendar-prod-rg \
  --environment aicalendar-env \
  --image aicalendarprodacr.azurecr.io/seq:latest \
  --target-port 80 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 1 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    "ACCEPT_EULA=Y" \
    "SEQ_FIRSTRUN_ADMINPASSWORD=YourSeqPassword123!"
```

#### Step 9: Get Application URLs

```bash
# Get API URL
az containerapp show \
  --name aicalendar-api \
  --resource-group aicalendar-prod-rg \
  --query properties.configuration.ingress.fqdn -o tsv

# Get Grafana URL
az containerapp show \
  --name grafana \
  --resource-group aicalendar-prod-rg \
  --query properties.configuration.ingress.fqdn -o tsv

# Get Seq URL
az containerapp show \
  --name seq \
  --resource-group aicalendar-prod-rg \
  --query properties.configuration.ingress.fqdn -o tsv
```

#### Cost Estimate (Azure)

| Service | Configuration | Monthly Cost |
|---------|---------------|--------------|
| Azure SQL Database | S1 (20 DTUs) | ~$30 |
| Azure Cache for Redis | Basic C1 (1GB) | ~$40 |
| Container Apps - API | 1-5 replicas, 1vCPU, 2GB | ~$30-100 |
| Container Apps - Monitoring | 3 containers, 0.5vCPU each | ~$45 |
| Container Registry | Basic | ~$5 |
| Storage Account | Standard LRS | ~$2 |
| **Total** | | **~$152-222/month** |

---

### Option 2: AWS with ECS Fargate

Deploy all containers to AWS using Elastic Container Service.

#### Architecture

```
ECS Fargate Cluster
├── API Task (Auto-scaling)
├── Grafana Task
├── Prometheus Task
└── Seq Task

AWS Managed Services
├── RDS for SQL Server (replaces db container)
└── ElastiCache Redis (replaces redis container)
```

#### Prerequisites

```bash
# Install AWS CLI
# https://aws.amazon.com/cli/

aws configure
```

#### Step 1: Create VPC and Networking

```bash
# Create VPC
aws ec2 create-vpc \
  --cidr-block 10.0.0.0/16 \
  --tag-specifications 'ResourceType=vpc,Tags=[{Key=Name,Value=aicalendar-vpc}]' \
  --query 'Vpc.VpcId' \
  --output text

# Save VPC ID
VPC_ID=<your-vpc-id>

# Create subnets (2 for high availability)
aws ec2 create-subnet \
  --vpc-id $VPC_ID \
  --cidr-block 10.0.1.0/24 \
  --availability-zone us-east-1a

aws ec2 create-subnet \
  --vpc-id $VPC_ID \
  --cidr-block 10.0.2.0/24 \
  --availability-zone us-east-1b
```

#### Step 2: Create RDS SQL Server

```bash
# Create DB subnet group
aws rds create-db-subnet-group \
  --db-subnet-group-name aicalendar-db-subnet \
  --db-subnet-group-description "AICalendar DB Subnet" \
  --subnet-ids subnet-xxx subnet-yyy

# Create SQL Server instance
aws rds create-db-instance \
  --db-instance-identifier aicalendar-db \
  --db-instance-class db.t3.small \
  --engine sqlserver-ex \
  --master-username sqladmin \
  --master-user-password 'YourStrong@Password123!' \
  --allocated-storage 20 \
  --db-subnet-group-name aicalendar-db-subnet \
  --publicly-accessible \
  --backup-retention-period 7

# Get endpoint (after creation completes)
aws rds describe-db-instances \
  --db-instance-identifier aicalendar-db \
  --query 'DBInstances[0].Endpoint.Address' \
  --output text
```

#### Step 3: Create ElastiCache Redis

```bash
aws elasticache create-cache-cluster \
  --cache-cluster-id aicalendar-redis \
  --cache-node-type cache.t3.micro \
  --engine redis \
  --num-cache-nodes 1

# Get endpoint
aws elasticache describe-cache-clusters \
  --cache-cluster-id aicalendar-redis \
  --show-cache-node-info \
  --query 'CacheClusters[0].CacheNodes[0].Endpoint.Address' \
  --output text
```

#### Step 4: Create ECS Cluster

```bash
aws ecs create-cluster \
  --cluster-name aicalendar-cluster \
  --capacity-providers FARGATE FARGATE_SPOT
```

#### Step 5: Create ECR Repositories and Push Images

```bash
# Create repositories
aws ecr create-repository --repository-name aicalendar/api
aws ecr create-repository --repository-name aicalendar/grafana
aws ecr create-repository --repository-name aicalendar/prometheus
aws ecr create-repository --repository-name aicalendar/seq

# Login to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com

# Build and push API
docker build -t aicalendar-api:latest -f Dockerfile --target final .
docker tag aicalendar-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/aicalendar/api:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/aicalendar/api:latest

# Push monitoring images
docker pull grafana/grafana:latest
docker tag grafana/grafana:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/aicalendar/grafana:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/aicalendar/grafana:latest

# Repeat for prometheus and seq
```

#### Step 6: Create Task Definitions

Create `ecs-api-task.json`:

```json
{
  "family": "aicalendar-api",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "1024",
  "memory": "2048",
  "containerDefinitions": [
    {
      "name": "api",
      "image": "<account-id>.dkr.ecr.us-east-1.amazonaws.com/aicalendar/api:latest",
      "portMappings": [
        {
          "containerPort": 8080,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        },
        {
          "name": "ConnectionStrings__DefaultConnection",
          "value": "Server=your-rds-endpoint.rds.amazonaws.com,1433;Database=AICalendarDb;User ID=sqladmin;Password=YourPassword;Encrypt=True;"
        },
        {
          "name": "ConnectionStrings__RedisConnection",
          "value": "your-redis-endpoint:6379,abortConnect=false"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/aicalendar-api",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

Register task definition:

```bash
aws ecs register-task-definition --cli-input-json file://ecs-api-task.json
```

#### Step 7: Create ECS Services

```bash
# Create API service
aws ecs create-service \
  --cluster aicalendar-cluster \
  --service-name aicalendar-api \
  --task-definition aicalendar-api \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxx,subnet-yyy],securityGroups=[sg-xxx],assignPublicIp=ENABLED}"
```

Repeat for Grafana, Prometheus, and Seq services.

#### Cost Estimate (AWS)

| Service | Configuration | Monthly Cost |
|---------|---------------|--------------|
| RDS SQL Server Express | db.t3.small | ~$40 |
| ElastiCache Redis | cache.t3.micro | ~$15 |
| ECS Fargate - API | 2 tasks, 1vCPU, 2GB | ~$60 |
| ECS Fargate - Monitoring | 3 tasks, 0.5vCPU each | ~$45 |
| ECR Storage | <10GB | ~$1 |
| **Total** | | **~$161/month** |

---

### Option 3: Render with Multiple Services

Deploy containers separately on Render using their multi-service support.

#### Prerequisites

- External SQL Server (Azure SQL or AWS RDS)
- Redis provider (Redis Labs, Upstash, or AWS ElastiCache)
- Render account

#### Step 1: Create `render.yaml`

Create this file in your project root:

```yaml
services:
  # Main API Service
  - type: web
    name: aicalendar-api
    env: docker
    dockerfilePath: ./Dockerfile
    dockerContext: .
    healthCheckPath: /health
    plan: starter
    autoDeploy: true
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ASPNETCORE_URLS
        value: http://+:8080
      - key: ConnectionStrings__DefaultConnection
        sync: false # Set in dashboard
      - key: ConnectionStrings__RedisConnection
        fromService:
          type: redis
          name: aicalendar-redis
          property: connectionString
      - key: Cache__Enabled
        value: true
      - key: Cache__Provider
        value: Redis

  # Grafana Service
  - type: web
    name: aicalendar-grafana
    env: docker
    dockerfilePath: ./Dockerfile.grafana
    dockerContext: ./monitoring
    plan: starter
    envVars:
      - key: GF_SECURITY_ADMIN_USER
        value: admin
      - key: GF_SECURITY_ADMIN_PASSWORD
        generateValue: true
      - key: GF_USERS_ALLOW_SIGN_UP
        value: false

  # Prometheus Service
  - type: web
    name: aicalendar-prometheus
    env: docker
    dockerfilePath: ./Dockerfile.prometheus
    dockerContext: ./monitoring
    plan: starter

  # Seq Service
  - type: web
    name: aicalendar-seq
    env: docker
    dockerfilePath: ./Dockerfile.seq
    dockerContext: .
    plan: starter
    envVars:
      - key: ACCEPT_EULA
        value: Y
      - key: SEQ_FIRSTRUN_ADMINPASSWORD
        generateValue: true

# Redis (managed by Render)
databases:
  - name: aicalendar-redis
    plan: starter
    maxmemoryPolicy: allkeys-lru
```

#### Step 2: Create Dockerfiles for Monitoring Services

**File**: `monitoring/Dockerfile.grafana`

```dockerfile
FROM grafana/grafana:latest

# Copy provisioning configs
COPY grafana/provisioning /etc/grafana/provisioning
COPY grafana/dashboards /var/lib/grafana/dashboards

EXPOSE 3000
```

**File**: `monitoring/Dockerfile.prometheus`

```dockerfile
FROM prom/prometheus:latest

# Copy prometheus config
COPY prometheus.yml /etc/prometheus/prometheus.yml

EXPOSE 9090
```

**File**: `Dockerfile.seq`

```dockerfile
FROM datalust/seq:latest

EXPOSE 80
```

#### Step 3: Update Prometheus Config for Render

Edit `monitoring/prometheus.yml` to use Render internal URLs:

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'aicalendar-api'
    static_configs:
      - targets: ['aicalendar-api:8080']

  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
```

#### Step 4: Deploy to Render

```bash
# Commit and push
git add render.yaml monitoring/Dockerfile.* Dockerfile.seq
git commit -m "Add Render multi-service configuration"
git push origin main

# Render will auto-detect render.yaml and create all services
```

#### Step 5: Configure Inter-Service Communication

In Render dashboard:

1. Go to API service settings
2. Add environment variables for internal service URLs:

```bash
Prometheus__Url=http://aicalendar-prometheus:9090
Seq__Url=http://aicalendar-seq
Grafana__Url=http://aicalendar-grafana:3000
```

#### Cost Estimate (Render)

| Service | Plan | Monthly Cost |
|---------|------|--------------|
| API Service | Starter | $7 |
| Grafana | Starter | $7 |
| Prometheus | Starter | $7 |
| Seq | Starter | $7 |
| Redis | Starter | $10 |
| Azure SQL (external) | Basic | $5 |
| **Total** | | **~$43/month** |

---

### Option 4: Docker Swarm (Self-Hosted)

Deploy all containers to your own server using Docker Swarm for orchestration.

#### Prerequisites

- Linux server (Ubuntu 20.04+ or CentOS 8+)
- Docker installed
- Domain name with DNS configured

#### Step 1: Initialize Swarm

```bash
# On your server
docker swarm init
```

#### Step 2: Create Docker Stack File

Create `docker-stack.yml`:

```yaml
version: '3.8'

services:
  api:
    image: your-registry/aicalendar-api:latest
    deploy:
      replicas: 2
      update_config:
        parallelism: 1
        delay: 10s
      restart_policy:
        condition: on-failure
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${SQL_CONNECTION}
      - ConnectionStrings__RedisConnection=redis:6379,password=${REDIS_PASSWORD}
    networks:
      - aicalendar-network

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    deploy:
      placement:
        constraints:
          - node.role == manager
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD}
    volumes:
      - mssql_data:/var/opt/mssql
    networks:
      - aicalendar-network

  redis:
    image: redis:7-alpine
    deploy:
      placement:
        constraints:
          - node.role == manager
    command: redis-server --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis_data:/data
    networks:
      - aicalendar-network

  prometheus:
    image: prom/prometheus:latest
    deploy:
      placement:
        constraints:
          - node.role == manager
    configs:
      - source: prometheus_config
        target: /etc/prometheus/prometheus.yml
    volumes:
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"
    networks:
      - aicalendar-network

  grafana:
    image: grafana/grafana:latest
    deploy:
      placement:
        constraints:
          - node.role == manager
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
    volumes:
      - grafana_data:/var/lib/grafana
    ports:
      - "3000:3000"
    networks:
      - aicalendar-network

  seq:
    image: datalust/seq:latest
    deploy:
      placement:
        constraints:
          - node.role == manager
    environment:
      - ACCEPT_EULA=Y
      - SEQ_FIRSTRUN_ADMINPASSWORD=${SEQ_PASSWORD}
    volumes:
      - seq_data:/data
    ports:
      - "5341:80"
    networks:
      - aicalendar-network

  node-exporter:
    image: prom/node-exporter:latest
    deploy:
      mode: global
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    networks:
      - aicalendar-network

volumes:
  mssql_data:
  redis_data:
  prometheus_data:
  grafana_data:
  seq_data:

networks:
  aicalendar-network:
    driver: overlay

configs:
  prometheus_config:
    file: ./monitoring/prometheus.yml
```

#### Step 3: Create Environment File

Create `.env.production`:

```bash
MSSQL_SA_PASSWORD=YourStrong@Password123!
REDIS_PASSWORD=YourRedisPassword123!
SQL_CONNECTION=Server=db;Database=AICalendarDb;User Id=sa;Password=YourStrong@Password123!;TrustServerCertificate=True;
GRAFANA_PASSWORD=YourGrafanaPassword123!
SEQ_PASSWORD=YourSeqPassword123!
```

#### Step 4: Deploy Stack

```bash
# Build and push API image to registry
docker build -t your-registry/aicalendar-api:latest -f Dockerfile --target final .
docker push your-registry/aicalendar-api:latest

# Deploy stack
docker stack deploy -c docker-stack.yml --env-file .env.production aicalendar
```

#### Step 5: Set Up Reverse Proxy (Nginx)

Install Nginx on host:

```bash
sudo apt-get install nginx certbot python3-certbot-nginx
```

Create `/etc/nginx/sites-available/aicalendar`:

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}

server {
    listen 80;
    server_name grafana.yourdomain.com;

    location / {
        proxy_pass http://localhost:3000;
        proxy_set_header Host $host;
    }
}

server {
    listen 80;
    server_name seq.yourdomain.com;

    location / {
        proxy_pass http://localhost:5341;
        proxy_set_header Host $host;
    }
}
```

Enable and get SSL:

```bash
sudo ln -s /etc/nginx/sites-available/aicalendar /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
sudo certbot --nginx
```

#### Cost Estimate (Self-Hosted)

| Item | Configuration | Monthly Cost |
|------|---------------|--------------|
| VPS Server | 4GB RAM, 2 vCPU | $20-40 |
| Domain | .com | $12/year (~$1/mo) |
| Backups | 100GB | $5 |
| **Total** | | **~$26-46/month** |

---

## Environment Variables Reference

### Required for All Deployments

```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# Database
ConnectionStrings__DefaultConnection=<SQL-SERVER-CONNECTION-STRING>

# Redis Cache
ConnectionStrings__RedisConnection=<REDIS-CONNECTION-STRING>
Cache__Enabled=true
Cache__Provider=Redis

# Monitoring (Optional)
Prometheus__Url=http://prometheus:9090
Seq__Url=http://seq
```

### Service-Specific

**Grafana:**
```bash
GF_SECURITY_ADMIN_USER=admin
GF_SECURITY_ADMIN_PASSWORD=<strong-password>
GF_USERS_ALLOW_SIGN_UP=false
```

**Seq:**
```bash
ACCEPT_EULA=Y
SEQ_FIRSTRUN_ADMINPASSWORD=<strong-password>
```

**SQL Server:**
```bash
ACCEPT_EULA=Y
MSSQL_SA_PASSWORD=<strong-password>
MSSQL_PID=Developer
```

**Redis:**
```bash
REDIS_PASSWORD=<strong-password>
```

---

## Service Communication Matrix

### Internal Network Communication

| From Service | To Service | Port | Purpose |
|--------------|------------|------|---------|
| API | db | 1433 | Database queries |
| API | redis | 6379 | Cache operations |
| API | seq | 80 | Log shipping |
| prometheus | API | 8080 | Metrics scraping |
| prometheus | node-exporter | 9100 | System metrics |
| grafana | prometheus | 9090 | Data source |

### External Access (Public Internet)

| Service | Port | Should Be Public? | Purpose |
|---------|------|-------------------|---------|
| API | 8080 | ✅ Yes | Main application |
| Grafana | 3000 | ⚠️ Optional | Monitoring dashboards |
| Seq | 5341 | ⚠️ Optional | Log viewer |
| Prometheus | 9090 | ❌ No | Internal metrics only |
| db | 1433 | ❌ No | Database (security risk) |
| redis | 6379 | ❌ No | Cache (security risk) |

---

## Monitoring and Observability

### Access Points After Deployment

| Service | Local URL | Purpose | Default Credentials |
|---------|-----------|---------|---------------------|
| **API** | http://localhost:8080 | Main API | N/A |
| **Swagger** | http://localhost:8080/swagger | API documentation | N/A |
| **Grafana** | http://localhost:3000 | Dashboards | admin / (see env) |
| **Prometheus** | http://localhost:9090 | Metrics | N/A |
| **Seq** | http://localhost:5341 | Logs | (see env) |
| **Hangfire** | http://localhost:8080/hangfire | Background jobs | N/A |

### Key Metrics to Monitor

**Application Metrics** (via Prometheus):
- HTTP request duration
- Request count by endpoint
- Error rate (4xx, 5xx)
- Active connections
- Background job status

**Infrastructure Metrics** (via Node Exporter):
- CPU usage
- Memory usage
- Disk I/O
- Network traffic

**Database Metrics** (SQL Server):
- Connection pool size
- Query execution time
- Deadlocks
- Database size

---

## Backup and Disaster Recovery

### Data Volumes to Backup

| Volume | Priority | Backup Frequency | Retention |
|--------|----------|------------------|-----------|
| SQL Server data | Critical | Hourly | 30 days |
| Redis data | Medium | Daily | 7 days |
| Seq logs | Low | Weekly | 90 days |
| Grafana dashboards | Low | Daily | 30 days |
| Prometheus metrics | Low | Daily | 14 days |

### Backup Scripts

**Azure Backup:**

```bash
# Automated SQL backup (built-in to Azure SQL)
az sql db show \
  --resource-group aicalendar-prod-rg \
  --server aicalendar-sql-prod \
  --name AICalendarDb \
  --query "earliestRestoreDate"

# Manual backup
az sql db export \
  --resource-group aicalendar-prod-rg \
  --server aicalendar-sql-prod \
  --name AICalendarDb \
  --admin-user sqladmin \
  --admin-password 'YourPassword' \
  --storage-key-type StorageAccessKey \
  --storage-key '<key>' \
  --storage-uri 'https://yourstorage.blob.core.windows.net/backups/backup.bacpac'
```

**Docker Volume Backup:**

```bash
# Backup SQL Server volume
docker run --rm \
  -v aicalendar_mssql_data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/mssql-backup-$(date +%Y%m%d).tar.gz /data

# Backup Redis volume
docker run --rm \
  -v aicalendar_redis_data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/redis-backup-$(date +%Y%m%d).tar.gz /data
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy Multi-Container App

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Login to Azure Container Registry
        uses: azure/docker-login@v1
        with:
          login-server: ${{ secrets.ACR_NAME }}.azurecr.io
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}

      - name: Build and Push API Image
        run: |
          docker build -t ${{ secrets.ACR_NAME }}.azurecr.io/aicalendar-api:${{ github.sha }} \
            -t ${{ secrets.ACR_NAME }}.azurecr.io/aicalendar-api:latest \
            -f Dockerfile --target final .
          docker push ${{ secrets.ACR_NAME }}.azurecr.io/aicalendar-api:${{ github.sha }}
          docker push ${{ secrets.ACR_NAME }}.azurecr.io/aicalendar-api:latest

      - name: Deploy to Azure Container Apps
        uses: azure/CLI@v1
        with:
          inlineScript: |
            az containerapp update \
              --name aicalendar-api \
              --resource-group aicalendar-prod-rg \
              --image ${{ secrets.ACR_NAME }}.azurecr.io/aicalendar-api:${{ github.sha }}
```

---

## Troubleshooting Guide

### Issue: Containers Can't Communicate

**Symptoms**: API can't connect to database or Redis

**Solutions**:
1. Verify all containers are on the same network
2. Use service names for DNS (e.g., `db` not `localhost`)
3. Check firewall rules in cloud providers
4. Verify security groups (AWS) or NSGs (Azure)

```bash
# Test connectivity
docker exec aicalendar-api ping db
docker exec aicalendar-api nc -zv redis 6379
```

### Issue: Prometheus Can't Scrape Metrics

**Symptoms**: No data in Grafana

**Solutions**:
1. Verify API exposes `/metrics` endpoint
2. Check Prometheus config targets
3. Ensure containers are on same network

```bash
# Test metrics endpoint
curl http://api:8080/metrics

# Check Prometheus targets
curl http://prometheus:9090/api/v1/targets
```

### Issue: Database Migration Fails

**Symptoms**: API crashes on startup with migration error

**Solutions**:
1. Run migrations manually
2. Check connection string
3. Verify database permissions

```bash
# Run migrations manually
docker exec -it aicalendar-api dotnet ef database update
```

### Issue: Out of Memory

**Symptoms**: Containers restart frequently

**Solutions**:
1. Increase memory limits
2. Optimize SQL queries
3. Configure Redis maxmemory policy

```bash
# Check memory usage
docker stats

# Azure: Update container memory
az containerapp update --name aicalendar-api --memory 4.0Gi
```

---

## Security Best Practices

### 1. Use Secrets Management

**Azure:**
```bash
az containerapp secret set \
  --name aicalendar-api \
  --secrets db-password=YourPassword
```

**AWS:**
```bash
aws secretsmanager create-secret \
  --name aicalendar/db-password \
  --secret-string "YourPassword"
```

### 2. Network Segmentation

- Put databases and Redis in private subnets
- Use security groups to restrict access
- Only expose API publicly

### 3. Enable HTTPS

- Use Let's Encrypt for free SSL
- Configure HTTPS in API (update Dockerfile EXPOSE 8081)
- Use Azure Front Door or AWS ALB for SSL termination

### 4. Regular Updates

```bash
# Update all images weekly
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull redis:7-alpine
docker pull grafana/grafana:latest
docker pull prom/prometheus:latest
docker pull datalust/seq:latest
```

---

## Recommended Deployment Path

### For Production (Best Performance & Reliability):
👉 **Option 1: Azure Container Apps**
- Native .NET support
- Managed SQL Server
- Auto-scaling
- Best monitoring integration

### For Budget (Best Value):
👉 **Option 3: Render Multi-Service**
- Simple setup
- Low cost ($43/month)
- Good for small teams

### For Control (Self-Managed):
👉 **Option 4: Docker Swarm**
- Full control
- Lowest cost ($26-46/month)
- Requires DevOps skills

---

## Next Steps

1. **Choose your deployment option** based on budget, scale, and expertise
2. **Set up managed databases** (Azure SQL or RDS) for production
3. **Configure environment variables** with secure credentials
4. **Deploy monitoring stack first** (Prometheus, Grafana, Seq)
5. **Deploy API and test connectivity** to all services
6. **Set up CI/CD pipeline** for automated deployments
7. **Configure backups** for critical data volumes
8. **Set up alerts** in Grafana for critical metrics
9. **Load test** your deployment
10. **Document** your specific configuration and credentials

---

**Last Updated**: December 2025
**Maintained By**: AICalendar Team
