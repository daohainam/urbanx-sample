# UrbanX - Multi-Merchant Commerce Platform

A modern, event-driven multi-merchant commerce platform built with .NET 10 microservices.

## Architecture

### Microservices

| Service | Description |
|---------|-------------|
| **Catalog Service** | Product catalog with CQRS: PostgreSQL write side, Elasticsearch read side |
| **Order Service** | Cart management, checkout, order tracking |
| **Merchant Service** | Merchant registration and product management |
| **Payment Service** | Payment processing via Stripe |
| **Inventory Service** | Stock reservation and management |
| **Identity Service** | Authentication and authorization (Duende IdentityServer) |
| **API Gateway** | Single entry point with YARP reverse proxy and rate limiting |

### Frontend

- **React 19** + TypeScript (Vite, Tailwind CSS 4) — customer-facing SPA located in `src/Frontend/urbanx-react`

### Infrastructure

- **PostgreSQL** — dedicated database per service
- **Apache Kafka** — asynchronous event messaging between services
- **Elasticsearch** — product search index (Catalog read side)
- **.NET Aspire** — orchestration, service discovery, health checks, and distributed tracing

## Key Features

### Customer Features
- Product search and browse (Elasticsearch-powered)
- Shopping cart management
- Checkout and online payment (Stripe)
- Order tracking with status timeline

### Merchant Features
- Product CRUD with inventory management
- Order acceptance and fulfillment workflow

## Architecture Patterns

- **Transactional Outbox** — Catalog, Order, Payment, and Inventory services atomically persist domain events to an outbox table before publishing to Kafka, ensuring no message loss.
- **Saga (Choreography)** — Order fulfillment is coordinated via events:  
  `OrderCreated` → Inventory reserved/failed → Payment processed/failed → Merchant notified
- **CQRS** — Catalog service writes to PostgreSQL and reads from Elasticsearch.
- **Rate Limiting** — API Gateway enforces 100 requests per minute per IP with HTTP 429 + `Retry-After` header.
- **Policy-Based Authorization** — All sensitive endpoints require JWT bearer tokens; policies: `CustomerOnly`, `MerchantOnly`, `CustomerOrMerchant`.

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- Docker & Docker Compose

### Option 1: .NET Aspire (Recommended)

Aspire automatically provisions PostgreSQL and Kafka, wires up service discovery, and streams all logs and traces to a dashboard.

1. **Install the Aspire workload** (if not already installed):
```bash
dotnet workload install aspire
```

2. **Start all backend services:**
```bash
cd src/AppHost/UrbanX.AppHost
dotnet run
```

This starts the Aspire dashboard at **http://localhost:15260** and launches all microservices.

3. **Start the frontend** (in a separate terminal):
```bash
cd src/Frontend/urbanx-react
npm install
npm run dev
```

The application is available at:
- **Frontend:** http://localhost:5173
- **API Gateway:** Dynamically assigned (see Aspire Dashboard)
- **Aspire Dashboard:** http://localhost:15260

### Option 2: Manual Setup

1. **Start infrastructure** (PostgreSQL & Kafka):
```bash
docker-compose up -d
```

2. **Start each backend service** in separate terminals:
```bash
# Identity Service
cd src/Services/Identity/UrbanX.Services.Identity && dotnet run

# Catalog Service
cd src/Services/Catalog/UrbanX.Services.Catalog && dotnet run

# Order Service
cd src/Services/Order/UrbanX.Services.Order && dotnet run

# Merchant Service
cd src/Services/Merchant/UrbanX.Services.Merchant && dotnet run

# Payment Service
cd src/Services/Payment/UrbanX.Services.Payment && dotnet run

# Inventory Service
cd src/Services/Inventory/UrbanX.Services.Inventory && dotnet run

# API Gateway
cd src/Gateway/UrbanX.Gateway && dotnet run
```

3. **Start the frontend:**
```bash
cd src/Frontend/urbanx-react
npm install
npm run dev
```

The application is available at:
- **Frontend:** http://localhost:5173
- **API Gateway:** http://localhost:5000
- **Identity Service:** http://localhost:5005

## API Endpoints

All routes below are accessed via the API Gateway. Endpoints marked 🔒 require a JWT bearer token.

### Catalog Service (`/api/products`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/products` | Public | List/search products |
| `GET` | `/api/products/{id}` | Public | Get product by ID |
| `GET` | `/api/products/merchant/{merchantId}` | Public | List merchant products |
| `POST` | `/api/products` | 🔒 Merchant | Create product |
| `PUT` | `/api/products/{id}` | 🔒 Merchant | Update product |
| `DELETE` | `/api/products/{id}` | 🔒 Merchant | Delete product |

### Order Service (`/api/cart`, `/api/orders`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/cart/{customerId}` | 🔒 Customer | Get customer cart |
| `POST` | `/api/cart/{customerId}/items` | 🔒 Customer | Add item to cart |
| `DELETE` | `/api/cart/{customerId}/items/{itemId}` | 🔒 Customer | Remove item from cart |
| `POST` | `/api/orders` | 🔒 Customer | Create order |
| `GET` | `/api/orders/{orderId}` | 🔒 Customer/Merchant | Get order details |
| `GET` | `/api/orders/customer/{customerId}` | 🔒 Customer | List customer orders |
| `PUT` | `/api/orders/{orderId}/status` | 🔒 Merchant | Update order status |

### Merchant Service (`/api/merchants`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/merchants/{id}` | Public | Get merchant details |
| `POST` | `/api/merchants` | 🔒 Merchant | Register merchant |
| `GET` | `/api/merchants/{merchantId}/products` | Public | List merchant products |
| `POST` | `/api/merchants/{merchantId}/products` | 🔒 Merchant | Add product |
| `PUT` | `/api/merchants/{merchantId}/products/{productId}` | 🔒 Merchant | Update product |
| `DELETE` | `/api/merchants/{merchantId}/products/{productId}` | 🔒 Merchant | Delete product |

### Payment Service (`/api/payments`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/payments` | 🔒 Customer | Process payment (Stripe) |
| `GET` | `/api/payments/{id}` | 🔒 Customer/Merchant | Get payment by ID |
| `GET` | `/api/payments/order/{orderId}` | 🔒 Customer/Merchant | Get payment by order |

### Inventory Service (`/api/inventory`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/inventory/{productId}` | 🔒 Merchant | Get inventory item |
| `POST` | `/api/inventory` | 🔒 Merchant | Create inventory item |
| `PUT` | `/api/inventory/{productId}` | 🔒 Merchant | Update stock quantity |
| `GET` | `/api/inventory/reservations/{orderId}` | 🔒 Customer/Merchant | Get reservations for order |

## Technology Stack

### Backend
- .NET 10 / ASP.NET Core Minimal APIs
- .NET Aspire (orchestration, service discovery, observability)
- Entity Framework Core + PostgreSQL (per-service database)
- Apache Kafka (event streaming)
- Elasticsearch (product search)
- Duende IdentityServer (JWT authentication)
- Stripe SDK (payment processing)
- YARP (API Gateway reverse proxy)
- OpenTelemetry (distributed tracing & metrics)

### Frontend
- React 19 + TypeScript
- Vite
- Tailwind CSS 4
- React Router
- oidc-client-ts (OIDC authentication)
- Lucide React (icons)

### Shared Libraries
- `UrbanX.Shared.Security` — JWT authorization policies, input validation, global exception handler
- `UrbanX.ServiceDefaults` — Aspire service defaults (health checks, telemetry, resilience)

## Project Structure

```
urbanx-sample/
├── src/
│   ├── AppHost/
│   │   └── UrbanX.AppHost/          # .NET Aspire orchestrator
│   ├── ServiceDefaults/
│   │   └── UrbanX.ServiceDefaults/  # Shared Aspire configuration
│   ├── Services/
│   │   ├── Catalog/                 # Product catalog (CQRS + Elasticsearch)
│   │   ├── Order/                   # Cart, orders, saga coordinator
│   │   ├── Merchant/                # Merchant & product management
│   │   ├── Payment/                 # Stripe payment processing
│   │   ├── Inventory/               # Stock reservation & management
│   │   └── Identity/                # Duende IdentityServer
│   ├── Gateway/
│   │   └── UrbanX.Gateway/          # YARP reverse proxy + rate limiting
│   ├── Frontend/
│   │   └── urbanx-react/            # Customer-facing React SPA
│   └── Shared/
│       ├── UrbanX.Shared/           # Shared domain models
│       └── UrbanX.Shared.Security/  # Security utilities
├── tests/                           # Unit and integration tests
├── kubernetes/                      # Kubernetes manifests
├── docker-compose.yml               # Infrastructure (PostgreSQL, Kafka)
├── docker-compose.production.yml    # Production Docker Compose
├── generate-migrations.sh           # EF Core migration helper script
└── UrbanX.sln                       # Solution file
```

## Development

### Using Aspire (Recommended)
Run `dotnet run` from `src/AppHost/UrbanX.AppHost` to start everything. The Aspire Dashboard provides real-time logs, traces, and health status for all services.

### Running Tests
```bash
dotnet test UrbanX.sln
```

Both unit and integration tests are provided for each service under the `tests/` directory.

### Database Migrations
Migrations are applied automatically on service startup. To generate a new migration:
```bash
cd src/Services/<Service>/UrbanX.Services.<Service>
dotnet ef migrations add <MigrationName> --context <Service>DbContext
```

See [DATABASE_MIGRATIONS.md](DATABASE_MIGRATIONS.md) for detailed guidance.

### Environment Configuration
Copy `.env.example` to `.env` and fill in the required values (database passwords, Stripe keys, etc.) before running manually.

## Security

- JWT bearer authentication on all sensitive endpoints
- Policy-based authorization (`CustomerOnly`, `MerchantOnly`, `CustomerOrMerchant`)
- Per-IP rate limiting in the API Gateway (100 req/min)
- Input validation on all write endpoints (`UrbanX.Shared.Security.RequestValidation`)
- Global exception handler with environment-aware error details
- Security headers (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy)

See [SECURITY.md](SECURITY.md) for production security guidance.

## Deployment

Docker and Kubernetes manifests are included:

```bash
# Production Docker Compose
docker-compose -f docker-compose.production.yml up -d

# Kubernetes
kubectl apply -f kubernetes/
```

See [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md) for a full deployment guide.

## License

MIT