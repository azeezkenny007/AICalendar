@echo off
if "%1"=="" (
    echo Error: Migration name is required.
    echo Usage: migrate MigrationName
    exit /b 1
)

echo Creating migration: %1
echo Running inside Docker container...

docker compose exec api dotnet ef migrations add %1 --project /src/src/AICalendar.Infrastructure/AICalendar.Infrastructure.csproj --startup-project /src/src/AICalendar.API/AICalendar.API.csproj

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Migration created successfully!
    echo To apply this migration, run: ./update-database.bat
) else (
    echo.
    echo ❌ Migration creation failed!
)
