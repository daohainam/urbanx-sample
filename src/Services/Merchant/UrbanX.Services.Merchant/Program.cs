using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Merchant.Data;
using UrbanX.Services.Merchant.Models;
using UrbanX.Services.Merchant;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<MerchantDbContext>("merchantdb");

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Ensure database is created and seeded
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MerchantDbContext>();
        await context.Database.EnsureCreatedAsync();
        await DataSeeder.SeedAsync(context);
    }
}

// Merchant management
app.MapGet("/api/merchants/{id:guid}", async (Guid id, MerchantDbContext db) =>
{
    var merchant = await db.Merchants.FindAsync(id);
    return merchant is not null ? Results.Ok(merchant) : Results.NotFound();
});

app.MapPost("/api/merchants", async (UrbanX.Services.Merchant.Models.Merchant merchant, MerchantDbContext db) =>
{
    merchant.Id = Guid.NewGuid();
    merchant.CreatedAt = DateTime.UtcNow;
    merchant.UpdatedAt = DateTime.UtcNow;
    merchant.IsActive = true;
    
    db.Merchants.Add(merchant);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/merchants/{merchant.Id}", merchant);
});

// Product management for merchants
app.MapGet("/api/merchants/{merchantId:guid}/products", async (Guid merchantId, MerchantDbContext db) =>
{
    var products = await db.Products
        .Where(p => p.MerchantId == merchantId)
        .ToListAsync();
    return Results.Ok(products);
});

app.MapPost("/api/merchants/{merchantId:guid}/products", async (Guid merchantId, MerchantProduct product, MerchantDbContext db) =>
{
    product.Id = Guid.NewGuid();
    product.MerchantId = merchantId;
    product.CreatedAt = DateTime.UtcNow;
    product.UpdatedAt = DateTime.UtcNow;
    product.IsActive = true;
    
    db.Products.Add(product);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/merchants/{merchantId}/products/{product.Id}", product);
});

app.MapPut("/api/merchants/{merchantId:guid}/products/{productId:guid}", async (Guid merchantId, Guid productId, MerchantProduct updatedProduct, MerchantDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.MerchantId == merchantId);
    if (product == null) return Results.NotFound();
    
    product.Name = updatedProduct.Name;
    product.Description = updatedProduct.Description;
    product.Price = updatedProduct.Price;
    product.StockQuantity = updatedProduct.StockQuantity;
    product.ImageUrl = updatedProduct.ImageUrl;
    product.Category = updatedProduct.Category;
    product.IsActive = updatedProduct.IsActive;
    product.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    return Results.Ok(product);
});

app.MapDelete("/api/merchants/{merchantId:guid}/products/{productId:guid}", async (Guid merchantId, Guid productId, MerchantDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.MerchantId == merchantId);
    if (product == null) return Results.NotFound();
    
    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Order management for merchants
app.MapGet("/api/merchants/{merchantId:guid}/orders", async (Guid merchantId, MerchantDbContext db) =>
{
    // This would typically query the Order service for orders containing this merchant's products
    // For now, returning empty list as a placeholder
    return Results.Ok(new List<object>());
});

// Category management for merchants
app.MapGet("/api/merchants/{merchantId:guid}/categories", async (Guid merchantId, MerchantDbContext db) =>
{
    var categories = await db.Categories
        .Where(c => c.MerchantId == merchantId)
        .ToListAsync();
    return Results.Ok(categories);
});

app.MapPost("/api/merchants/{merchantId:guid}/categories", async (Guid merchantId, MerchantCategory category, MerchantDbContext db) =>
{
    category.Id = Guid.NewGuid();
    category.MerchantId = merchantId;
    category.CreatedAt = DateTime.UtcNow;
    category.UpdatedAt = DateTime.UtcNow;
    category.IsActive = true;
    
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/merchants/{merchantId}/categories/{category.Id}", category);
});

app.MapPut("/api/merchants/{merchantId:guid}/categories/{categoryId:guid}", async (Guid merchantId, Guid categoryId, MerchantCategory updatedCategory, MerchantDbContext db) =>
{
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.MerchantId == merchantId);
    if (category == null) return Results.NotFound();
    
    category.Name = updatedCategory.Name;
    category.Description = updatedCategory.Description;
    category.IsActive = updatedCategory.IsActive;
    category.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    return Results.Ok(category);
});

app.MapDelete("/api/merchants/{merchantId:guid}/categories/{categoryId:guid}", async (Guid merchantId, Guid categoryId, MerchantDbContext db) =>
{
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.MerchantId == merchantId);
    if (category == null) return Results.NotFound();
    
    db.Categories.Remove(category);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
