# UrbanX - Multi-Merchant Commerce Platform

UrbanX is a sample e-commerce platform built to demonstrate how a real-world application is structured using modern software engineering techniques. It is designed for educational purposes, so the code is organized to make each concept as clear as possible.

The system is built as a set of **microservices** — small, independent backend programs that each handle one area of the business. A React frontend provides the customer-facing interface. Everything runs behind an API Gateway that acts as the single entry point for all requests.

---

## What This Project Teaches

This project is a hands-on example of several industry-standard patterns and technologies:

- **Microservices architecture** — breaking a large application into small, independently deployable services
- **Event-driven communication** — services talk to each other by publishing and subscribing to events via Apache Kafka
- **CQRS (Command Query Responsibility Segregation)** — separating write operations (PostgreSQL) from read operations (Elasticsearch) for better performance
- **Saga pattern (choreography-based)** — coordinating a multi-step business process (place order → reserve stock → process payment → notify merchant) without a central controller
- **Transactional outbox** — a technique that guarantees messages are never lost even if a service crashes after saving data but before sending the message
- **JWT authentication and policy-based authorization** — securing APIs so only the right users can access the right endpoints
- **API Gateway with rate limiting** — a single front door that protects backend services from being overwhelmed
- **OpenTelemetry and distributed tracing** — observing what happens across many services when a single request is processed

---

## System Overview

When a customer places an order, the following sequence of events happens automatically across multiple services:

1. The customer adds products to a cart (Order Service) and checks out.
2. The Order Service creates an order and publishes an `OrderCreated` event to Kafka.
3. The Inventory Service receives the event, checks available stock, reserves the items, and publishes an `InventoryReserved` or `InventoryFailed` event.
4. If inventory is reserved, the Payment Service processes the charge via Stripe and publishes a `PaymentCompleted` or `PaymentFailed` event.
5. If payment succeeds, the Merchant Service is notified so the merchant can accept and fulfill the order.
6. If any step fails, the order is cancelled and the customer is informed.

This flow is an example of the **Saga (choreography)** pattern — no single service controls the whole process; each service reacts to events from the previous step.

---

## Architecture

### Microservices

| Service | Port | Description |
|---------|------|-------------|
| Catalog Service | 5001 | Manages the product catalog. Uses CQRS: writes go to PostgreSQL, reads come from Elasticsearch for fast search. |
| Order Service | 5002 | Handles shopping carts, order creation, and order status tracking. |
| Merchant Service | 5003 | Manages merchant accounts and their product listings. |
| Payment Service | 5004 | Processes payments using Stripe. Uses the transactional outbox to reliably publish payment events. |
| Inventory Service | (dynamic) | Tracks stock levels. Reserves stock when an order is placed and releases it if the order is cancelled. |
| Identity Service | 5005 | Handles user registration, login, and issues JWT tokens using Duende IdentityServer. |
| API Gateway | 5000 | The single entry point for all client requests. Routes traffic to the correct service using YARP. Enforces rate limiting. |

### Frontend

The frontend is a React 19 single-page application (SPA) located in `src/Frontend/urbanx-react`. It connects to the backend exclusively through the API Gateway and authenticates users via OpenID Connect (OIDC).

### Infrastructure

| Component | Purpose |
|-----------|---------|
| PostgreSQL | Each service has its own dedicated database (database-per-service pattern). |
| Apache Kafka | The message broker used for asynchronous event-driven communication between services. |
| Elasticsearch | Stores a searchable copy of the product catalog for fast full-text search. |
| .NET Aspire | A development-time tool that starts all services together, provides service discovery, health checks, and a live observability dashboard. |

---

## Architecture Patterns Explained

### Transactional Outbox

**The problem it solves:** A service saves data to its database and then needs to send a message (event) to Kafka. If the service crashes between these two steps, the data is saved but the message is never sent. Other services never learn what happened.

**How it works:** Instead of publishing to Kafka directly, the service saves the event as a row in an "outbox" table in the *same database transaction* as the business data. A background worker (`OutboxRelayService`) then reads from this outbox table and publishes the messages to Kafka. This guarantees that if the data is saved, the message will eventually be sent — even after a crash.

Services that use this pattern: Catalog, Order, Payment, Inventory.

### Saga (Choreography)

**The problem it solves:** Completing an order requires several steps across multiple services. If one step fails, all previous steps must be rolled back. How do you coordinate this without tightly coupling the services together?

**How it works (choreography style):** There is no central coordinator. Each service listens for events and reacts to them. For example:
- The Order Service publishes `OrderCreated`.
- The Inventory Service hears this and reserves stock, then publishes `InventoryReserved`.
- The Payment Service hears this and charges the customer, then publishes `PaymentCompleted`.
- The Merchant Service hears this and marks the order as ready for fulfillment.

If anything goes wrong, a failure event triggers the appropriate compensation steps (cancelling the order, releasing reserved stock, etc.).

### CQRS (Command Query Responsibility Segregation)

**The problem it solves:** A product database optimized for safe, transactional writes (PostgreSQL) is not always the best tool for fast, flexible search queries.

**How it works:** The Catalog Service writes all product changes to PostgreSQL (the "command" side). At the same time, it publishes events that update an Elasticsearch index (the "query" side). Read requests — like searching for products — go directly to Elasticsearch for speed, while writes always go through PostgreSQL for consistency.

---

## Getting Started

### Prerequisites

Before running the project, make sure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker and Docker Compose](https://www.docker.com/)

### Option 1: .NET Aspire (Recommended for Development)

.NET Aspire is the easiest way to run the whole project locally. It automatically starts PostgreSQL and Kafka in Docker, connects all the services together, and opens a dashboard where you can see logs and traces from every service in one place.

**Step 1:** Install the Aspire workload (only needed once):

```bash
dotnet workload install aspire
```

**Step 2:** Start all backend services:

```bash
cd src/AppHost/UrbanX.AppHost
dotnet run
```

The Aspire Dashboard will open at `http://localhost:15260`. Wait until all services show as healthy before proceeding.

**Step 3:** Start the frontend in a new terminal:

```bash
cd src/Frontend/urbanx-react
npm install
npm run dev
```

The application is now available at:

- Frontend: `http://localhost:5173`
- API Gateway: Shown in the Aspire Dashboard (dynamically assigned)
- Aspire Dashboard: `http://localhost:15260`

### Option 2: Manual Setup

Use this approach if you prefer to start each component individually or if you are not using Aspire.

**Step 1:** Start the infrastructure (PostgreSQL and Kafka) using Docker:

```bash
docker-compose up -d
```

This starts:
- PostgreSQL on port 5432
- Kafka on port 9092
- Zookeeper on port 2181

**Step 2:** Copy the environment file and fill in any required values:

```bash
cp .env.example .env
```

**Step 3:** Start each backend service in its own terminal. The Identity Service must start first because other services depend on it for authentication:

```bash
# Terminal 1 - Identity Service (start this first)
cd src/Services/Identity/UrbanX.Services.Identity && dotnet run

# Terminal 2 - Catalog Service
cd src/Services/Catalog/UrbanX.Services.Catalog && dotnet run

# Terminal 3 - Order Service
cd src/Services/Order/UrbanX.Services.Order && dotnet run

# Terminal 4 - Merchant Service
cd src/Services/Merchant/UrbanX.Services.Merchant && dotnet run

# Terminal 5 - Payment Service
cd src/Services/Payment/UrbanX.Services.Payment && dotnet run

# Terminal 6 - Inventory Service
cd src/Services/Inventory/UrbanX.Services.Inventory && dotnet run

# Terminal 7 - API Gateway
cd src/Gateway/UrbanX.Gateway && dotnet run
```

Alternatively, use the provided scripts:

```bash
# Linux / macOS
./start-services.sh

# Windows PowerShell
.\start-services.ps1
```

**Step 4:** Start the frontend:

```bash
cd src/Frontend/urbanx-react
npm install
npm run dev
```

The application is now available at:

- Frontend: `http://localhost:5173`
- API Gateway: `http://localhost:5000`
- Identity Service: `http://localhost:5005`

---

## API Endpoints

All API requests from the frontend go through the API Gateway at `http://localhost:5000`. The gateway forwards them to the appropriate service.

Endpoints marked **[Auth required]** require a valid JWT bearer token in the `Authorization: Bearer <token>` request header. The token is obtained by logging in through the Identity Service.

### Catalog Service — `/api/products`

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| GET | `/api/products` | Public | Search and list all products |
| GET | `/api/products/{id}` | Public | Get details of a specific product |
| GET | `/api/products/merchant/{merchantId}` | Public | List products for a specific merchant |
| POST | `/api/products` | [Auth required] Merchant | Create a new product |
| PUT | `/api/products/{id}` | [Auth required] Merchant | Update an existing product |
| DELETE | `/api/products/{id}` | [Auth required] Merchant | Delete a product |

### Order Service — `/api/cart` and `/api/orders`

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| GET | `/api/cart/{customerId}` | [Auth required] Customer | Get the customer's current cart |
| POST | `/api/cart/{customerId}/items` | [Auth required] Customer | Add an item to the cart |
| DELETE | `/api/cart/{customerId}/items/{itemId}` | [Auth required] Customer | Remove an item from the cart |
| POST | `/api/orders` | [Auth required] Customer | Place an order (checkout) |
| GET | `/api/orders/{orderId}` | [Auth required] Customer or Merchant | Get details of a specific order |
| GET | `/api/orders/customer/{customerId}` | [Auth required] Customer | List all orders for a customer |
| PUT | `/api/orders/{orderId}/status` | [Auth required] Merchant | Update the status of an order |

### Merchant Service — `/api/merchants`

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| GET | `/api/merchants/{id}` | Public | Get merchant profile |
| POST | `/api/merchants` | [Auth required] Merchant | Register as a new merchant |
| GET | `/api/merchants/{merchantId}/products` | Public | List a merchant's products |
| POST | `/api/merchants/{merchantId}/products` | [Auth required] Merchant | Add a product |
| PUT | `/api/merchants/{merchantId}/products/{productId}` | [Auth required] Merchant | Update a product |
| DELETE | `/api/merchants/{merchantId}/products/{productId}` | [Auth required] Merchant | Delete a product |

### Payment Service — `/api/payments`

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| POST | `/api/payments` | [Auth required] Customer | Submit a payment for an order |
| GET | `/api/payments/{id}` | [Auth required] Customer or Merchant | Get a payment record by ID |
| GET | `/api/payments/order/{orderId}` | [Auth required] Customer or Merchant | Get the payment for a specific order |

### Inventory Service — `/api/inventory`

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| GET | `/api/inventory/{productId}` | [Auth required] Merchant | Get the inventory record for a product |
| POST | `/api/inventory` | [Auth required] Merchant | Create an inventory record for a new product |
| PUT | `/api/inventory/{productId}` | [Auth required] Merchant | Update the stock quantity for a product |
| GET | `/api/inventory/reservations/{orderId}` | [Auth required] Customer or Merchant | Get the inventory reservations for an order |

---

## Technology Stack

### Backend

| Technology | Purpose |
|------------|---------|
| .NET 10 / ASP.NET Core | The main framework for all backend services, using Minimal API style |
| .NET Aspire | Development orchestration, service discovery, health checks, and distributed tracing dashboard |
| Entity Framework Core | Object-relational mapper (ORM) for database access |
| PostgreSQL | Relational database; each service has its own isolated database |
| Apache Kafka | Distributed message broker for event-driven communication |
| Elasticsearch | Full-text search engine used for the product catalog read model |
| Duende IdentityServer | OpenID Connect and OAuth 2.0 server for issuing JWT tokens |
| YARP | The "Yet Another Reverse Proxy" library used to build the API Gateway |
| Stripe SDK | Payment processing integration |
| OpenTelemetry | Distributed tracing and metrics collection |

### Frontend

| Technology | Purpose |
|------------|---------|
| React 19 + TypeScript | UI library and language for building the customer-facing application |
| Vite | Fast development server and build tool |
| Tailwind CSS 4 | Utility-first CSS framework for styling |
| React Router | Client-side routing |
| oidc-client-ts | Handles OpenID Connect login flow with the Identity Service |

### Shared Libraries

| Library | Purpose |
|---------|---------|
| `UrbanX.Shared.Security` | Reusable security utilities: JWT authorization policies, input validation helpers, and the global exception handler |
| `UrbanX.ServiceDefaults` | Common Aspire configuration applied to all services: health checks, telemetry, and resilience policies |

---

## Project Structure

```
urbanx-sample/
├── src/
│   ├── AppHost/
│   │   └── UrbanX.AppHost/           # .NET Aspire host — starts and wires up all services
│   ├── ServiceDefaults/
│   │   └── UrbanX.ServiceDefaults/   # Shared Aspire defaults (health, telemetry, resilience)
│   ├── Services/
│   │   ├── Catalog/                  # Product catalog service (CQRS, Elasticsearch)
│   │   ├── Order/                    # Cart and order service (Saga coordinator via events)
│   │   ├── Merchant/                 # Merchant registration and management
│   │   ├── Payment/                  # Payment processing via Stripe
│   │   ├── Inventory/                # Stock tracking and reservation
│   │   └── Identity/                 # Authentication server (Duende IdentityServer)
│   ├── Gateway/
│   │   └── UrbanX.Gateway/           # API Gateway (YARP reverse proxy + rate limiting)
│   ├── Frontend/
│   │   └── urbanx-react/             # Customer-facing React SPA
│   └── Shared/
│       ├── UrbanX.Shared/            # Shared domain models and contracts
│       └── UrbanX.Shared.Security/   # Security helpers used across all services
├── tests/                            # Unit and integration tests for all services
├── kubernetes/                       # Kubernetes deployment manifests
├── docker-compose.yml                # Local infrastructure (PostgreSQL, Kafka, Elasticsearch)
├── docker-compose.production.yml     # Production-ready Docker Compose configuration
├── generate-migrations.sh            # Helper script to generate EF Core migrations
├── start-services.sh                 # Linux/macOS script to start all services
├── start-services.ps1                # Windows PowerShell script to start all services
└── UrbanX.sln                        # .NET solution file
```

---

## Development Guide

### Running Tests

The `tests/` directory contains both unit tests and integration tests for each service. To run all tests:

```bash
dotnet test UrbanX.sln
```

### Hot Reload

- **Frontend:** Hot reload is enabled automatically when you run `npm run dev`. Changes to React components appear in the browser instantly.
- **Backend:** Use `dotnet watch run` instead of `dotnet run` to enable hot reload for .NET services.

### Database Migrations

Database schema migrations are applied automatically when each service starts up. To create a new migration after changing a data model:

```bash
cd src/Services/<ServiceName>/UrbanX.Services.<ServiceName>
dotnet ef migrations add <MigrationName> --context <ServiceName>DbContext
```

Replace `<ServiceName>` with the name of the service you changed (for example, `Catalog`, `Order`, `Merchant`).

See [DATABASE_MIGRATIONS.md](DATABASE_MIGRATIONS.md) for full guidance.

### Environment Configuration

Sensitive values such as database passwords and Stripe API keys are managed through environment variables. A template is provided:

```bash
cp .env.example .env
```

Open `.env` in a text editor and fill in the required values before starting the services manually.

### Testing API Endpoints Manually

Each service includes a `.http` file that you can use to test endpoints directly from Visual Studio Code (with the REST Client extension) or JetBrains Rider:

- `UrbanX.Services.Catalog.http`
- `UrbanX.Services.Order.http`
- `UrbanX.Services.Merchant.http`
- `UrbanX.Services.Payment.http`

You can also use `curl` to test the API through the gateway:

```bash
# List all products (no authentication required)
curl http://localhost:5000/api/products

# Get a specific product by ID
curl http://localhost:5000/api/products/<product-id>
```

### Troubleshooting

**Port already in use:** Change the port in the service's `Properties/launchSettings.json` file.

**Database connection problems:**
```bash
docker-compose ps               # Check which containers are running
docker-compose up -d postgres   # Restart the PostgreSQL container
```

**Frontend not loading:**
```bash
cd src/Frontend/urbanx-react
rm -rf node_modules package-lock.json
npm install
```

**Backend build errors:**
```bash
dotnet clean && dotnet build
```

---

## Security

- **JWT authentication** — All sensitive endpoints require a valid JWT bearer token issued by the Identity Service.
- **Policy-based authorization** — Three authorization policies are defined: `CustomerOnly`, `MerchantOnly`, and `CustomerOrMerchant`. Each endpoint declares which policy applies.
- **Rate limiting** — The API Gateway limits each IP address to 100 requests per minute. Requests that exceed this limit receive an HTTP 429 response with a `Retry-After` header.
- **Input validation** — All write endpoints validate request data using helpers in `UrbanX.Shared.Security.RequestValidation`.
- **Global exception handling** — Unhandled exceptions are caught and returned as standardized error responses. Stack traces are hidden in production environments.
- **Security headers** — All responses include `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, and `Referrer-Policy` headers.

See [SECURITY.md](SECURITY.md) for production security guidance.

---

## Deployment

### Docker Compose (Production)

A production-ready Docker Compose file is included:

```bash
docker-compose -f docker-compose.production.yml up -d
```

### Kubernetes

Kubernetes manifests for all services are located in the `kubernetes/` directory:

```bash
kubectl apply -f kubernetes/
```

See [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md) for a complete step-by-step deployment guide.

---

## License

MIT
