# Start all backend services for UrbanX

Write-Host "Starting UrbanX Services..." -ForegroundColor Green

# Start Catalog Service
Write-Host "Starting Catalog Service..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -WorkingDirectory "src/Services/Catalog/UrbanX.Services.Catalog" -WindowStyle Hidden

Start-Sleep -Seconds 2

# Start Order Service
Write-Host "Starting Order Service..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -WorkingDirectory "src/Services/Order/UrbanX.Services.Order" -WindowStyle Hidden

Start-Sleep -Seconds 2

# Start Merchant Service
Write-Host "Starting Merchant Service..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -WorkingDirectory "src/Services/Merchant/UrbanX.Services.Merchant" -WindowStyle Hidden

Start-Sleep -Seconds 2

# Start Payment Service
Write-Host "Starting Payment Service..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -WorkingDirectory "src/Services/Payment/UrbanX.Services.Payment" -WindowStyle Hidden

Start-Sleep -Seconds 2

# Start Identity Service
Write-Host "Starting Identity Service..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -WorkingDirectory "src/Services/Identity/UrbanX.Services.Identity" -WindowStyle Hidden

Start-Sleep -Seconds 2

# Start Gateway
Write-Host "Starting API Gateway..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -WorkingDirectory "src/Gateway/UrbanX.Gateway" -WindowStyle Hidden

Write-Host "`nAll services started!" -ForegroundColor Green
Write-Host "Catalog: http://localhost:5001" -ForegroundColor Yellow
Write-Host "Order: http://localhost:5002" -ForegroundColor Yellow
Write-Host "Merchant: http://localhost:5003" -ForegroundColor Yellow
Write-Host "Payment: http://localhost:5004" -ForegroundColor Yellow
Write-Host "Identity: http://localhost:5005" -ForegroundColor Yellow
Write-Host "Gateway: http://localhost:5000" -ForegroundColor Yellow
Write-Host "`nPress any key to stop all services..." -ForegroundColor White

$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Stop all dotnet processes
Get-Process dotnet | Stop-Process -Force
Write-Host "All services stopped." -ForegroundColor Green
