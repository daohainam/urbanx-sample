#!/bin/bash

# Start all backend services for UrbanX

echo "Starting UrbanX Services..."

# Start Catalog Service
echo "Starting Catalog Service..."
cd src/Services/Catalog/UrbanX.Services.Catalog
dotnet run --no-build &
CATALOG_PID=$!
cd ../../../..

# Wait a bit for service to start
sleep 2

# Start Order Service
echo "Starting Order Service..."
cd src/Services/Order/UrbanX.Services.Order
dotnet run --no-build &
ORDER_PID=$!
cd ../../../..

sleep 2

# Start Merchant Service
echo "Starting Merchant Service..."
cd src/Services/Merchant/UrbanX.Services.Merchant
dotnet run --no-build &
MERCHANT_PID=$!
cd ../../../..

sleep 2

# Start Payment Service
echo "Starting Payment Service..."
cd src/Services/Payment/UrbanX.Services.Payment
dotnet run --no-build &
PAYMENT_PID=$!
cd ../../../..

sleep 2

# Start Identity Service
echo "Starting Identity Service..."
cd src/Services/Identity/UrbanX.Services.Identity
dotnet run --no-build &
IDENTITY_PID=$!
cd ../../../..

sleep 2

# Start Gateway
echo "Starting API Gateway..."
cd src/Gateway/UrbanX.Gateway
dotnet run --no-build &
GATEWAY_PID=$!
cd ../..

echo "All services started!"
echo "Catalog: http://localhost:5001"
echo "Order: http://localhost:5002"
echo "Merchant: http://localhost:5003"
echo "Payment: http://localhost:5004"
echo "Identity: http://localhost:5005"
echo "Gateway: http://localhost:5000"
echo ""
echo "Press Ctrl+C to stop all services"

# Wait for user interrupt
wait
