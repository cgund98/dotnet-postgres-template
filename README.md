# dotnet-postgres-template

A .NET 10 REST API template with PostgreSQL, built with a pragmatic layered architecture combining ideas from hexagonal, DDD, and 3-tier patterns.

## Stack

| Category | Tools |
|----------|-------|
| Runtime | .NET 10 / ASP.NET Core Minimal APIs |
| Database | PostgreSQL 16, Dapper, Npgsql |
| Messaging | SNS FIFO → SQS FIFO (AWS SDK), CloudEvents v1.0 |
| Validation | FluentValidation (API) + domain validators |
| Logging | Serilog (structured, JSON in production) |
| API Docs | OpenAPI + Scalar UI (`/docs`) |
| Testing | xUnit + NSubstitute |
| Migrations | golang-migrate |
| Dev Environment | Docker Compose, LocalStack |
| Linting | Roslynator + `dotnet format` |

## Project Structure

```
src/
├── Api/                  # Minimal API (composition root)
│   ├── Common/           # Shared contracts, validation filter
│   ├── Middleware/        # Exception handler (Problem Details)
│   └── Users/            # Endpoints + request/response DTOs
├── Domain/               # Business logic (no external deps except Serilog)
│   ├── Events/           # Interfaces, envelope, router, handler registration
│   ├── Exceptions/       # Domain + event exception types
│   ├── Persistence/      # IDbContext, ITransactionManager
│   └── Users/            # Model, commands, service, validators, events
├── Infrastructure/       # Implementations (Dapper, Npgsql, AWS SDK)
│   ├── Events/Sns/       # SnsEventPublisher
│   ├── Events/Sqs/       # SqsEventConsumer
│   ├── Persistence/      # DbContext, TransactionManager
│   └── Users/            # UserRepository (SQL)
└── Worker/               # Background event consumer
    └── Users/            # Typed event handlers

tests/
└── Domain.Tests/         # Unit tests with mocked dependencies

resources/
├── db/migrations/        # SQL migration files
├── docker/               # Dockerfiles (workspace, app, migrate)
└── scripts/              # Helper scripts (LocalStack setup)
```

## Quick Start

### Development

```bash
make workspace-build    # Build the dev container
make workspace-up       # Start Postgres, LocalStack, workspace
make localstack-setup   # Create SNS topics and SQS queues
make migrate            # Run database migrations
make run-api            # Start API with hot reload on :8080
make run-worker         # Start worker with hot reload (separate terminal)
```

### Production

```bash
make app-up             # Build image and run API + worker containers
make app-down           # Stop both
```

## Key Features

### API

- **RFC 9457 Problem Details** for all error responses
- **Two-layer validation** — FluentValidation at the API boundary, domain validators for business rules

### Data

- **Ambient scoped DbContext** — repositories share connection/transaction without explicit parameter passing
- **Transaction management** — mutations wrapped in `TransactionAsync` with automatic commit/rollback

### Messaging

- **CloudEvents v1.0 envelopes** over SNS FIFO → SQS FIFO
- **Typed handler routing** — `EventRouter` dispatches to `IEventHandler<T>` with automatic deserialization
- **FIFO ordering** — messages grouped by aggregate ID for per-entity ordering

### Developer Experience

- **Separate build caches** — host and container can build simultaneously without conflicts
- **LocalStack** — local SNS/SQS development without an AWS account

## Potential Improvements

- **Transactional Outbox** — events are currently published after commit (fire-and-forget). For guaranteed delivery, write events to an outbox table within the same transaction and relay them to the broker via a background process.

## Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) — Design patterns, layer responsibilities, conventions
- [DEVELOPMENT.md](DEVELOPMENT.md) — Setup, Makefile commands, workflows
