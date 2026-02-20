# Quick Setup Guide

## Prerequisites
- .NET 10 SDK
- Node.js 20+
- Docker & Docker Compose

## Recommended: .NET Aspire

The fastest way to run the entire platform locally is with .NET Aspire, which automatically
provisions PostgreSQL and Kafka, wires up service discovery, and provides a live dashboard.

```bash
# 1. Install Aspire workload (once)
dotnet workload install aspire

# 2. Start all backend services
cd src/AppHost/UrbanX.AppHost
dotnet run

# 3. Start the frontend (separate terminal)
cd src/Frontend/urbanx-react
npm install
npm run dev
```

- **Aspire Dashboard:** http://localhost:15260
- **Frontend:** http://localhost:5173
- **API Gateway:** Dynamically assigned (visible in the Aspire Dashboard)

## Manual Setup

### 1. Start Infrastructure
```bash
docker-compose up -d
```

This starts:
- PostgreSQL on port 5432
- Kafka on port 9092
- Zookeeper on port 2181

### 2. Start Backend Services

Open a separate terminal for each service:

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

You can also use the helper scripts:

```bash
# Linux/macOS
./start-services.sh

# Windows PowerShell
.\start-services.ps1
```

### 3. Start the Frontend
```bash
cd src/Frontend/urbanx-react
npm install
npm run dev
```

### 4. Access the Application
- **Frontend:** http://localhost:5173
- **API Gateway:** http://localhost:5000
- **Identity Server:** http://localhost:5005

## API Testing

`.http` files are included in each service directory for quick endpoint testing:
- `UrbanX.Services.Catalog.http`
- `UrbanX.Services.Order.http`
- `UrbanX.Services.Merchant.http`
- `UrbanX.Services.Payment.http`

Or test via the Gateway directly:

```bash
# List products (public)
curl http://localhost:5000/api/products

# Get a specific product
curl http://localhost:5000/api/products/<product-id>
```

## Running Tests
```bash
dotnet test UrbanX.sln
```

## Troubleshooting

### Port Already in Use
Change the port in the service's `Properties/launchSettings.json`.

### Database Connection Issues
```bash
docker-compose ps              # check container status
docker-compose up -d postgres  # restart if needed
```

### Frontend Build Issues
```bash
cd src/Frontend/urbanx-react
rm -rf node_modules package-lock.json
npm install
```

### Backend Build Issues
```bash
dotnet clean && dotnet build
```

## Development Tips

### Hot Reload
- Frontend: automatic with Vite (`npm run dev`)
- Backend: use `dotnet watch run` for hot reload

### Database Migrations
Migrations are applied automatically on startup. To add a new migration:
```bash
cd src/Services/<Service>/UrbanX.Services.<Service>
dotnet ef migrations add <MigrationName> --context <Service>DbContext
```

See [DATABASE_MIGRATIONS.md](DATABASE_MIGRATIONS.md) for full guidance.
