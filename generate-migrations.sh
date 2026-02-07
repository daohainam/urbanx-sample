#!/bin/bash

# Migration Generation Script for UrbanX Services
# This script generates initial EF Core migrations for all services

set -e  # Exit on error

echo "=========================================="
echo "UrbanX Database Migration Generator"
echo "=========================================="
echo ""

# Check if dotnet ef is installed
if ! command -v dotnet-ef &> /dev/null; then
    echo "Error: dotnet-ef tool is not installed"
    echo "Install it with: dotnet tool install --global dotnet-ef"
    exit 1
fi

# Array of services with their contexts
declare -A services
services=(
    ["Order"]="OrderDbContext"
    ["Catalog"]="CatalogDbContext"
    ["Payment"]="PaymentDbContext"
    ["Merchant"]="MerchantDbContext"
)

MIGRATION_NAME="${1:-InitialCreate}"
echo "Migration name: $MIGRATION_NAME"
echo ""

# Track results
declare -a success=()
declare -a failed=()

# Generate migrations for each service
for service in "${!services[@]}"; do
    context="${services[$service]}"
    project_path="src/Services/$service/UrbanX.Services.$service"
    
    echo "----------------------------------------"
    echo "Processing: $service Service"
    echo "Context: $context"
    echo "Path: $project_path"
    echo "----------------------------------------"
    
    if [ ! -d "$project_path" ]; then
        echo "❌ Error: Project directory not found: $project_path"
        failed+=("$service")
        continue
    fi
    
    cd "$project_path"
    
    # Check if migrations folder exists
    if [ -d "Migrations" ]; then
        echo "⚠️  Warning: Migrations folder already exists. This will add a new migration."
        read -p "Continue? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            echo "Skipped $service"
            cd - > /dev/null
            continue
        fi
    fi
    
    # Generate migration
    echo "Generating migration..."
    if dotnet ef migrations add "$MIGRATION_NAME" --context "$context"; then
        echo "✅ Success: Migration created for $service service"
        success+=("$service")
        
        # Generate SQL script for review
        echo "Generating SQL script for review..."
        dotnet ef migrations script --context "$context" --output "Migrations/${MIGRATION_NAME}.sql"
        echo "📝 SQL script saved to: Migrations/${MIGRATION_NAME}.sql"
    else
        echo "❌ Error: Failed to create migration for $service service"
        failed+=("$service")
    fi
    
    cd - > /dev/null
    echo ""
done

# Summary
echo "=========================================="
echo "Migration Generation Summary"
echo "=========================================="
echo ""
echo "✅ Successful: ${#success[@]}"
for svc in "${success[@]}"; do
    echo "  - $svc"
done
echo ""
echo "❌ Failed: ${#failed[@]}"
for svc in "${failed[@]}"; do
    echo "  - $svc"
done
echo ""

if [ ${#failed[@]} -eq 0 ]; then
    echo "🎉 All migrations generated successfully!"
    echo ""
    echo "Next steps:"
    echo "1. Review the generated migrations in each service's Migrations folder"
    echo "2. Review the SQL scripts: Migrations/${MIGRATION_NAME}.sql"
    echo "3. Update Program.cs in each service to use Database.MigrateAsync()"
    echo "4. Test migrations on development database"
    echo "5. Commit the migration files to version control"
    echo ""
    echo "See DATABASE_MIGRATIONS.md for detailed instructions"
    exit 0
else
    echo "⚠️  Some migrations failed. Please fix the errors and try again."
    exit 1
fi
