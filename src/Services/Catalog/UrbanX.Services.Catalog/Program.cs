using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;
using UrbanX.Services.Catalog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CatalogDb") 
        ?? "Host=localhost;Database=urbanx_catalog;Username=postgres;Password=postgres"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Ensure database is created and seeded
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.EnsureCreatedAsync();
        await DataSeeder.SeedAsync(context);
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
    var product = await db.Products.FindAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

app.MapGet("/api/products/merchant/{merchantId:guid}", async (Guid merchantId, CatalogDbContext db) =>
{
    var products = await db.Products
        .Where(p => p.MerchantId == merchantId && p.IsActive)
        .ToListAsync();
    return Results.Ok(products);
});

app.Run();
