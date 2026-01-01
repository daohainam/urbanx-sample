# Quick Setup Guide

## Prerequisites
- .NET 10 SDK
- Node.js 18+
- Docker & Docker Compose (for PostgreSQL and Kafka)

## Quick Start

### 1. Start Infrastructure
```bash
docker-compose up -d
```

This will start:
- PostgreSQL on port 5432
- Keycloak on port 8080
- Kafka on port 9092
- Zookeeper on port 2181

### 2. Build Backend
```bash
dotnet build UrbanX.sln
```

### 3. Start Services

#### Option A: Using Scripts
**Linux/Mac:**
```bash
./start-services.sh
```

**Windows:**
```powershell
.\start-services.ps1
```

#### Option B: Manual Start
Open separate terminals for each service:

```bash
# Terminal 1 - Gateway
cd src/Gateway/UrbanX.Gateway
dotnet run

# Terminal 2 - Catalog Service
cd src/Services/Catalog/UrbanX.Services.Catalog
dotnet run

# Terminal 3 - Order Service
cd src/Services/Order/UrbanX.Services.Order
dotnet run

# Terminal 4 - Merchant Service
cd src/Services/Merchant/UrbanX.Services.Merchant
dotnet run

# Terminal 5 - Payment Service
cd src/Services/Payment/UrbanX.Services.Payment
dotnet run

# Terminal 6 - Identity Service
cd src/Services/Identity/UrbanX.Services.Identity
dotnet run
```

### 4. Start Frontend
```bash
cd src/Frontend/urbanx-frontend
npm install
npm run dev
```

### 5. Access the Application
- **Frontend:** http://localhost:5173
- **API Gateway:** http://localhost:5000
- **Identity Server:** http://localhost:5005
- **Keycloak Admin Console:** http://localhost:8080 (admin/admin)

## Test Users
### Admin
- **Email:** admin@urbanx.com
- **Password:** Admin123!
- **Roles:** admin
  
### Merchants
- **Merchant 1:**
  - Email: merchant1@urbanx.com
  - Password: Merchant123!
  - Roles: merchant-editor, merchant-order-manager
  
- **Merchant 2:**
  - Email: merchant2@urbanx.com
  - Password: Merchant123!
  - Roles: merchant-editor

### Buyers/Customers
- **Buyer 1:**
  - Email: buyer1@urbanx.com
  - Password: Buyer123!
  - Roles: buyer
  
- **Buyer 2:**
  - Email: buyer2@urbanx.com
  - Password: Buyer123!
  - Roles: buyer

## Architecture Overview

```
┌─────────────┐
│   React     │ (Port 5173)
│   Frontend  │
└──────┬──────┘
       │
       │ HTTP
       ▼
┌─────────────┐
│   YARP      │ (Port 5000)
│   Gateway   │
└──────┬──────┘
       │
       ├───────────────┬──────────────┬──────────────┬──────────────┐
       │               │              │              │              │
       ▼               ▼              ▼              ▼              ▼
┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
│ Catalog  │   │  Order   │   │ Merchant │   │ Payment  │   │ Identity │
│ Service  │   │ Service  │   │ Service  │   │ Service  │   │ Service  │
│ (5001)   │   │ (5002)   │   │ (5003)   │   │ (5004)   │   │ (5005)   │
└────┬─────┘   └────┬─────┘   └────┬─────┘   └────┬─────┘   └────┬─────┘
     │              │              │              │              │
     ▼              ▼              ▼              ▼              ▼
┌────────────────────────────────────────────────────────────────────┐
│                     PostgreSQL (5432)                               │
└────────────────────────────────────────────────────────────────────┘
     ▲              ▲              ▲
     │              │              │
     └──────────────┴──────────────┘
              Kafka (9092)
```

## API Testing

You can test the APIs using the provided `.http` files in each service directory:
- `UrbanX.Services.Catalog.http`
- `UrbanX.Services.Order.http`
- `UrbanX.Services.Merchant.http`
- `UrbanX.Services.Payment.http`
- `UrbanX.Services.Identity.http`

Or use the Gateway at http://localhost:5000:

```bash
# Get products
curl http://localhost:5000/api/products

# Get cart for a customer
curl http://localhost:5000/api/cart/00000000-0000-0000-0000-000000000001
```

## Troubleshooting

### Port Already in Use
If you get port conflicts, you can change the ports in each service's `Properties/launchSettings.json` file.

### Database Connection Issues
Ensure PostgreSQL is running:
```bash
docker-compose ps
```

If not running:
```bash
docker-compose up -d postgres
```

### Frontend Build Issues
Clear node_modules and reinstall:
```bash
cd src/Frontend/urbanx-frontend
rm -rf node_modules package-lock.json
npm install
```

### Backend Build Issues
Clean and rebuild:
```bash
dotnet clean
dotnet build
```

## Development Tips

### Hot Reload
- Frontend: Hot reload is automatic with Vite
- Backend: Use `dotnet watch run` instead of `dotnet run` for hot reload

### Database Migrations
Each service has its own database context. To create migrations:

```bash
cd src/Services/Catalog/UrbanX.Services.Catalog
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Environment Variables
Copy `.env.example` to `.env` in the frontend directory and adjust as needed:
```bash
cp src/Frontend/urbanx-frontend/.env.example src/Frontend/urbanx-frontend/.env
```

## Next Steps

1. **Add Database Migrations:** Create and apply EF Core migrations for each service
2. **Implement Kafka Events:** Add event publishing/consuming between services
3. **Add Authentication:** Integrate OIDC authentication in the frontend
4. **Add Tests:** Create unit and integration tests
5. **Add Logging:** Implement structured logging with Serilog
6. **Add Health Checks:** Implement health check endpoints
7. **Add API Documentation:** Use Swagger/OpenAPI for API documentation
8. **Deploy:** Create Kubernetes manifests or Docker Compose for production
