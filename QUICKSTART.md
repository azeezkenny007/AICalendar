# AICalendar - Quick Start Guide

## 1. Prerequisites

- **Docker Desktop**: [Download Here](https://www.docker.com/products/docker-desktop/)
- **Git**: [Download Here](https://git-scm.com/downloads)

## 2. Initial Setup

1.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    cd AICalendar
    ```

2.  **Run the setup script:**
    *   **Windows (PowerShell):** `.\setup.ps1`
    *   **Mac/Linux:** `chmod +x setup.sh && ./setup.sh`

    *This generates secure passwords in a `.env` file, builds containers, and starts the app.*

3.  **Verify it works:**
    *   **API Health:** http://localhost:8080/health
    *   **Swagger UI:** http://localhost:8080/swagger
    *   **Hangfire Dashboard:** http://localhost:8080/hangfire

## 3. Development Workflow

### Managing the Application
| Action | Command |
|--------|---------|
| **Start App** | `docker compose up -d` |
| **Stop App** | `docker compose down` |
| **View Logs** | `docker compose logs -f` (or `-f api` for just API) |
| **Rebuild** | `docker compose up -d --build` |

### Database Migrations
We use helper scripts to run migrations **inside the Docker container**.

| Action | Command |
|--------|---------|
| **Create Migration** | `.\migrate.bat <MigrationName>` |
| **Apply to DB** | `.\update-database.bat` |
| **Revert DB** | `.\update-database.bat <TargetMigrationName>` |
| **Remove Last Migration** | `.\unmigrate.bat` (Removes code only) |

*Note: To undo a migration completely, first revert the DB using `update-database.bat`, then remove the code using `unmigrate.bat`.*

## 4. Accessing Data

### SQL Server (Database)
Use **Azure Data Studio** or **SSMS**.

*   **Server:** `localhost,1433`
*   **User:** `sa`
*   **Password:** *(Found in your `.env` file under `MSSQL_SA_PASSWORD`)*
*   **Database:** `AICalendarDb`
*   **Trust Server Certificate:** ✅ **True** (Required)

### Redis (Cache)
Use **Redis Insight** for the best experience.

*   **Download:** [Redis Insight](https://redis.io/insight/)
*   **Host:** `localhost`
*   **Port:** `6379`
*   **Password:** *(Found in your `.env` file under `REDIS_PASSWORD`)*

## 5. Configuration

*   **Secrets (`.env`)**: Passwords and sensitive keys are stored here. This file is **gitignored**.
*   **App Settings (`appsettings.json`)**: Contains structure and defaults.
*   **Local Development**: If running outside Docker, copy `appsettings.Development.json.template` to `appsettings.Development.json` and fill in secrets from `.env`.

## 6. Troubleshooting

*   **"Login failed for user 'sa'"**: Check your `.env` file for the correct password. Ensure you checked "Trust Server Certificate" in your SQL client.
*   **"Read-only file system"**: Ensure your `docker-compose.yml` volume mount for `./src` does NOT have `:ro` at the end.
*   **Migration fails**: Ensure containers are running (`docker compose up -d`).

---
**Happy Coding! 🚀**
