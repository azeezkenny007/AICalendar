# AICalendar - Predictive Calendar System

A modern, AI-powered calendar application that analyzes transaction history to generate personalized calendar suggestions for recurring payments. Built with Clean Architecture, CQRS, and event-driven patterns for scalability and maintainability.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
- [Development](#development)
- [API Documentation](#api-documentation)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## Overview

AICalendar is a sophisticated predictive calendar service that leverages machine learning and transaction pattern analysis to help users manage recurring payments. The system identifies spending patterns and suggests upcoming calendar events based on historical transaction data.

### Use Cases

- Automatically schedule reminders for recurring payments
- Predict subscription renewal dates
- Identify billing patterns across merchants
- Optimize budget planning with AI-driven insights

## Key Features

- **AI-Powered Predictions**: Detects recurring patterns (monthly, weekly, bi-weekly) in transaction data
- **Pattern Recognition**: Groups transactions by merchant/receiver ID for accurate pattern matching
- **User Feedback Loop**: Learns from user feedback (Keep/Discard/Edit) to improve prediction accuracy
- **Real-Time Notifications**: Push notifications via Firebase, Azure Notification Hub, or hybrid approach
- **Background Jobs**: Hangfire integration for scheduled tasks and async processing
- **Event-Driven Architecture**: Outbox pattern for reliable event publishing
- **gRPC & REST APIs**: Dual API support for flexible client integration
- **Monitoring & Observability**: Prometheus metrics, Grafana dashboards, and structured logging
- **Multi-Container Deployment**: Docker Compose for local development, Azure Container Apps for production

## Technology Stack

### Backend
- **Runtime**: .NET 8
- **Architecture**: Clean Architecture + CQRS + MediatR
- **Database**: SQL Server with Entity Framework Core
- **Caching**: Redis
- **Message Queue**: Hangfire (background jobs)
- **APIs**: REST (Swagger/OpenAPI), gRPC

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Orchestration**: Azure Container Apps (production), Docker Compose (development)
- **Monitoring**: Prometheus + Grafana
- **Notifications**: Firebase Cloud Messaging, Azure Notification Hub
- **CI/CD**: Git-based workflow with branch protection

### Testing
- xUnit for unit tests
- Integration tests with test containers
- Architecture tests for design validation

## Prerequisites

Before you begin, ensure you have the following installed:

- **Docker Desktop** ([Download](https://www.docker.com/products/docker-desktop/)) - For containerized development
- **Git** ([Download](https://git-scm.com/downloads)) - For version control
- **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download)) - Optional, for local development
- **Visual Studio 2022** or **VS Code** - For development
- **PowerShell 5.1+** (Windows) or Bash (Mac/Linux) - For scripts

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/azeezkenny007/AICalendar.git
cd AICalendar
git checkout dev
```

### 2. Run Setup Script

The setup script generates secure passwords, creates the `.env` file, and starts all services.

**Windows (PowerShell):**
```powershell
.\setup.ps1
```

**Mac/Linux:**
```bash
chmod +x setup.sh && ./setup.sh
```

### 3. Verify the Installation

- **API Health Check**: http://localhost:8080/health
- **Swagger UI**: http://localhost:8080/swagger
- **Hangfire Dashboard**: http://localhost:8080/hangfire

### 4. Start Development

The application is now running with hot reload enabled. Make changes to your code and see them reflected instantly.

## Project Structure

```
AICalendar/
├── src/                              # Source code
│   ├── AICalendar.Domain/           # Domain entities and business rules
│   ├── AICalendar.Application/      # Business logic, CQRS commands/queries
│   ├── AICalendar.Infrastructure/   # Data access, external services
│   └── AICalendar.API/              # REST/gRPC endpoints, middleware
├── tests/                            # Test projects
│   ├── AICalendar.Domain.UnitTests/
│   ├── AICalendar.Application.UnitTests/
│   ├── AICalendar.Infrastructure.IntegrationTests/
│   ├── AICalendar.API.IntegrationTests/
│   └── AICalendar.ArchitectureTests/
├── docs/                             # Documentation
│   ├── architecture/                 # ADRs and system design
│   ├── api/                         # API guides and specs
│   ├── database/                    # Database schemas and migrations
│   └── deployment/                  # Deployment guides
├── scripts/                          # Utility scripts
│   ├── migrate.bat                  # Create migrations
│   ├── update-database.bat          # Apply migrations
│   └── unmigrate.bat                # Remove migrations
├── docker-compose.yml               # Local development environment
├── Dockerfile                       # Container definition
└── AICalendar.sln                  # Solution file
```

## Architecture

AICalendar follows industry-leading architectural patterns:

### Clean Architecture
- **Domain Layer**: Core business logic and entities
- **Application Layer**: Use cases, CQRS commands/queries, validators
- **Infrastructure Layer**: Data access, external integrations, repositories
- **API Layer**: REST and gRPC endpoints, middleware, error handling

### CQRS Pattern
- **Commands**: State-changing operations (Create, Update, Delete)
- **Queries**: Read operations (optimized for performance)
- **MediatR**: Decouples commands/queries from handlers

### Event-Driven Architecture
- **Outbox Pattern**: Ensures reliable event publishing across domain boundaries
- **Event Sourcing**: Track all state changes as events
- **Background Jobs**: Hangfire processes asynchronous tasks

### See Also
- [System Architecture Overview](docs/architecture/SystemArchitecture.md)
- [Architecture Decision Records (ADRs)](docs/architecture/)

## Development

### Managing the Application

| Action | Command |
|--------|---------|
| **Start** | `docker compose up -d` |
| **Stop** | `docker compose down` |
| **View Logs** | `docker compose logs -f` |
| **Rebuild** | `docker compose up -d --build` |
| **Enter API Container** | `docker compose exec api sh` |

### Database Migrations

Helper scripts simplify database management:

| Action | Command |
|--------|---------|
| **Create Migration** | `.\migrate.bat <MigrationName>` |
| **Apply Migrations** | `.\update-database.bat` |
| **Revert to Target** | `.\update-database.bat <TargetMigrationName>` |
| **Remove Code Only** | `.\unmigrate.bat` |

**Note**: To completely undo a migration:
1. Revert the database: `.\update-database.bat <PreviousMigrationName>`
2. Remove the migration code: `.\unmigrate.bat`

### Running Tests

```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/AICalendar.Application.UnitTests/

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=lcov
```

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release
```

## API Documentation

### REST API
- **Swagger UI**: http://localhost:8080/swagger
- **OpenAPI Spec**: [API_Doc.md](API_Doc.md)
- **Detailed Guides**: [docs/api/](docs/api/)

### Key Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/predictions` | Generate calendar predictions |
| `POST` | `/api/feedback` | Submit user feedback |
| `GET` | `/api/health` | Health check |
| `GET` | `/hangfire` | Background job dashboard |

### Authentication
All API requests require authentication (see API documentation for details).

## Deployment

### Local Development
- **Environment**: Docker Compose
- **Services**: API, SQL Server, Redis, Hangfire, Prometheus, Grafana
- **Setup**: Run `setup.ps1` or `setup.sh`

### Production Environments
- **Azure Container Apps**: [AZURE_CONTAINER_APPS_DEPLOYMENT](docs/deployment/)
- **Render Platform**: [RENDER_DEPLOYMENT_GUIDE.md](RENDER_DEPLOYMENT_GUIDE.md)
- **Docker**: [MULTI_CONTAINER_DEPLOYMENT_GUIDE.md](MULTI_CONTAINER_DEPLOYMENT_GUIDE.md)

### Environment Variables
Create a `.env` file in the root directory (auto-generated by setup scripts):

```env
# Database
SQL_SA_PASSWORD=<secure-password>
SQL_CONNECTION_STRING=Server=db;Database=AICalendarDb;...

# Redis
REDIS_PASSWORD=<secure-password>
REDIS_CONNECTION_STRING=redis:6379,...

# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
```

## Contributing

We follow a strict branch protection workflow. **All changes must go through pull requests** to ensure code quality and stability.

### Contributing Workflow

1. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Your Changes**
   - Write clean, well-documented code
   - Add unit tests for new functionality
   - Update documentation as needed

3. **Build and Test Locally**
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit with Clear Messages**
   ```bash
   git commit -m "Add feature: description of changes"
   ```

5. **Push and Create Pull Request**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Code Review**
   - Respond to reviewer feedback
   - Make requested changes
   - Pass all CI/CD checks

### Code Standards
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests (aim for >80% coverage)
- Keep commits atomic and focused

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## Monitoring & Observability

### Prometheus Metrics
- **Endpoint**: http://localhost:9090
- **Configuration**: [monitoring/prometheus.yml](monitoring/prometheus.yml)
- **Metrics collected**: Request latency, error rates, database queries, Redis operations

### Grafana Dashboards
- **Access**: http://localhost:3000
- **Default Credentials**: admin/admin
- **Pre-built Dashboards**: API performance, database health, system resources

### Logs
- **Format**: Structured logging with Serilog
- **Output**: Console + File (in container logs)
- **View**: `docker compose logs -f [service-name]`

## Troubleshooting

### Common Issues

**Issue**: Docker containers won't start
```bash
# Clean up and restart
docker compose down -v
docker compose up --build
```

**Issue**: Database connection errors
```bash
# Check container status
docker compose ps

# View logs
docker compose logs db
```

**Issue**: Port already in use
- Change ports in `docker-compose.yml`
- Or stop other services using those ports

**Issue**: Migrations failing
```bash
# Revert to clean state
.\update-database.bat -MigrationName 0

# Then reapply
.\update-database.bat
```

### Getting Help
- Check [docs/](docs/) for detailed guides
- Review [QUICKSTART.md](QUICKSTART.md) for setup help
- Open an issue on GitHub
- Contact the team

## Additional Resources

- [API Documentation](API_Doc.md)
- [Quick Start Guide](QUICKSTART.md)
- [Architecture Guides](docs/architecture/)
- [Deployment Guides](docs/deployment/)
- [Background Jobs Guide](docs/BackgroundJobs.md)
- [Notification Systems](docs/HYBRID_NOTIFICATION_IMPLEMENTATION.md)

## License

This project is licensed under the [LICENSE](LICENSE) file (if applicable). See the LICENSE file for details.

---

**Last Updated**: January 2026  
**Version**: 1.0.0  
**Repository**: [azeezkenny007/AICalendar](https://github.com/azeezkenny007/AICalendar)
