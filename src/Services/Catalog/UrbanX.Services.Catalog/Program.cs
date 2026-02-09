using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;
using UrbanX.Services.Catalog;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>(name: "catalogdb", tags: new[] { "ready", "db" });

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations for CatalogDbContext...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
        
        // Seed data in development
        if (app.Environment.IsDevelopment())
        {
            await DataSeeder.SeedAsync(context);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

// Product search and browse
app.MapGet("/api/products", async (CatalogDbContext db, string? search, string? category, int page = 1, int pageSize = 20) =>
{
    var query = db.Products.Where(p => p.IsActive);
    
    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
    }
    
    if (!string.IsNullOrEmpty(category))
    {
        query = query.Where(p => p.Category == category);
    }
    
    var total = await query.CountAsync();
    var products = await query
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return Results.Ok(new { products, total, page, pageSize });
});

app.MapGet("/api/products/{id:guid}", async (Guid id, CatalogDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    
    var product = await db.Products.FindAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

app.MapGet("/api/products/merchant/{merchantId:guid}", async (Guid merchantId, CatalogDbContext db) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    
    var products = await db.Products
        .Where(p => p.MerchantId == merchantId && p.IsActive)
        .ToListAsync();
    return Results.Ok(products);
});

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
