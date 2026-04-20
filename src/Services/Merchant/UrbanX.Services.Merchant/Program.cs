using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Merchant.Data;
using UrbanX.Services.Merchant.Models;
using UrbanX.Services.Merchant;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<MerchantDbContext>("merchantdb");

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MerchantDbContext>(name: "merchantdb", tags: ["ready", "db"]);

// Configure JWT bearer authentication
var identityAuthority = builder.Configuration["services__identity__https__0"]
    ?? builder.Configuration["services__identity__http__0"]
    ?? builder.Configuration["IdentityServer:Authority"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = identityAuthority;
        options.Audience = builder.Configuration["IdentityServer:Audience"] ?? "urbanx-api";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddUrbanXAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseProductionDefaults();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MerchantDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations for MerchantDbContext...");
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

// Merchant management
app.MapGet("/api/merchants/{id:guid}", async (Guid id, MerchantDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    
    var merchant = await db.Merchants.FindAsync(id);
    return merchant is not null ? Results.Ok(merchant) : Results.NotFound();
});

app.MapPost("/api/merchants", async (UrbanX.Services.Merchant.Models.Merchant merchant, MerchantDbContext db) =>
{
    RequestValidation.ValidateRequiredString(merchant.Name, nameof(merchant.Name), 200);
    RequestValidation.ValidateRequiredString(merchant.Email, nameof(merchant.Email), 100);
    RequestValidation.ValidateEmail(merchant.Email);
    
    merchant.Id = Guid.NewGuid();
    merchant.CreatedAt = DateTime.UtcNow;
    merchant.UpdatedAt = DateTime.UtcNow;
    merchant.IsActive = true;
    
    db.Merchants.Add(merchant);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/merchants/{merchant.Id}", merchant);
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

// Product management for merchants
app.MapGet("/api/merchants/{merchantId:guid}/products", async (Guid merchantId, MerchantDbContext db) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    
    var products = await db.Products
        .AsNoTracking()
        .Where(p => p.MerchantId == merchantId)
        .ToListAsync();
    return Results.Ok(products);
});

app.MapPost("/api/merchants/{merchantId:guid}/products", async (Guid merchantId, MerchantProduct product, MerchantDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    RequestValidation.ValidateRequiredString(product.Name, nameof(product.Name), 200);
    RequestValidation.ValidatePositive(product.Price, nameof(product.Price));

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != merchantId)
        return Results.Forbid();

    product.Id = Guid.NewGuid();
    product.MerchantId = merchantId;
    product.CreatedAt = DateTime.UtcNow;
    product.UpdatedAt = DateTime.UtcNow;
    product.IsActive = true;
    
    db.Products.Add(product);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/merchants/{merchantId}/products/{product.Id}", product);
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.MapPut("/api/merchants/{merchantId:guid}/products/{productId:guid}", async (Guid merchantId, Guid productId, MerchantProduct updatedProduct, MerchantDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    RequestValidation.ValidateGuid(productId, nameof(productId));
    RequestValidation.ValidateRequiredString(updatedProduct.Name, nameof(updatedProduct.Name), 200);
    RequestValidation.ValidatePositive(updatedProduct.Price, nameof(updatedProduct.Price));

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != merchantId)
        return Results.Forbid();

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
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.MapDelete("/api/merchants/{merchantId:guid}/products/{productId:guid}", async (Guid merchantId, Guid productId, MerchantDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    RequestValidation.ValidateGuid(productId, nameof(productId));

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != merchantId)
        return Results.Forbid();

    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.MerchantId == merchantId);
    if (product == null) return Results.NotFound();
    
    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

// Order management for merchants
app.MapGet("/api/merchants/{merchantId:guid}/orders", async (Guid merchantId, MerchantDbContext db) =>
{
    // This would typically query the Order service for orders containing this merchant's products
    // For now, returning empty list as a placeholder
    return Results.Ok(new List<object>());
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

// Category management for merchants
app.MapGet("/api/merchants/{merchantId:guid}/categories", async (Guid merchantId, MerchantDbContext db) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));

    var categories = await db.Categories
        .AsNoTracking()
        .Where(c => c.MerchantId == merchantId)
        .ToListAsync();
    return Results.Ok(categories);
});

app.MapPost("/api/merchants/{merchantId:guid}/categories", async (Guid merchantId, MerchantCategory category, MerchantDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    RequestValidation.ValidateRequiredString(category.Name, nameof(category.Name), 200);

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != merchantId)
        return Results.Forbid();

    category.Id = Guid.NewGuid();
    category.MerchantId = merchantId;
    category.CreatedAt = DateTime.UtcNow;
    category.UpdatedAt = DateTime.UtcNow;
    category.IsActive = true;
    
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/merchants/{merchantId}/categories/{category.Id}", category);
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.MapPut("/api/merchants/{merchantId:guid}/categories/{categoryId:guid}", async (Guid merchantId, Guid categoryId, MerchantCategory updatedCategory, MerchantDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    RequestValidation.ValidateGuid(categoryId, nameof(categoryId));
    RequestValidation.ValidateRequiredString(updatedCategory.Name, nameof(updatedCategory.Name), 200);

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != merchantId)
        return Results.Forbid();

    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.MerchantId == merchantId);
    if (category == null) return Results.NotFound();
    
    category.Name = updatedCategory.Name;
    category.Description = updatedCategory.Description;
    category.IsActive = updatedCategory.IsActive;
    category.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    return Results.Ok(category);
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.MapDelete("/api/merchants/{merchantId:guid}/categories/{categoryId:guid}", async (Guid merchantId, Guid categoryId, MerchantDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    RequestValidation.ValidateGuid(categoryId, nameof(categoryId));

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != merchantId)
        return Results.Forbid();

    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.MerchantId == merchantId);
    if (category == null) return Results.NotFound();
    
    db.Categories.Remove(category);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
