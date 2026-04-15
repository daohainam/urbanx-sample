# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UrbanX is a sample e-commerce **microservices** application built with .NET 10 and React 19, intended as an educational reference for distributed systems patterns. It demonstrates: Transactional Outbox, Choreography-based Saga, CQRS with dual storage (PostgreSQL + Elasticsearch), and event-driven communication via Kafka.

## Architecture

### Services

| Service | Port | Database | Kafka | Role |
|---------|------|----------|-------|------|
| Identity | 5005 | identitydb | No | OAuth 2.0 / OIDC (Duende IdentityServer) |
| Catalog | 5001 | catalogdb | Yes | Product catalog — CQRS (PostgreSQL writes, Elasticsearch reads) |
| Order | 5002 | orderdb | Yes | Shopping carts, orders — Saga coordinator |
| Merchant | 5003 | merchantdb | No | Merchant registration and profiles |
| Payment | 5004 | paymentdb | Yes | Stripe integration with transactional outbox |
| Inventory | dynamic | inventorydb | Yes | Stock tracking and reservations |
| API Gateway | 5000 | — | No | YARP reverse proxy, rate limiting (100 req/min), CORS |
| Frontend | 5173 (dev) | — | No | React 19 SPA — communicates only through the Gateway |

### Service Communication
- **Synchronous**: Frontend → Gateway → Services (HTTP/REST with JWT auth)
- **Asynchronous**: Services → Kafka → Services (event-driven, via Outbox relay)
- **Startup order**: PostgreSQL → Kafka → Identity → Catalog/Order/Payment/Inventory/Merchant → Gateway → Frontend

### Key Patterns
- **Transactional Outbox**: Catalog, Order, Payment, Inventory write events to an outbox table in the same DB transaction; background `OutboxRelayService` workers publish to Kafka. Guarantees at-least-once delivery.
- **Choreography Saga**: Order service emits `OrderCreated` → Inventory replies `InventoryReserved` → Payment emits `PaymentCompleted` → Order fulfills. No central orchestrator.
- **CQRS (Catalog)**: Writes go to PostgreSQL; Kafka events sync to Elasticsearch for fast full-text search queries.
- **Authorization policies**: `CustomerOnly`, `MerchantOnly`, `CustomerOrMerchant` — enforced at gateway and service level.

### Shared Libraries
- `src/Shared/UrbanX.Shared` — Common utilities, outbox infrastructure
- `src/Shared/UrbanX.Shared.Security` — JWT helpers, `GlobalExceptionHandler`, `RequestValidation`
- `src/ServiceDefaults/UrbanX.ServiceDefaults` — Aspire service defaults (OTEL, health checks, service discovery)
- `Directory.Packages.props` — Centralized NuGet version management for the entire solution

## Development Setup

### Prerequisites
- .NET 10 SDK with Aspire workload: `dotnet workload install aspire`
- Node.js (for frontend)
- Docker (for infrastructure)

### Recommended: .NET Aspire (starts everything)
```bash
# Start all backend services + infrastructure
cd src/AppHost/UrbanX.AppHost
dotnet run
# Aspire Dashboard: http://localhost:15260

# In a second terminal, start the frontend
cd src/Frontend/urbanx-react
npm install
npm run dev
# Frontend: http://localhost:5173
```

### Alternative: Manual startup
```bash
# 1. Start infrastructure only
docker-compose up -d   # PostgreSQL on :5432, Kafka on :9092

# 2. Start each service (separate terminals or use scripts)
./start-services.sh         # Linux/macOS
.\start-services.ps1        # Windows PowerShell

# 3. Start frontend
cd src/Frontend/urbanx-react && npm run dev
```

## Common Commands

### Backend
```bash
# Build entire solution
dotnet build UrbanX.sln

# Run a specific service with hot reload
cd src/Services/Order/UrbanX.Services.Order
dotnet watch run

# Run all tests
dotnet test UrbanX.sln

# Run a specific test project
dotnet test tests/UrbanX.Services.Catalog.UnitTests

# Run with coverage
dotnet test UrbanX.sln --collect:"XPlat Code Coverage"

# Add a migration (run from the service's project directory)
dotnet ef migrations add <MigrationName> --context <Service>DbContext
```

### Frontend
```bash
cd src/Frontend/urbanx-react

npm run dev      # Start dev server (Vite)
npm run build    # Production build
npm run lint     # ESLint
```

## Testing

- Framework: **xUnit** + **Moq**
- Database: **Microsoft.EntityFrameworkCore.InMemory** for unit tests
- Coverage: **coverlet**
- Test projects live in `tests/`
- `.http` files are included in service directories for manual API testing

## Key Configuration

- Environment template: `.env.example` (DB connection strings, Kafka, Stripe, OTEL endpoint)
- Per-service: `appsettings.json` / `appsettings.Development.json` / `appsettings.Production.json`
- Aspire wiring (service dependencies, resource references): `src/AppHost/UrbanX.AppHost/AppHost.cs`
- Gateway routes: `src/Gateway/UrbanX.Gateway/` — YARP configuration

## Infrastructure

- `docker-compose.yml` — Development: PostgreSQL 16 + Kafka (Confluent)
- `docker-compose.production.yml` — Full stack including all 6 services with health checks, resource limits, and restart policies
- `kubernetes/` — K8s manifests for production deployment
- All service Dockerfiles use multi-stage builds (`sdk:10.0` → `aspnet:10.0`), non-root user (uid 1001), `/alive` health endpoints
