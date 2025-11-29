# AICalendar - Quick Start Guide

## Prerequisites

Before you begin, ensure you have Docker installed:
- **Windows**: [Docker Desktop for Windows](https://docs.docker.com/desktop/install/windows-install/)
- **Mac**: [Docker Desktop for Mac](https://docs.docker.com/desktop/install/mac-install/)
- **Linux**: [Docker Engine](https://docs.docker.com/engine/install/)

## Installation

### Step 1: Download the Project

Clone the repository or download and extract the ZIP file:

```bash
git clone <repository-url>
cd AICalendar
```

### Step 2: Run the Setup Script

The setup script will automatically:
- Generate secure passwords for SQL Server and Redis
- Create your `.env` configuration file
- Build Docker containers
- Start all services

**Windows (PowerShell):**
```powershell
.\setup.ps1
```

**Mac / Linux / WSL:**
```bash
chmod +x setup.sh
./setup.sh
```

**Note for Windows users**: If you encounter an execution policy error, the script will handle it automatically. Alternatively, run PowerShell as Administrator first.

### Step 3: Verify Installation

Once setup completes, verify your installation:

```bash
docker compose ps
```

All services should show as "healthy". Access your application:

- **API**: http://localhost:8080
- **Health Check**: http://localhost:8080/health
- **Swagger Documentation**: http://localhost:8080/swagger
- **Hangfire Dashboard**: http://localhost:8080/hangfire

## Understanding Your Setup

### Generated Files

After setup, you'll have:

```
AICalendar/
├── .env                  # Your configuration with secure passwords (not tracked by git)
├── .env.example          # Template file (tracked by git)
└── .gitignore           # Ensures .env stays private
```

### Running Services

Three Docker containers will be running:

| Service | Container Name | Port | Description |
|---------|---------------|------|-------------|
| API | aicalendar-api | 8080 | .NET 8 Web API |
| Database | aicalendar-db | 1433 | SQL Server 2022 |
| Cache | aicalendar-redis | 6379 | Redis 7 |

### Data Persistence

Your data is stored in Docker volumes that persist across restarts:
- `aicalendar_mssql_data` - Database files
- `aicalendar_redis_data` - Redis cache data

## Essential Commands

### Viewing Logs

View logs from all services:
```bash
docker compose logs -f
```

View logs from a specific service:
```bash
docker compose logs -f api
docker compose logs -f db
docker compose logs -f redis
```

### Managing Services

Stop all services:
```bash
docker compose down
```

Restart all services:
```bash
docker compose restart
```

Check service status:
```bash
docker compose ps
```

Rebuild after code changes:
```bash
docker compose down
docker compose up --build
```

## Database Access

### Connection Details

Retrieve your database password:

**Windows (PowerShell):**
```powershell
Get-Content .env | Select-String "MSSQL_SA_PASSWORD"
```

**Mac / Linux:**
```bash
grep MSSQL_SA_PASSWORD .env
```

**Connection Information:**
- **Host**: localhost
- **Port**: 1433
- **Username**: sa
- **Password**: (found in .env file)
- **Database**: AICalendarDb

### Running Migrations

Execute database migrations:
```bash
docker exec -it aicalendar-api dotnet ef database update
```

## Redis Access

### Connection Details

Retrieve your Redis password:

**Windows (PowerShell):**
```powershell
Get-Content .env | Select-String "REDIS_PASSWORD"
```

**Mac / Linux:**
```bash
grep REDIS_PASSWORD .env
```

### Connect to Redis CLI

Access the Redis command-line interface:
```bash
docker exec -it aicalendar-redis redis-cli
```

Then authenticate:
```bash
AUTH <your-redis-password>
```

### Useful Redis Commands

```bash
PING                    # Test connection
KEYS *                  # List all keys
KEYS aicalendar:*       # List application keys
GET <key>               # Retrieve a value
INFO                    # View Redis statistics
FLUSHALL                # Clear all data (use with caution)
```

## Troubleshooting

### Docker Issues

**Docker is not running:**
- Start Docker Desktop (Windows/Mac)
- Start Docker service (Linux): `sudo systemctl start docker`

**Containers won't start:**
```bash
# View detailed logs
docker compose logs

# Clean restart
docker compose down -v
```

Then run the setup script again.

### Permission Issues

**Mac / Linux - Permission denied on setup script:**
```bash
chmod +x setup.sh
./setup.sh
```

**Windows - Execution policy error:**

Run PowerShell as Administrator and execute:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Service Health Checks

Check if services are healthy:
```bash
docker inspect aicalendar-db | grep -A 5 Health
docker inspect aicalendar-redis | grep -A 5 Health
```

**Windows (PowerShell):**
```powershell
docker inspect aicalendar-db | Select-String -Pattern "Health" -Context 0,5
```

### Connection Problems

**Can't connect to database:**
1. Verify password in `.env` matches your connection string
2. Check if SQL Server container is healthy: `docker compose ps`
3. Review SQL Server logs: `docker compose logs db`

**Can't connect to Redis:**
1. Verify password in `.env`
2. Test connection: `docker exec -it aicalendar-redis redis-cli PING`
3. Review Redis logs: `docker compose logs redis`

## Resetting Your Environment

### Restart Without Losing Data
```bash
docker compose restart
```

### Fresh Start (Keeps Passwords)
```bash
docker compose down -v
docker compose up -d
```

### Complete Reset (New Passwords)

**Windows (PowerShell):**
```powershell
docker compose down -v
Remove-Item .env
.\setup.ps1
```

**Mac / Linux:**
```bash
docker compose down -v
rm .env
./setup.sh
```

## Security Notes

- **Never commit `.env` to version control** - It contains sensitive passwords
- **Passwords are auto-generated** using cryptographic randomness
- **Keep your `.env` file secure** - Treat it like a password
- **For production**, use environment-specific configurations and secrets management

## Next Steps

1. Explore the API documentation at http://localhost:8080/swagger
2. Check the health endpoint at http://localhost:8080/health
3. Monitor background jobs at http://localhost:8080/hangfire
4. Review additional documentation in the `docs/` folder
5. Start developing - code changes are automatically reloaded

## Getting Help

If you encounter issues:

1. Check service status: `docker compose ps`
2. Review logs: `docker compose logs -f`
3. Try a clean restart: `docker compose down -v` then run setup script
4. Verify Docker Desktop is running (Windows/Mac)
5. Check the documentation in the `docs/` folder

---

**You're all set! Happy coding! 🚀**
