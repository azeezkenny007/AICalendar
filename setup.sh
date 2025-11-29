#!/bin/bash
# setup.sh - Quick setup script for AICalendar
# Works on: Mac, Linux, WSL (Windows)

set -e  # Exit on error

echo "🚀 AICalendar - Setup Script"
echo "================================"
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    echo "   Visit: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check if Docker Compose is available
if ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose is not available. Please update Docker."
    exit 1
fi

echo "✅ Docker is installed"
echo ""

# Create .env file if it doesn't exist
if [ ! -f .env ]; then
    echo "📝 Creating .env file..."
    cp .env.example .env

    # Generate random password for SQL Server
    MSSQL_PASSWORD="Strong@$(openssl rand -base64 12 | tr -d '/+=' | head -c 12)1"

    # Generate random password for Redis
    REDIS_PASSWORD="Redis@$(openssl rand -base64 16 | tr -d '/+=' | head -c 16)2"

    # Update .env with generated passwords
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        sed -i '' "s/YourStrong@Passw0rd/$MSSQL_PASSWORD/g" .env
        sed -i '' "s/YourRedis@Passw0rd/$REDIS_PASSWORD/g" .env
    else
        # Linux/WSL
        sed -i "s/YourStrong@Passw0rd/$MSSQL_PASSWORD/g" .env
        sed -i "s/YourRedis@Passw0rd/$REDIS_PASSWORD/g" .env
    fi

    echo "✅ Created .env file with generated passwords"
    echo "   📊 SQL Server password: $MSSQL_PASSWORD"
    echo "   🔐 Redis password: $REDIS_PASSWORD"
    echo ""
    echo "   💡 Passwords are saved in .env file (gitignored)"
    echo ""
else
    echo "✅ .env file already exists"
    echo ""
fi

# Stop any running containers
echo "🛑 Stopping existing containers..."
docker compose down -v 2>/dev/null || true
echo ""

# Build containers
echo "🏗️  Building containers (this may take a few minutes)..."
docker compose build --no-cache
echo ""

# Start services
echo "🚀 Starting services..."
docker compose up -d
echo ""

# Wait for services to be healthy
echo "⏳ Waiting for services to be healthy..."
echo "   This may take 30-60 seconds..."
echo ""

MAX_WAIT=60
ELAPSED=0

while [ $ELAPSED -lt $MAX_WAIT ]; do
    # Check health status
    DB_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' aicalendar-db 2>/dev/null || echo "starting")
    REDIS_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' aicalendar-redis 2>/dev/null || echo "starting")

    if [ "$DB_HEALTH" = "healthy" ] && [ "$REDIS_HEALTH" = "healthy" ]; then
        echo "✅ All services are healthy!"
        break
    fi

    echo "   Waiting... (${ELAPSED}s) - DB: $DB_HEALTH | Redis: $REDIS_HEALTH"
    sleep 5
    ELAPSED=$((ELAPSED + 5))
done

echo ""

# Check if API is running
if docker ps | grep -q aicalendar-api; then
    echo "✅ API is running"
else
    echo "⚠️  API container exists but may not be running yet"
    echo "   Run 'docker compose logs api' to check"
fi

echo ""
echo "================================"
echo "🎉 Setup Complete!"
echo "================================"
echo ""
echo "📋 Services:"
echo "   • API:         http://localhost:8080"
echo "   • Health:      http://localhost:8080/health"
echo "   • SQL Server:  localhost:1433 (user: sa)"
echo "   • Redis:       localhost:6379"
echo ""
echo "🔧 Useful Commands:"
echo "   • View logs:      docker compose logs -f"
echo "   • Stop services:  docker compose down"
echo "   • Restart:        docker compose restart"
echo "   • Check health:   docker compose ps"
echo ""
echo "📊 Check status:"
echo "   docker compose ps"
echo ""

# Show current status
docker compose ps

echo ""
echo "💡 Tip: Run 'docker compose logs -f api' to watch API logs"
echo ""
