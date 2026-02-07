# Database Migration Guide

## Overview
This guide explains how to properly manage database schema changes using Entity Framework Core migrations instead of the development-only `EnsureCreatedAsync()` method.

## Why Migrations Are Critical

### Problems with EnsureCreatedAsync
- ❌ No schema version tracking
- ❌ Cannot handle schema changes (will fail on second run)
- ❌ No rollback capability
- ❌ Data loss risk when schema changes
- ❌ Not suitable for production

### Benefits of EF Core Migrations
- ✅ Schema version control
- ✅ Safe schema evolution
- ✅ Rollback capability
- ✅ Team collaboration support
- ✅ Production-ready

## Prerequisites

Install EF Core tools globally:
```bash
dotnet tool install --global dotnet-ef
# Or update if already installed
dotnet tool update --global dotnet-ef
```

## Creating Migrations

### Step 1: Generate Initial Migrations

For each service, navigate to the project directory and create migrations:

#### Order Service
```bash
cd src/Services/Order/UrbanX.Services.Order
dotnet ef migrations add InitialCreate --context OrderDbContext
```

#### Catalog Service
```bash
cd src/Services/Catalog/UrbanX.Services.Catalog
dotnet ef migrations add InitialCreate --context CatalogDbContext
```

#### Payment Service
```bash
cd src/Services/Payment/UrbanX.Services.Payment
dotnet ef migrations add InitialCreate --context PaymentDbContext
```

#### Merchant Service
```bash
cd src/Services/Merchant/UrbanX.Services.Merchant
dotnet ef migrations add InitialCreate --context MerchantDbContext
```

### Step 2: Review Generated Migrations

Each service will now have a `Migrations` folder with:
- `{Timestamp}_InitialCreate.cs` - The migration code
- `{Timestamp}_InitialCreate.Designer.cs` - Snapshot metadata
- `{DbContext}ModelSnapshot.cs` - Current model state

**Always review the generated SQL before applying:**
```bash
dotnet ef migrations script --context OrderDbContext
```

### Step 3: Update Program.cs

Replace `EnsureCreatedAsync()` with proper migration:

**Before (Development Only):**
```csharp
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
```

**After (Production Ready):**
```csharp
// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        throw;
    }
}
```

## Applying Migrations

### Development Environment
Migrations are automatically applied on application startup using `Database.MigrateAsync()`.

### Production Environment

#### Option 1: Application Startup (Simple)
Use `Database.MigrateAsync()` in Program.cs (as shown above). The application will apply pending migrations on startup.

**Pros:**
- Simple to implement
- Automatic
- Works with containerized deployments

**Cons:**
- Requires database permissions for schema changes
- Multiple instances may attempt migration simultaneously (mitigated by EF Core locks)
- Startup delay while migrations run

#### Option 2: Migration Script (Recommended for Large Databases)
Generate SQL scripts and apply manually:

```bash
# Generate SQL script
dotnet ef migrations script --idempotent --context OrderDbContext --output migrations.sql

# Apply using psql
psql -h postgres-host -U postgres -d urbanx_order -f migrations.sql
```

**Pros:**
- Full control over when migrations run
- Can review changes before applying
- No risk of concurrent migration attempts
- Application doesn't need schema modification permissions

**Cons:**
- Manual process
- Requires separate deployment step

#### Option 3: Kubernetes Migration Job (Recommended for K8s)
Run migrations as a pre-deployment Job:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: order-service-migration
  namespace: urbanx
  annotations:
    "helm.sh/hook": pre-install,pre-upgrade
    "helm.sh/hook-delete-policy": before-hook-creation
spec:
  template:
    spec:
      containers:
      - name: migration
        image: ghcr.io/daohainam/urbanx-sample/urbanx-order:latest
        command:
          - dotnet
          - ef
          - database
          - update
          - --context
          - OrderDbContext
        env:
        - name: DATABASE_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: urbanx-secrets
              key: DATABASE_CONNECTION_STRING
      restartPolicy: Never
  backoffLimit: 3
```

## Managing Schema Changes

### Adding a New Migration

When you modify your models:

```bash
cd src/Services/Order/UrbanX.Services.Order
dotnet ef migrations add AddOrderNotes --context OrderDbContext
```

This creates a new migration with only the changes since the last migration.

### Reviewing Migration
```bash
# View the SQL that will be executed
dotnet ef migrations script AddOrderNotes --context OrderDbContext

# View just the last migration
dotnet ef migrations script <PreviousMigration> AddOrderNotes --context OrderDbContext
```

### Applying the Migration
Development:
```bash
dotnet ef database update --context OrderDbContext
```

Production:
```bash
# Generate idempotent script (safe to run multiple times)
dotnet ef migrations script --idempotent --context OrderDbContext > update.sql
psql -h postgres-host -U postgres -d urbanx_order -f update.sql
```

## Rolling Back Migrations

### Revert to a Previous Migration
```bash
# List all migrations
dotnet ef migrations list --context OrderDbContext

# Revert to specific migration
dotnet ef database update InitialCreate --context OrderDbContext
```

### Remove Last Migration (Before Applying)
```bash
# Remove the last migration (only if not yet applied)
dotnet ef migrations remove --context OrderDbContext
```

## Best Practices

### 1. Never Modify Applied Migrations
Once a migration has been applied to any environment (especially production), never modify it. Instead, create a new migration.

### 2. Always Backup Before Migration
```bash
# PostgreSQL backup
pg_dump -h postgres-host -U postgres urbanx_order > backup_$(date +%Y%m%d_%H%M%S).sql
```

### 3. Test Migrations on Staging
Always test migrations on a staging environment with production-like data before applying to production.

### 4. Use Idempotent Scripts
Always use `--idempotent` flag for production scripts:
```bash
dotnet ef migrations script --idempotent --context OrderDbContext
```

### 5. Monitor Migration Performance
For large tables, migrations may take time. Consider:
- Running during maintenance windows
- Adding indexes in separate migrations
- Using `CREATE INDEX CONCURRENTLY` for PostgreSQL (requires raw SQL)

### 6. Version Control
Always commit migration files to version control:
```bash
git add src/Services/Order/UrbanX.Services.Order/Migrations/
git commit -m "Add InitialCreate migration for Order service"
```

## Handling Multiple Databases

Each service has its own database. Migrations must be created and applied for each:

```bash
# Script to create all migrations
#!/bin/bash
services=("Order" "Catalog" "Payment" "Merchant")

for service in "${services[@]}"; do
    echo "Creating migration for $service service..."
    cd "src/Services/$service/UrbanX.Services.$service"
    dotnet ef migrations add InitialCreate --context "${service}DbContext"
    cd ../../../../
done
```

## Connection String Configuration

### Development
Uses connection string from `appsettings.Development.json`

### Production
Use environment variables:
```bash
export DATABASE_CONNECTION_STRING="Host=postgres;Database=urbanx_order;Username=urbanx_order_user;Password=<strong-password>;SslMode=Require"
```

Or in Kubernetes:
```yaml
env:
- name: DATABASE_CONNECTION_STRING
  valueFrom:
    secretKeyRef:
      name: urbanx-secrets
      key: ORDER_DB_CONNECTION
```

## Troubleshooting

### Migration Already Applied Error
If you see "migration has already been applied", it means the migration was previously run. Use idempotent scripts or skip to the next migration.

### Connection String Not Found
Ensure environment variables are set:
```bash
echo $DATABASE_CONNECTION_STRING
```

### Permission Denied
Ensure the database user has schema modification permissions:
```sql
GRANT CREATE ON DATABASE urbanx_order TO urbanx_order_user;
GRANT ALL ON SCHEMA public TO urbanx_order_user;
```

### Multiple Contexts in Same Project
Always specify the context:
```bash
dotnet ef migrations add MigrationName --context OrderDbContext
```

## Migration Checklist

### Before Creating Migration
- [ ] All model changes committed
- [ ] Build succeeds
- [ ] Unit tests pass
- [ ] Context configured correctly

### Before Applying to Production
- [ ] Migration reviewed
- [ ] SQL script generated and reviewed
- [ ] Database backed up
- [ ] Tested on staging environment
- [ ] Rollback plan documented
- [ ] Team notified (if downtime expected)
- [ ] Maintenance window scheduled (if needed)

### After Applying
- [ ] Migration applied successfully
- [ ] Application started successfully
- [ ] Health checks passing
- [ ] Smoke tests completed
- [ ] Backup verified
- [ ] Team notified of completion

## Additional Resources

- [EF Core Migrations Documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Managing Migrations in Production](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [PostgreSQL Best Practices](https://www.postgresql.org/docs/current/ddl.html)
