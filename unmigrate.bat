@echo off
echo Removing last migration inside Docker container...
echo.

docker compose exec api dotnet ef migrations remove %* --project /src/src/AICalendar.Infrastructure/AICalendar.Infrastructure.csproj --startup-project /src/src/AICalendar.API/AICalendar.API.csproj

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Last migration removed successfully!
) else (
    echo.
    echo ❌ Failed to remove migration!
    echo Make sure the Docker containers are running: docker compose up -d
)
