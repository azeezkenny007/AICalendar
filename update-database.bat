@echo off
if "%1"=="" (
    echo Applying LATEST migrations to database...
) else (
    echo Reverting/Updating database to migration: %1
)
echo.

docker compose exec api dotnet ef database update %1 --project /src/src/AICalendar.Infrastructure/AICalendar.Infrastructure.csproj --startup-project /src/src/AICalendar.API/AICalendar.API.csproj

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Database updated successfully!
) else (
    echo.
    echo ❌ Database update failed!
    echo Make sure the Docker containers are running: docker compose up -d
)
