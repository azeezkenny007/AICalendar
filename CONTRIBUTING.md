# Contributing to AICalendar

Welcome! Thanks for contributing to AICalendar. To keep our codebase stable, we've adopted a **branch protection workflow**: All changes must go through pull requests (PRs) to the `main` branch. Direct pushes to `main` are blocked to prevent accidental breaks.

## Quick Start for Contributors

If you're new or need a refresher, follow these steps. (Assumes you have Git installed and your repo cloned.)

### 1. Clone the Repo (If Not Already Done)

```bash
git clone https://github.com/azeezkenny007/AICalendar.git
cd AICalendar
```

### 2. Set Up Your Remote (If Forked)

If you've forked the repository, add the upstream remote:

```bash
git remote add upstream https://github.com/azeezkenny007/AICalendar.git
git fetch upstream
```

### 3. Always Work from the `main` Branch

Sync your local `main` with the latest:

```bash
git checkout main
git pull origin main  # Or 'git pull upstream main' if using upstream
```

### 4. Create a Feature Branch

For every new task/bug/fix, create a branch from `main`. Use descriptive names (e.g., `feature/user-login` or `fix/bug-123`).

```bash
git checkout -b feature/your-branch-name
```

### 5. Make Your Changes

- Edit files, add commits with clear messages: `git commit -m "Add user login feature"`.
- Build and test locally:
  ```bash
  dotnet build
  dotnet test
  ```

### 6. Push Your Branch

```bash
git push origin feature/your-branch-name
```

### 7. Open a Pull Request (PR)

1. Go to the repo on [GitHub.com](https://github.com/azeezkenny007/AICalendar).
2. You'll see a prompt like "Compare & pull request"—click it.
3. **Base branch**: Select `main`.
4. Add a title (e.g., "Add user login feature") and description (what/why/how).
5. Click **Create pull request**.

### 8. Review & Merge

- The maintainer will review your PR. Address feedback in comments or new commits.
- Once approved (and checks pass, if any), it merges to `main`.
- After merge: Delete your branch on GitHub, then clean up locally:
  ```bash
  git checkout main
  git pull origin main
  git branch -d feature/your-branch-name  # Local delete
  ```

## Development Setup

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server LocalDB
- Your favorite IDE ([Visual Studio](https://visualstudio.microsoft.com/), [VS Code](https://code.visualstudio.com/), or [Rider](https://www.jetbrains.com/rider/))

### Build & Run

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project src/AICalendar.API

# Run tests
dotnet test
```

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add YourMigrationName --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API

# Update database
dotnet ef database update --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

## Project Structure

This project follows **Clean Architecture** principles:

- **AICalendar.Domain** - Core business entities and logic
- **AICalendar.Application** - Use cases, business logic, and MediatR handlers
- **AICalendar.Infrastructure** - Data access, external services, EF Core configuration
- **AICalendar.API** - Web API controllers and presentation layer

## Best Practices

- **Keep Branches Short-Lived**: Merge often to avoid conflicts.
- **Commit Often**: Small, atomic changes are easier to review.
- **Follow Clean Architecture**: Respect the dependency rules (Domain → Application → Infrastructure/API).
- **Write Tests**: Add unit tests for new features when applicable.
- **Lint & Test**: Run `dotnet build` and `dotnet test` before pushing.
- **Questions?** Ping in a PR comment or open an issue.

## Common Errors & Fixes

- **"Push rejected" to `main`?** That's expected—use PRs instead!
- **Conflicts?** Pull latest `main`, merge it into your branch (`git merge main`), resolve, then push.
- **No Write Access?** Fork the repo, work there, and PR from your fork.
- **Build Errors?** Make sure you have .NET 8.0 SDK installed and all dependencies restored.

## Code Style

- Follow standard C# naming conventions (PascalCase for classes/methods, camelCase for parameters)
- Use meaningful variable and method names
- Keep methods focused and single-purpose
- Add XML documentation comments for public APIs

Let's build awesome stuff together! If this workflow changes, we'll update here.
