# setup.ps1 - Quick setup script for AICalendar
# Works on: Windows (PowerShell)

$ErrorActionPreference = "Stop"

Write-Host "AICalendar - Setup Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check execution policy and set if needed
$currentPolicy = Get-ExecutionPolicy -Scope CurrentUser
if ($currentPolicy -eq "Restricted" -or $currentPolicy -eq "Undefined") {
    Write-Host "Setting PowerShell execution policy..." -ForegroundColor Yellow
    try {
        Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
        Write-Host "Execution policy updated" -ForegroundColor Green
    } catch {
        Write-Host "Could not set execution policy. You may need to run PowerShell as Administrator." -ForegroundColor Yellow
    }
    Write-Host ""
}

# Check if Docker is installed
try {
    $dockerVersion = docker --version 2>$null
    if (-not $dockerVersion) {
        throw "Docker not found"
    }
    Write-Host "Docker is installed" -ForegroundColor Green
} catch {
    Write-Host "Docker is not installed. Please install Docker Desktop first." -ForegroundColor Red
    Write-Host "Visit: https://docs.docker.com/desktop/install/windows-install/" -ForegroundColor Yellow
    exit 1
}

# Check if Docker Compose is available
try {
    $composeVersion = docker compose version 2>$null
    if (-not $composeVersion) {
        throw "Docker Compose not found"
    }
} catch {
    Write-Host "Docker Compose is not available. Please update Docker Desktop." -ForegroundColor Red
    exit 1
}

Write-Host ""

# Function to generate random password
function Generate-Password {
    param([string]$Prefix, [int]$Length = 12)
    $bytes = New-Object byte[] $Length
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $base64 = [Convert]::ToBase64String($bytes) -replace '[/+=]', ''
    return "$Prefix$($base64.Substring(0, $Length))1"
}

# Create .env file if it doesn't exist
if (-not (Test-Path .env)) {
    Write-Host "Creating .env file..." -ForegroundColor Yellow

    if (Test-Path .env.example) {
        Copy-Item .env.example .env
    } else {
        Write-Host ".env.example not found. Creating default .env..." -ForegroundColor Yellow
        $defaultEnv = @"
# Database Configuration
MSSQL_SA_PASSWORD=YourStrong@Passw0rd
DB_HOST=db
DB_PORT=1433
DB_NAME=AICalendar
DB_USER=sa

# Redis Configuration
REDIS_PASSWORD=YourRedis@Passw0rd
REDIS_HOST=redis
REDIS_PORT=6379

# API Configuration
API_PORT=8080
ASPNETCORE_ENVIRONMENT=Development
"@
        $defaultEnv | Out-File -FilePath .env -Encoding UTF8
    }

    # Generate random passwords
    $mssqlPwd = Generate-Password -Prefix "Strong@" -Length 12
    $redisPwd = Generate-Password -Prefix "Redis@" -Length 16

    # Update .env with generated passwords
    $envContent = Get-Content .env -Raw
    $envContent = $envContent -replace 'YourStrong@Passw0rd', $mssqlPwd
    $envContent = $envContent -replace 'YourRedis@Passw0rd', $redisPwd
    $envContent | Out-File -FilePath .env -Encoding UTF8 -NoNewline

    Write-Host "Created .env file with generated passwords" -ForegroundColor Green
    Write-Host "SQL Server password: $mssqlPwd" -ForegroundColor Cyan
    Write-Host "Redis password: $redisPwd" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Passwords are saved in .env file (gitignored)" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host ".env file already exists" -ForegroundColor Green
    Write-Host ""
}

# Stop any running containers
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
try {
    docker compose down -v 2>$null
} catch {
    # Ignore errors
}
Write-Host ""

# Build containers
Write-Host "Building containers (this may take a few minutes)..." -ForegroundColor Yellow
docker compose build --no-cache
Write-Host ""

# Start services
Write-Host "Starting services..." -ForegroundColor Yellow
docker compose up -d
Write-Host ""

# Wait for services to be healthy
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Write-Host "This may take 30-60 seconds..." -ForegroundColor Gray
Write-Host ""

$maxWait = 60
$elapsed = 0

while ($elapsed -lt $maxWait) {
    try {
        $dbHealth = docker inspect --format='{{.State.Health.Status}}' aicalendar-db 2>$null
        $redisHealth = docker inspect --format='{{.State.Health.Status}}' aicalendar-redis 2>$null

        if (-not $dbHealth) { $dbHealth = "starting" }
        if (-not $redisHealth) { $redisHealth = "starting" }

        if ($dbHealth -eq "healthy" -and $redisHealth -eq "healthy") {
            Write-Host "All services are healthy!" -ForegroundColor Green
            break
        }

        $status = "Waiting... ({0}s) - DB: {1} | Redis: {2}" -f $elapsed, $dbHealth, $redisHealth
        Write-Host $status -ForegroundColor Gray
        Start-Sleep -Seconds 5
        $elapsed += 5
    } catch {
        $status = "Waiting... ({0}s) - Services starting..." -f $elapsed
        Write-Host $status -ForegroundColor Gray
        Start-Sleep -Seconds 5
        $elapsed += 5
    }
}

Write-Host ""

# Check if API is running
$apiRunning = docker ps --filter "name=aicalendar-api" --format "{{.Names}}" 2>$null
if ($apiRunning) {
    Write-Host "API is running" -ForegroundColor Green
} else {
    Write-Host "API container exists but may not be running yet" -ForegroundColor Yellow
    Write-Host "Run 'docker compose logs api' to check" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services:" -ForegroundColor Yellow
Write-Host "  API:         http://localhost:8080"
Write-Host "  Health:      http://localhost:8080/health"
Write-Host "  SQL Server:  localhost:1433 (user: sa)"
Write-Host "  Redis:       localhost:6379"
Write-Host ""
Write-Host "Useful Commands:" -ForegroundColor Yellow
Write-Host "  View logs:      docker compose logs -f"
Write-Host "  Stop services:  docker compose down"
Write-Host "  Restart:        docker compose restart"
Write-Host "  Check health:   docker compose ps"
Write-Host ""
Write-Host "Check status:" -ForegroundColor Yellow
Write-Host "  docker compose ps"
Write-Host ""

# Show current status
docker compose ps

Write-Host ""
Write-Host "Tip: Run 'docker compose logs -f api' to watch API logs" -ForegroundColor Cyan
Write-Host ""
