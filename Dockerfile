# Dockerfile - Multi-stage build for .NET 8 API
# Works on: Windows, Mac, Linux

# ============================================================================
# Stage 1: Base - Runtime dependencies
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# ============================================================================
# Stage 2: Build - Restore and build the application
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution file
COPY ["AICalendar.sln", "./"]

# Copy project files (for layer caching - only restore if .csproj changes)
COPY ["src/AICalendar.API/AICalendar.API.csproj", "src/AICalendar.API/"]
COPY ["src/AICalendar.Application/AICalendar.Application.csproj", "src/AICalendar.Application/"]
COPY ["src/AICalendar.Domain/AICalendar.Domain.csproj", "src/AICalendar.Domain/"]
COPY ["src/AICalendar.Infrastructure/AICalendar.Infrastructure.csproj", "src/AICalendar.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/AICalendar.API/AICalendar.API.csproj"

# Install EF Core tools for migrations (development only)
RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"


# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/AICalendar.API"
RUN dotnet build "AICalendar.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore

# ============================================================================
# Stage 3: Publish - Create production build
# ============================================================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AICalendar.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================================================
# Stage 4: Final - Production image
# ============================================================================
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user for security
RUN groupadd -r appuser && \
    useradd -r -g appuser appuser && \
    chown -R appuser:appuser /app

USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AICalendar.API.dll"]
