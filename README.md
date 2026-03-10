# dotnet-postgres-template

A .NET 10 REST API template with PostgreSQL, built with a pragmatic layered architecture combining ideas from hexagonal, DDD, and 3-tier patterns.

## Stack

- **Runtime**: .NET 10 / ASP.NET Core Minimal APIs
- **Database**: PostgreSQL 16 + Dapper (micro-ORM) + Npgsql
- **Validation**: FluentValidation (API) + domain validators
- **Logging**: Serilog (structured, JSON in production)
- **API Docs**: OpenAPI + Scalar UI (`/docs`)
- **Testing**: xUnit + NSubstitute
- **Migrations**: golang-migrate
- **Dev Environment**: Docker Compose workspace container
- **Linting**: Roslynator + `dotnet format`

## Project Structure

```
src/
├── Api/                  # ASP.NET Core Minimal API (composition root)
│   ├── Common/           # Shared contracts, validation filter
│   ├── Middleware/        # Exception handler (Problem Details)
│   └── Users/            # User endpoints + request/response DTOs
├── Domain/               # Pure business logic (no dependencies)
│   ├── Exceptions/       # Domain exception types
│   ├── Persistence/      # IDbContext, ITransactionManager interfaces
│   └── Users/            # User model, commands, service, validators, repo interface
├── Infrastructure/       # Implementations (Dapper, Npgsql)
│   ├── Persistence/      # DbContext, TransactionManager
│   └── Users/            # UserRepository (SQL)
└── Worker/               # Background job processing

tests/
└── Domain.Tests/         # Unit tests with mocked dependencies
    └── Users/

resources/
├── db/migrations/        # SQL migration files
├── docker/               # Dockerfiles (workspace, app, migrate)
└── scripts/              # Helper scripts
```

## Quick Start

```bash
make workspace-build    # Build the dev container
make workspace-up       # Start Postgres, LocalStack, workspace
make migrate            # Run database migrations
make run-api            # Start API with hot reload on :8080
```

## Key Features

- **RFC 9457 Problem Details** for all error responses
- **Two-layer validation**: FluentValidation at the API boundary, domain validators for business rules
- **Ambient scoped DbContext**: repositories share connection/transaction without explicit parameter passing
- **Transaction management**: mutations wrapped in `TransactionAsync` with automatic commit/rollback
- **Separate build caches**: host and container can build simultaneously without conflicts

## Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) — Design patterns, layer responsibilities, conventions
- [DEVELOPMENT.md](DEVELOPMENT.md) — Setup, Makefile commands, workflows
