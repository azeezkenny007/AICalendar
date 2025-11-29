@echo off
dotnet ef migrations remove --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
