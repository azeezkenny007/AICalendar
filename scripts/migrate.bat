@echo off
if "%1"=="" (
    echo Error: Migration name is required.
    echo Usage: .\scripts\migrate.bat MigrationName
    exit /b 1
)

echo Creating migration: %1
dotnet ef migrations add %1 --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API

echo.
echo Migration created. Docker will automatically apply it when the API restarts.
