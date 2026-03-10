# Development Guide

Everything you need to develop with this project, including workspace container setup, Makefile commands, and workflows.

> For architecture and design patterns, see [ARCHITECTURE.md](ARCHITECTURE.md).

## Prerequisites

- **Docker** and **Docker Compose**
- **AWS CLI** (optional, for infrastructure deployment)

All development tools (.NET SDK, AWS CLI, golang-migrate) are provided by the **workspace container** — no local .NET installation required.

## Getting Started

1. Clone the repository:

```bash
git clone <repository-url>
cd dotnet-postgres-template
```

2. Build and start the workspace container:

```bash
make workspace-build
make workspace-up
```

3. Run database migrations:

```bash
make migrate
```

4. Start the API server (with hot reload):

```bash
make run-api
```

The API is available at `http://localhost:8080`. API docs are at `http://localhost:8080/docs`.

## Workspace Container

The workspace container ensures a consistent development environment. It includes:

- .NET 10 SDK
- golang-migrate (database migrations)
- AWS CLI v2
- PostgreSQL client

All `make` commands execute inside the container. The source directory is mounted at `/workspace` and the container build cache lives at `/tmp/build` (a named Docker volume), keeping it separate from the host's `obj/` and `bin/` directories so both host IDE and container builds work simultaneously.

## Makefile Commands

### Workspace

| Command | Description |
|---------|-------------|
| `make workspace-build` | Build the workspace container image |
| `make workspace-up` | Start the workspace container |
| `make workspace-down` | Stop and remove the workspace container |
| `make workspace-shell` | Open a shell in the workspace container |

### Development

| Command | Description |
|---------|-------------|
| `make restore` | Restore NuGet packages |
| `make build` | Build the solution |
| `make clean` | Clean build artifacts |
| `make test` | Run tests |
| `make lint` | Check code formatting (`dotnet format --verify-no-changes`) |
| `make fix` | Fix code formatting (`dotnet format`) |

### Running Services

| Command | Description |
|---------|-------------|
| `make run-api` | Run the API with hot reload (`dotnet watch`) |
| `make run-worker` | Run the worker with hot reload |

### NuGet Packages

| Command | Description |
|---------|-------------|
| `make add PROJECT=src/Api PKG=Serilog` | Add a NuGet package |
| `make remove PROJECT=src/Api PKG=Serilog` | Remove a NuGet package |

### Database Migrations

Migrations are managed with [golang-migrate](https://github.com/golang-migrate/migrate) and live in `resources/db/migrations/`.

| Command | Description |
|---------|-------------|
| `make migrate` | Run all pending migrations |
| `make migrate-down` | Rollback the last migration |
| `make migrate-version` | Show current migration version |
| `make migrate-force VERSION=1` | Force-set the migration version |
| `make migrate-create NAME=add_orders` | Create a new migration pair |

### LocalStack

| Command | Description |
|---------|-------------|
| `make localstack-up` | Start LocalStack (SNS/SQS) |
| `make localstack-setup` | Create SNS topics and SQS queues |
| `make localstack-down` | Stop LocalStack |
| `make localstack-logs` | Tail LocalStack logs |

### Production

| Command | Description |
|---------|-------------|
| `make build-image` | Build the production Docker image |
| `make build-migrations` | Build the migrations Docker image |

## Development Workflow

1. Start the workspace: `make workspace-up`
2. Make code changes (hot reload picks them up automatically with `make run-api`)
3. Check formatting: `make lint`
4. Run tests: `make test`
5. Fix formatting issues: `make fix`

## Testing

Tests use **xUnit** with **NSubstitute** for mocking. Domain services and validators are tested with mocked repository interfaces — no database required.

```bash
make test
```

Test files live under `tests/Domain.Tests/` organized by domain:

```
tests/Domain.Tests/
└── Users/
    └── UserValidatorsTests.cs
```

## Docker Services

| Service | Port | Description |
|---------|------|-------------|
| `postgres` | 5432 | PostgreSQL 16 database |
| `localstack` | 4566 | AWS LocalStack (SNS/SQS) |
| `workspace` | 8080 | Development container |
| `migrate` | — | One-shot migration runner |

## Host / Container Build Cache

The container redirects build output to `/tmp/build` via `Directory.Build.props` (activated by `DOTNET_BUILD_DIR` env var). This prevents the container's `obj/` and `bin/` from conflicting with the host IDE's build cache on the mounted source volume.
