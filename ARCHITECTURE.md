# Architecture

This document describes the architecture, design patterns, and conventions used in this codebase.

## Overview

The application follows a pragmatic **3-tier architecture** with ideas borrowed from hexagonal (ports & adapters) and domain-driven design. Business logic lives in a pure Domain layer that defines interfaces (ports); Infrastructure provides the implementations (adapters); the API layer handles HTTP concerns.

```
┌──────────────────────────────────────────────────────┐
│                   API Layer (src/Api)                 │
│   Minimal API endpoints, contracts, validation,      │
│   middleware, OpenAPI docs                            │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────┐
│                 Domain Layer (src/Domain)             │
│   Services, models, repository interfaces,           │
│   validators, commands, exceptions                   │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────┐
│            Infrastructure Layer (src/Infrastructure)  │
│   Dapper repositories, Postgres connections,         │
│   transaction management                             │
└──────────────────────────────────────────────────────┘
```

Dependencies point inward: Api → Domain ← Infrastructure. The Domain layer has no external dependencies except `Serilog` for logging.

## Projects

| Project | Role | Key dependencies |
|---------|------|-----------------|
| `Api` | HTTP endpoints, DI composition root | ASP.NET Core, FluentValidation, Serilog, Scalar |
| `Domain` | Business logic, interfaces | Serilog |
| `Infrastructure` | Data access, event publishing/consuming | Dapper, Npgsql, AWS SDK (SNS, SQS) |
| `Worker` | Background event consumer | Microsoft.Extensions.Hosting, Serilog |
| `Domain.Tests` | Unit tests | xUnit, NSubstitute |

## Layer Responsibilities

### API Layer (`src/Api/`)

Handles HTTP concerns only. Organized by domain (e.g., `Users/`).

- **Endpoints** (`UserEndpoints.cs`): Minimal API route handlers grouped under `RouteGroupBuilder`
- **Contracts** (`UserContracts.cs`): Request/response records and FluentValidation validators
- **Common** (`Common/`): Shared contracts (`PaginationParams`, `PaginatedResponse`) and the `ValidationFilter`
- **Middleware** (`Middleware/`): `DomainExceptionHandler` maps domain exceptions to RFC 9457 Problem Details responses

### Domain Layer (`src/Domain/`)

Pure business logic with no infrastructure dependencies. Organized by domain.

- **Models** (`User.cs`): Immutable `record` types representing domain entities
- **Commands** (`Commands.cs`): Immutable `record` types for write operations
- **Services** (`UserService.cs`): Orchestrates validators, repositories, and transactions
- **Validators** (`UserValidators.cs`): Static methods for business rule validation (including async repository checks)
- **Repository interfaces** (`UserRepository.cs`): `IUserRepository` — the port that Infrastructure implements
- **Persistence interfaces** (`Persistence/`): `IDbContext` and `ITransactionManager`
- **Exceptions** (`Exceptions/`): `ValidationException`, `NotFoundException`, `DuplicateException`, `EventException`
- **Events** (`Events/`): `IEventPayload`, `IEventPublisher`, `IEventConsumer`, `IEventHandler<T>`, `EventEnvelope` (CloudEvents v1.0), `EventRouter`, `EventHandlerRegistration<T>`

### Infrastructure Layer (`src/Infrastructure/`)

Implements domain interfaces using concrete technologies.

- **Repositories** (`Users/UserRepository.cs`): Dapper-based SQL queries against `IDbContext`
- **DbContext** (`Persistence/Postgres/DbContext.cs`): Ambient scoped context holding the current `NpgsqlConnection` and `IDbTransaction`
- **TransactionManager** (`Persistence/Postgres/TransactionManager.cs`): Opens a connection, begins a transaction, sets it on `DbContext`, commits or rolls back
- **Event Publisher** (`Events/Sns/SnsEventPublisher.cs`): Publishes CloudEvents envelopes to SNS FIFO topics with message group and deduplication
- **Event Consumer** (`Events/Sqs/SqsEventConsumer.cs`): Long-polls SQS FIFO queues and routes messages through `EventRouter`

## Design Patterns

### Repository Pattern

Domain defines the interface; Infrastructure provides the implementation using Dapper + raw SQL.

```
Domain:         IUserRepository (interface)
                    ↑
Infrastructure: UserRepository (Dapper implementation)
```

Repositories receive `IDbContext` via DI and use `db.Connection` / `db.Transaction` for all queries.

### Ambient DbContext

A scoped `DbContext` holds the current connection and transaction for the request. Repositories read from it; `TransactionManager` writes to it. This avoids passing connection/transaction parameters through every method.

- **Reads**: Use the lazy connection (`_connection ??= dataSource.OpenConnection()`)
- **Writes**: `TransactionManager` opens a dedicated connection + transaction and sets them on `DbContext`

### Transaction Management

Mutations in services are wrapped in `transactionManager.TransactionAsync(...)`:

```csharp
return await transactionManager.TransactionAsync(async () =>
{
    await UserValidators.ValidateCreateUserAsync(command, userRepository);
    return await userRepository.CreateAsync(command);
});
```

The lambda runs inside a transaction. On success it commits; on exception it rolls back.

### Validation Strategy

Two layers of validation:

1. **API layer** — FluentValidation via a generic `ValidationFilter` applied to the `/api/v1` route group. Catches malformed requests before they reach the domain.
2. **Domain layer** — Static validator methods in `UserValidators` enforce business rules (uniqueness checks, field constraints). These run inside transactions when needed.

### Error Handling

All errors flow through `DomainExceptionHandler` (`IExceptionHandler`) and are returned as RFC 9457 **Problem Details**:

| Exception | Status | Title |
|-----------|--------|-------|
| `ValidationException` | 400 | Validation Error |
| `NotFoundException` | 404 | Not Found |
| `DuplicateException` | 409 | Conflict |
| `BadHttpRequestException` | 400 | Bad Request |
| Unhandled | 500 | Internal Server Error |

Only 500 errors are logged with the exception. Client-facing 500 responses never leak internal details.

### Event-Driven Messaging

Events use a CloudEvents v1.0 envelope published to SNS FIFO and consumed via SQS FIFO:

```
UserService (publish after commit)
  → IEventPayload.ToEnvelope() → EventEnvelope (CloudEvents)
  → SnsEventPublisher → SNS FIFO topic
  → SNS filter policy routes by event_type attribute
  → SQS FIFO queue (per domain, e.g. user-events.fifo)
  → SqsEventConsumer → EventRouter
  → EventHandlerRegistration<T> (deserializes to concrete type)
  → IEventHandler<T> (typed handler)
```

Key design decisions:

- **FIFO ordering**: `MessageGroupId` is the aggregate ID, ensuring ordered processing per entity
- **Type-safe handlers**: `EventRouter` maps event type strings to `IEventHandler<TPayload>` via `EventHandlerRegistration<T>`, which owns deserialization
- **Fire-and-forget publishing**: Events are published after the transaction commits. For guaranteed delivery, replace with a transactional outbox pattern
- **SNS filter policies**: Each SQS queue subscribes to multiple event types via message attribute filters, so one queue per domain rather than one per event type

### Dependency Injection

All wiring happens in `Program.cs`. Services, repositories, and persistence types are registered as **scoped** (one instance per HTTP request):

```
NpgsqlDataSource → DbContext → IDbContext
                 → TransactionManager → ITransactionManager
IDbContext → UserRepository → IUserRepository
IUserRepository + ITransactionManager → UserService
```

## Data Flow

### Read Request

```
HTTP GET /api/v1/users/{id}
  → ValidationFilter (no body to validate)
  → UserEndpoints handler
  → UserService.GetUserByIdAsync
  → UserRepository.GetByIdAsync (lazy connection, no transaction)
  → Dapper query → Postgres
  → User record returned
  → UserResponse DTO
  → HTTP 200 JSON
```

### Write Request

```
HTTP POST /api/v1/users
  → ValidationFilter (validates CreateUserRequest via FluentValidation)
  → UserEndpoints handler
  → UserService.CreateUserAsync
  → TransactionManager.TransactionAsync (opens connection + transaction)
    → UserValidators.ValidateCreateUserAsync (duplicate check inside transaction)
    → UserRepository.CreateAsync (INSERT inside transaction)
  → Commit
  → IEventPublisher.PublishAsync (fire-and-forget to SNS)
  → UserResponse DTO
  → HTTP 201 JSON
```

## Conventions

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Files | PascalCase | `UserEndpoints.cs`, `UserService.cs` |
| Classes/Records | PascalCase | `UserService`, `CreateUserCommand` |
| Interfaces | `I` prefix | `IUserRepository`, `IDbContext` |
| Async methods | `Async` suffix | `CreateUserAsync`, `GetByIdAsync` |
| API routes | kebab-case | `/api/v1/users` |
| DB columns | snake_case | `created_at`, `updated_at` |

### Code Organization

Each domain is organized by feature:

```
src/Domain/Users/
├── User.cs              # Domain model (record)
├── Commands.cs          # CreateUserCommand, PatchUserCommand (records)
├── UserService.cs       # Business logic orchestration
├── UserValidators.cs    # Static validation methods
└── UserRepository.cs    # IUserRepository interface

src/Infrastructure/Users/
└── UserRepository.cs    # Dapper implementation

src/Api/Users/
├── UserEndpoints.cs     # Minimal API route definitions
└── UserContracts.cs     # Request/response DTOs + FluentValidation

src/Worker/Users/
├── UserCreatedHandler.cs  # IEventHandler<UserCreatedEvent>
├── UserUpdatedHandler.cs  # IEventHandler<UserUpdatedEvent>
└── UserDeletedHandler.cs  # IEventHandler<UserDeletedEvent>
```

### Adding a New Domain

1. Create domain models and commands in `src/Domain/{Entity}/`
2. Define the repository interface in the same folder
3. Add validators and service
4. Implement the repository in `src/Infrastructure/{Entity}/`
5. Create endpoints and contracts in `src/Api/{Entity}/`
6. Register services in `Program.cs`
7. Create a migration in `resources/db/migrations/`
8. Add tests in `tests/Domain.Tests/{Entity}/`
