# UrbanX - Multi-Merchant Commerce Platform

A modern, on-demand multi-merchant commerce platform built with microservices architecture.

## Architecture

### Backend
- **.NET 10** with **ASP.NET Minimal APIs**
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

1. **Start infrastructure (PostgreSQL & Kafka):**
```bash
docker-compose up -d
```

2. **Start backend services:**

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
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL
- Confluent Kafka
- Duende IdentityServer
- YARP (Yet Another Reverse Proxy)

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
│   ├── Services/
│   │   ├── Catalog/          # Product catalog service
│   │   ├── Order/            # Order management service
│   │   ├── Merchant/         # Merchant management service
│   │   ├── Payment/          # Payment processing service
│   │   └── Identity/         # Identity & authentication service
│   ├── Gateway/              # API Gateway with YARP
│   ├── Frontend/
│   │   └── urbanx-frontend/  # React SPA
│   └── Shared/               # Shared libraries
├── docker-compose.yml        # Infrastructure setup
└── UrbanX.sln               # Solution file
```

## Development

### Backend Development
Each service can be run independently. They use in-memory or local PostgreSQL databases.

### Frontend Development
```bash
cd src/Frontend/urbanx-frontend
npm run dev
```

Hot reload is enabled for development.

## License

MIT