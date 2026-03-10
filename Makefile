.PHONY: help workspace-up workspace-down workspace-build workspace-shell restore build clean test lint fix run-api run-worker add remove build-image build-migrations app-api app-worker app-up app-down migrate migrate-up migrate-down migrate-version migrate-force migrate-create localstack-up localstack-down localstack-setup localstack-logs

SERVICE := workspace

help: ## Show this help message
	@echo 'Usage: make [target]'
	@echo ''
	@echo 'Available targets:'
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  %-20s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

# Workspace container management
workspace-up: ## Start the workspace container
	docker compose --profile workspace up -d $(SERVICE)

workspace-build: ## Build the workspace container
	docker compose build $(SERVICE)

workspace-down: ## Stop and remove the workspace container
	docker compose --profile workspace down

workspace-shell: ## Open a shell in the workspace container
	docker compose exec $(SERVICE) /bin/bash

# Development commands (run in workspace container)
restore: ## Restore NuGet packages
	docker compose exec $(SERVICE) dotnet restore

build: ## Build the solution
	docker compose exec $(SERVICE) dotnet build

clean: ## Clean build artifacts
	docker compose exec $(SERVICE) dotnet clean

test: ## Run tests
	docker compose exec $(SERVICE) dotnet test

lint: ## Check code formatting
	docker compose exec $(SERVICE) dotnet format --verify-no-changes

fix: ## Fix code formatting
	docker compose exec $(SERVICE) dotnet format

run-api: ## Run the API server (with hot reload)
	docker compose exec $(SERVICE) dotnet watch run --project src/Api

run-worker: ## Run the worker (with hot reload)
	docker compose exec $(SERVICE) dotnet watch run --project src/Worker

add: ## Add a NuGet package (usage: make add PROJECT=src/Api PKG=Serilog)
	@if [ -z "$(PROJECT)" ] || [ -z "$(PKG)" ]; then \
		echo "Error: PROJECT and PKG are required. Usage: make add PROJECT=src/Infrastructure PKG=Npgsql.EntityFrameworkCore.PostgreSQL"; \
		exit 1; \
	fi
	docker compose exec $(SERVICE) dotnet add $(PROJECT) package $(PKG)

remove: ## Remove a NuGet package (usage: make remove PROJECT=src/Api PKG=Serilog)
	@if [ -z "$(PROJECT)" ] || [ -z "$(PKG)" ]; then \
		echo "Error: PROJECT and PKG are required. Usage: make remove PROJECT=src/Api PKG=Serilog"; \
		exit 1; \
	fi
	docker compose exec $(SERVICE) dotnet remove $(PROJECT) package $(PKG)

# Production Docker image builds
build-image: ## Build the production Docker image
	docker build -f resources/docker/app.Dockerfile -t app:latest .

app-api: build-image ## Run the API from the production image
	docker compose --profile app up -d app-api

app-worker: build-image ## Run the worker from the production image
	docker compose --profile app up -d app-worker

app-up: build-image ## Run both API and worker from the production image
	docker compose --profile app up -d app-api app-worker

app-down: ## Stop API and worker containers
	docker compose --profile app down

build-migrations: ## Build migrations Docker image
	docker build -f resources/docker/migrate.Dockerfile -t app-migrations:latest .

# Database migration commands
migrate: build-migrations ## Run database migrations (up)
	docker compose run --rm migrate -path /migrations -database "postgres://postgres:postgres@postgres:5432/app?sslmode=disable" up

migrate-up: migrate ## Alias for migrate

migrate-down: build-migrations ## Rollback last migration
	docker compose run --rm migrate -path /migrations -database "postgres://postgres:postgres@postgres:5432/app?sslmode=disable" down

migrate-version: build-migrations ## Show current migration version
	docker compose run --rm migrate -path /migrations -database "postgres://postgres:postgres@postgres:5432/app?sslmode=disable" version

migrate-force: build-migrations ## Force set migration version (usage: make migrate-force VERSION=1)
	@if [ -z "$(VERSION)" ]; then \
		echo "Error: VERSION is required. Usage: make migrate-force VERSION=1"; \
		exit 1; \
	fi
	docker compose run --rm migrate -path /migrations -database "postgres://postgres:postgres@postgres:5432/app?sslmode=disable" force $(VERSION)

migrate-create: ## Create a new migration (usage: make migrate-create NAME=my_migration)
	@if [ -z "$(NAME)" ]; then \
		echo "Error: NAME is required. Usage: make migrate-create NAME=my_migration"; \
		exit 1; \
	fi
	bash resources/scripts/migrate.sh create $(NAME)

# LocalStack commands
localstack-up: ## Start LocalStack services
	@echo "Starting LocalStack..."
	docker compose up -d localstack
	@echo "Waiting for LocalStack to be ready..."
	@timeout=60; \
	while [ $$timeout -gt 0 ]; do \
		if docker compose exec -T localstack curl -f http://localhost:4566/_localstack/health >/dev/null 2>&1; then \
			echo "LocalStack is ready!"; \
			exit 0; \
		fi; \
		sleep 2; \
		timeout=$$((timeout - 2)); \
	done; \
	echo "Warning: LocalStack may not be fully ready yet"

localstack-setup: localstack-up ## Setup LocalStack resources (SNS topics and SQS queues)
	@echo "Setting up LocalStack resources (SNS topics and SQS queues)..."
	@docker compose exec $(SERVICE) bash resources/scripts/setup_localstack.sh

localstack-down: ## Stop LocalStack services
	@echo "Stopping LocalStack..."
	docker compose stop localstack

localstack-logs: ## View LocalStack logs
	docker compose logs -f localstack
