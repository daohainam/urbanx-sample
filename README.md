# UrbanX - Multi-Merchant Commerce Platform

A modern, on-demand multi-merchant commerce platform built with microservices architecture.

## Architecture

### Backend
- **.NET 10** with **ASP.NET Minimal APIs**
- **.NET Aspire** for orchestration, service discovery, and observability
- **Microservices:**
  - **Catalog Service** (Port 5001) - Product search, browse
  - **Order Service** (Port 5002) - Cart, checkout, order tracking
  - **Merchant Service** (Port 5003) - Merchant and product management
  - **Payment Service** (Port 5004) - Payment processing
  - **Identity Service** (Port 5005) - Authentication with OIDC
  - **API Gateway** (Port 5000) - BFF using YARP
- **EF Core** with **PostgreSQL** per service
- **Kafka** for event messaging

### Frontend
- **React** + **Vite** + **TypeScript**
- **Tailwind CSS** for styling
- **OIDC** authentication (Authorization Code + PKCE)
- **React Router** for navigation

## Features

### Customer Features
- Product search and browse
- Shopping cart management
- Checkout process
- Online payment
- Order tracking timeline

### Merchant Features
- Product CRUD operations
- Pricing management
- Inventory management
- Order acceptance and preparation

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 18+
- Docker & Docker Compose

### Setup

#### Option 1: Using .NET Aspire (Recommended)

.NET Aspire provides orchestration, service discovery, and observability for all services.

1. **Install .NET Aspire workload (if not already installed):**
```bash
dotnet workload install aspire
```

2. **Start all services using Aspire AppHost:**
```bash
cd src/AppHost/UrbanX.AppHost
dotnet run
```

This will:
- Start the Aspire dashboard at http://localhost:15260
- Automatically provision PostgreSQL and Kafka containers
- Launch all microservices with proper configuration
- Enable service discovery, health checks, and distributed tracing
- Provide real-time monitoring and logs

3. **Start frontend:**
```bash
cd src/Frontend/urbanx-frontend
npm install
npm run dev
```

The application will be available at:
- **Aspire Dashboard:** http://localhost:15260
- **Frontend:** http://localhost:5173
- **API Gateway:** Dynamically assigned (check Aspire Dashboard)

#### Option 2: Manual Setup (Traditional)

1. **Start infrastructure (PostgreSQL & Kafka):**
```bash
docker-compose up -d
```

2. **Start backend services manually:**

```bash
# Catalog Service
cd src/Services/Catalog/UrbanX.Services.Catalog
dotnet run

# Order Service
cd src/Services/Order/UrbanX.Services.Order
dotnet run

# Merchant Service
cd src/Services/Merchant/UrbanX.Services.Merchant
dotnet run

# Payment Service
cd src/Services/Payment/UrbanX.Services.Payment
dotnet run

# Identity Service
cd src/Services/Identity/UrbanX.Services.Identity
dotnet run

# API Gateway
cd src/Gateway/UrbanX.Gateway
dotnet run
```

Or run all services:
```bash
dotnet build UrbanX.sln
```

3. **Start frontend:**
```bash
cd src/Frontend/urbanx-frontend
npm install
npm run dev
```

The application will be available at:
- **Frontend:** http://localhost:5173
- **API Gateway:** http://localhost:5000
- **Identity Service:** http://localhost:5005

## API Endpoints

### Catalog Service (via Gateway /api/products)
- `GET /api/products` - List products with search/filter
- `GET /api/products/{id}` - Get product details
- `GET /api/products/merchant/{merchantId}` - Get merchant products

### Order Service (via Gateway /api/cart, /api/orders)
- `GET /api/cart/{customerId}` - Get customer cart
- `POST /api/cart/{customerId}/items` - Add item to cart
- `DELETE /api/cart/{customerId}/items/{itemId}` - Remove from cart
- `POST /api/orders` - Create order
- `GET /api/orders/{orderId}` - Get order details
- `GET /api/orders/customer/{customerId}` - Get customer orders
- `PUT /api/orders/{orderId}/status` - Update order status

### Merchant Service (via Gateway /api/merchants)
- `GET /api/merchants/{id}` - Get merchant details
- `POST /api/merchants` - Create merchant
- `GET /api/merchants/{merchantId}/products` - Get merchant products
- `POST /api/merchants/{merchantId}/products` - Add product
- `PUT /api/merchants/{merchantId}/products/{productId}` - Update product
- `DELETE /api/merchants/{merchantId}/products/{productId}` - Delete product

### Payment Service (via Gateway /api/payments)
- `POST /api/payments` - Process payment
- `GET /api/payments/{id}` - Get payment details
- `GET /api/payments/order/{orderId}` - Get payment by order

## Technology Stack

### Backend
- .NET 10
- .NET Aspire (Orchestration, Service Discovery, Observability)
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL
- Confluent Kafka
- Duende IdentityServer
- YARP (Yet Another Reverse Proxy)
- OpenTelemetry (Distributed Tracing & Metrics)

### Frontend
- React 18
- TypeScript
- Vite
- Tailwind CSS
- React Router
- OIDC Client
- Lucide React (Icons)

## Project Structure

```
urbanx/
├── src/
│   ├── AppHost/              # .NET Aspire orchestrator
│   │   └── UrbanX.AppHost/   # Aspire AppHost project
│   ├── ServiceDefaults/      # Shared Aspire configuration
│   │   └── UrbanX.ServiceDefaults/
│   ├── Services/
│   │   ├── Catalog/          # Product catalog service
│   │   ├── Order/            # Order management service
│   │   ├── Merchant/         # Merchant management service
│   │   ├── Payment/          # Payment processing service
│   │   └── Identity/         # Identity & authentication service
│   ├── Gateway/              # API Gateway with YARP
│   ├── Frontend/
│   │   ├── urbanx-frontend/  # Customer-facing React SPA
│   │   ├── merchant-app/     # Merchant portal React SPA
│   │   └── UrbanX.MerchantAdmin/ # Merchant admin Blazor WASM
│   └── Shared/               # Shared libraries
├── docker-compose.yml        # Infrastructure setup (for manual setup)
└── UrbanX.sln               # Solution file
```

## Merchant Portal

The merchant portal is a separate React application for managing products, categories, and orders.

### Starting the Merchant Portal

```bash
cd src/Frontend/merchant-app
npm install
npm run dev
```

The merchant portal will be available at http://localhost:5174

**Test Credentials:**
- Username: `merchant@test.com`
- Password: `Password123!`

### Merchant Portal Features
- Dashboard with statistics overview
- Category management (create, edit, delete)
- Product management (create, delete, inventory tracking)
- Order management (view orders, update status)

## Merchant Admin (Blazor WebAssembly)

The Merchant Admin is a Blazor WebAssembly application providing an alternative admin interface for merchants.

### Starting the Merchant Admin

```bash
cd src/Frontend/UrbanX.MerchantAdmin
dotnet run
```

The merchant admin will be available at http://localhost:5175

**Test Credentials:**
- Username: `merchant@test.com`
- Password: `Password123!`

### Merchant Admin Features
- Dashboard with statistics cards
- Product management (list, create, edit, delete)
- Category management (CRUD operations)
- Order management with status filtering and updates
- OIDC authentication integration
- Responsive Bootstrap UI

## .NET Aspire Features

The application uses .NET Aspire to provide:

### Orchestration
- **Unified Development Experience**: Single command to start all services
- **Automatic Resource Provisioning**: PostgreSQL and Kafka containers are automatically created and configured
- **Service Discovery**: Services can discover each other automatically without hardcoded URLs

### Observability
- **Distributed Tracing**: Track requests across all microservices using OpenTelemetry
- **Structured Logging**: Centralized logs from all services in the Aspire Dashboard
- **Metrics & Health Checks**: Real-time monitoring of service health and performance
- **Dashboard**: Visual interface to monitor all services, resources, and logs

### Developer Productivity
- **ServiceDefaults**: Shared configuration for health checks, telemetry, and resilience
- **Component Integration**: Seamless integration with PostgreSQL, Kafka, and other services
- **Environment Configuration**: Automatic connection string and configuration management

## Development

### Using Aspire for Development (Recommended)
When developing with .NET Aspire:
- Run `dotnet run` from the AppHost project to start all services
- Access the Aspire Dashboard to monitor services, view logs, and traces
- Services automatically discover each other via Aspire's service discovery
- Hot reload works for individual services - just save your code changes

### Backend Development (Manual)
Each service can be run independently. They use in-memory or local PostgreSQL databases.

### Frontend Development
```bash
cd src/Frontend/urbanx-frontend
npm run dev
```

Hot reload is enabled for development.

## License

MIT