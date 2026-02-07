# UrbanX Production Deployment Guide

This guide provides comprehensive instructions for deploying UrbanX to production environments.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Configuration](#configuration)
3. [Deployment Options](#deployment-options)
4. [Security Considerations](#security-considerations)
5. [Monitoring and Observability](#monitoring-and-observability)
6. [Database Migrations](#database-migrations)
7. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Tools
- **Docker** 20.10+ and **Docker Compose** 2.x
- **Kubernetes** 1.28+ (for K8s deployment)
- **kubectl** CLI tool
- **.NET 10 SDK** (for local builds)
- **PostgreSQL** 16+ (if not using containerized database)
- **Apache Kafka** (if not using containerized messaging)

### Required Secrets
Before deploying, you must configure the following secrets:

```bash
# Database
POSTGRES_PASSWORD=<strong-password>
DATABASE_CONNECTION_STRING=Host=postgres;Database=urbanx;Username=postgres;Password=<strong-password>

# Identity Server
IDENTITY_SERVER_URL=https://identity.yourdomain.com
IDENTITY_SERVER_ISSUER_URI=https://identity.yourdomain.com

# Frontend URLs
SPA_REDIRECT_URIS=https://app.yourdomain.com/callback
SPA_LOGOUT_URIS=https://app.yourdomain.com
SPA_CORS_ORIGINS=https://app.yourdomain.com

# Merchant Portal URLs
MERCHANT_SPA_REDIRECT_URIS=https://merchant.yourdomain.com/callback
MERCHANT_SPA_LOGOUT_URIS=https://merchant.yourdomain.com
MERCHANT_SPA_CORS_ORIGINS=https://merchant.yourdomain.com

# Observability
OTEL_COLLECTOR_ENDPOINT=http://otel-collector:4317

# CORS
CORS_ALLOWED_ORIGINS=https://app.yourdomain.com,https://merchant.yourdomain.com
```

## Configuration

### 1. Environment Variables

Create a `.env.production` file in the root directory:

```bash
# Copy the template
cp .env.example .env.production

# Edit with your production values
nano .env.production
```

### 2. Production Configuration Files

All services include `appsettings.Production.json` files with placeholder values marked with `${VARIABLE_NAME}`. These are replaced at runtime by environment variables.

**Example for Order Service:**
```json
{
  "ConnectionStrings": {
    "orderdb": "${DATABASE_CONNECTION_STRING}"
  },
  "OTEL_EXPORTER_OTLP_ENDPOINT": "${OTEL_COLLECTOR_ENDPOINT}"
}
```

### 3. Secrets Management

#### Option A: Kubernetes Secrets (Recommended for K8s)
```bash
kubectl create secret generic urbanx-secrets \
  --from-literal=POSTGRES_PASSWORD='your-password' \
  --from-literal=DATABASE_CONNECTION_STRING='your-connection-string' \
  -n urbanx
```

#### Option B: Azure Key Vault (Recommended for Azure)
1. Create Key Vault: `az keyvault create --name urbanx-vault --resource-group urbanx-rg`
2. Add secrets: `az keyvault secret set --vault-name urbanx-vault --name DatabasePassword --value 'your-password'`
3. Configure Managed Identity for services to access Key Vault

#### Option C: Environment Variables (Docker Compose)
Store in `.env.production` file (ensure this is in `.gitignore`)

## Deployment Options

### Option 1: Docker Compose (Recommended for Small/Medium Deployments)

#### Step 1: Build Images
```bash
# Build all service images
docker compose -f docker-compose.production.yml build
```

#### Step 2: Start Services
```bash
# Start with environment file
docker compose -f docker-compose.production.yml --env-file .env.production up -d
```

#### Step 3: Verify Deployment
```bash
# Check service health
docker compose -f docker-compose.production.yml ps

# Check logs
docker compose -f docker-compose.production.yml logs -f gateway
```

#### Step 4: Database Initialization
```bash
# Apply migrations (see Database Migrations section)
docker compose -f docker-compose.production.yml exec order-service dotnet ef database update
docker compose -f docker-compose.production.yml exec catalog-service dotnet ef database update
docker compose -f docker-compose.production.yml exec payment-service dotnet ef database update
docker compose -f docker-compose.production.yml exec merchant-service dotnet ef database update
```

### Option 2: Kubernetes (Recommended for Large Scale/High Availability)

#### Step 1: Create Namespace
```bash
kubectl apply -f kubernetes/infrastructure.yaml
```

#### Step 2: Configure Secrets
```bash
# Update the secrets in kubernetes/infrastructure.yaml with your values
# Then apply:
kubectl apply -f kubernetes/infrastructure.yaml
```

#### Step 3: Deploy Infrastructure
```bash
kubectl apply -f kubernetes/infrastructure.yaml
kubectl wait --for=condition=ready pod -l app=postgres -n urbanx --timeout=300s
```

#### Step 4: Deploy Services
```bash
kubectl apply -f kubernetes/services.yaml
```

#### Step 5: Verify Deployment
```bash
# Check pod status
kubectl get pods -n urbanx

# Check services
kubectl get services -n urbanx

# View logs
kubectl logs -f deployment/gateway -n urbanx
```

#### Step 6: Expose Gateway (LoadBalancer)
```bash
# Get external IP
kubectl get service gateway -n urbanx

# Or use Ingress
kubectl apply -f kubernetes/ingress.yaml  # Create this based on your ingress controller
```

### Option 3: Cloud-Specific Deployments

#### Azure Container Apps
```bash
# Create resource group
az group create --name urbanx-rg --location eastus

# Create Container Apps environment
az containerapp env create \
  --name urbanx-env \
  --resource-group urbanx-rg \
  --location eastus

# Deploy each service
az containerapp create \
  --name gateway \
  --resource-group urbanx-rg \
  --environment urbanx-env \
  --image ghcr.io/daohainam/urbanx-sample/urbanx-gateway:latest \
  --target-port 8080 \
  --ingress external
```

#### AWS ECS/Fargate
```bash
# Create ECS cluster
aws ecs create-cluster --cluster-name urbanx-cluster

# Register task definitions (see AWS documentation)
# Deploy services
```

## Security Considerations

### 1. HTTPS/TLS
- **Always use HTTPS in production**
- Configure SSL/TLS certificates
- For Kubernetes: Use cert-manager with Let's Encrypt

```bash
# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create ClusterIssuer for Let's Encrypt
kubectl apply -f kubernetes/cert-issuer.yaml
```

### 2. Network Security
- Use private networks for service-to-service communication
- Expose only the Gateway to the internet
- Configure firewall rules to restrict access

### 3. Database Security
- Use strong passwords (minimum 16 characters)
- Enable SSL/TLS for database connections
- Restrict database access to service IPs only
- Regular backups and encryption at rest

### 4. Secrets Management
- **NEVER commit secrets to version control**
- Use dedicated secrets management (Key Vault, Secrets Manager, etc.)
- Rotate secrets regularly (every 90 days minimum)
- Use managed identities where possible

### 5. Authentication & Authorization
- Configure proper OAuth2 scopes
- Implement rate limiting
- Enable audit logging for authentication events
- Use strong signing keys for JWT tokens

## Monitoring and Observability

### 1. Health Checks
All services expose health endpoints:
- **Liveness**: `/alive` - Checks if service is running
- **Readiness**: `/health` - Checks if service is ready to accept traffic

### 2. OpenTelemetry Integration

#### Deploy OpenTelemetry Collector
```bash
# Using Kubernetes
kubectl apply -f https://github.com/open-telemetry/opentelemetry-operator/releases/latest/download/opentelemetry-operator.yaml
kubectl apply -f kubernetes/otel-collector.yaml
```

#### Configure Backends
Connect to your observability platform:
- **Grafana Cloud**
- **Azure Application Insights**
- **AWS CloudWatch**
- **Datadog**
- **New Relic**

### 3. Logging
All services use structured logging. Configure log aggregation:
- **Elasticsearch + Kibana**
- **Azure Log Analytics**
- **AWS CloudWatch Logs**

```bash
# View logs in Kubernetes
kubectl logs -f deployment/gateway -n urbanx

# With Docker Compose
docker compose -f docker-compose.production.yml logs -f
```

### 4. Metrics and Alerts

Configure alerts for:
- High error rates (>5% for 5 minutes)
- High response times (>1s P95)
- Pod/Container restarts
- Database connection failures
- Memory/CPU usage (>80%)

## Database Migrations

### Important: Migration Strategy

The application currently uses `EnsureCreatedAsync()` which is **NOT production-ready**. You must implement proper migrations:

### Step 1: Create Migrations

For each service:
```bash
cd src/Services/Order/UrbanX.Services.Order
dotnet ef migrations add InitialCreate
dotnet ef migrations script > migrations.sql
```

### Step 2: Apply Migrations in Production

#### Manual Approach:
```bash
# Review the SQL script first
cat migrations.sql

# Apply using psql
psql -h postgres -U postgres -d urbanx_order -f migrations.sql
```

#### Automated Approach (K8s Job):
```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: order-migration
  namespace: urbanx
spec:
  template:
    spec:
      containers:
      - name: migration
        image: ghcr.io/daohainam/urbanx-sample/urbanx-order:latest
        command: ["dotnet", "ef", "database", "update"]
      restartPolicy: Never
```

### Step 3: Backup Before Migration
```bash
# PostgreSQL backup
pg_dump -h postgres -U postgres urbanx > backup-$(date +%Y%m%d).sql
```

## Troubleshooting

### Common Issues

#### 1. Services Can't Connect to Database
```bash
# Check database is running
kubectl get pods -l app=postgres -n urbanx

# Check connection string
kubectl get secret urbanx-secrets -n urbanx -o yaml

# Test connection
kubectl run -it --rm psql --image=postgres:16 -- psql -h postgres -U postgres
```

#### 2. Services Can't Discover Each Other
```bash
# Check service DNS
kubectl run -it --rm debug --image=busybox -- nslookup catalog-service.urbanx.svc.cluster.local

# Check service endpoints
kubectl get endpoints -n urbanx
```

#### 3. High Memory/CPU Usage
```bash
# Check resource usage
kubectl top pods -n urbanx

# Adjust resource limits in kubernetes/services.yaml
```

#### 4. Database Connection Pool Exhausted
- Increase `MaxPoolSize` in connection string
- Check for connection leaks in application code
- Scale database vertically

### Getting Support
- Check logs: `kubectl logs -f <pod-name> -n urbanx`
- Check events: `kubectl get events -n urbanx --sort-by='.lastTimestamp'`
- Review health endpoints: `curl http://service:8080/health`

## Scaling

### Horizontal Scaling

#### Kubernetes:
```bash
# Scale specific service
kubectl scale deployment order-service --replicas=5 -n urbanx

# Or use Horizontal Pod Autoscaler
kubectl autoscale deployment order-service --cpu-percent=70 --min=2 --max=10 -n urbanx
```

#### Docker Compose:
```bash
docker compose -f docker-compose.production.yml up -d --scale order-service=3
```

### Vertical Scaling
Adjust resource limits in `kubernetes/services.yaml`:
```yaml
resources:
  requests:
    memory: "512Mi"
    cpu: "500m"
  limits:
    memory: "1Gi"
    cpu: "1000m"
```

## Backup and Disaster Recovery

### Database Backups
```bash
# Daily backup script
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
pg_dump -h postgres -U postgres urbanx > /backups/urbanx_$DATE.sql
# Upload to S3/Azure Blob/GCS
```

### Service State
- All services are stateless
- State is in PostgreSQL and Kafka
- Focus on backing up data stores

## Performance Tuning

### Database Optimization
- Add indexes on frequently queried columns
- Configure connection pooling: `Pooling=true;MinPoolSize=5;MaxPoolSize=100`
- Enable query plan caching

### Service Optimization
- Enable response caching where appropriate
- Use Redis for distributed caching
- Configure HTTP client connection pooling

### Network Optimization
- Use CDN for static assets
- Enable HTTP/2
- Configure compression (already enabled via ASP.NET Core)

## Cost Optimization

### Infrastructure
- Use spot instances for non-critical workloads
- Right-size your services (review resource usage monthly)
- Use reserved instances for predictable workloads

### Monitoring
- Use sampling for traces (e.g., 10% of requests)
- Aggregate logs before sending to backend
- Retain logs based on compliance requirements

---

## Quick Reference

### Health Check URLs
- Gateway: `http://gateway:8080/alive`
- All Services: `http://<service>:8080/alive`

### Important Ports
- Gateway: 8080 (internal), 5000 (external)
- Services: 8080 (internal)
- PostgreSQL: 5432
- Kafka: 9092

### Key Files
- Production config: `appsettings.Production.json`
- Docker Compose: `docker-compose.production.yml`
- Kubernetes manifests: `kubernetes/*.yaml`
- CI/CD pipeline: `.github/workflows/build-and-test.yml`
