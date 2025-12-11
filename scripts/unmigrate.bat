@echo off
echo Removing last migration...
dotnet ef migrations remove --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API

echo.
echo Last migration removed.
