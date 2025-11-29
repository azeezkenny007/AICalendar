@echo off
if "%1"=="" (
    echo Error: Migration name is required.
    echo Usage: migrate MigrationName
    exit /b 1
)
dotnet ef migrations add %1 --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
