using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Catalog;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Messaging;
using UrbanX.Services.Catalog.Models;
using UrbanX.Services.Catalog.Search;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>(name: "catalogdb", tags: ["ready", "db"]);

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

// Configure Elasticsearch (read side)
var elasticsearchUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
builder.Services.AddSingleton(new ElasticsearchClient(new Uri(elasticsearchUri)));
builder.Services.AddSingleton<IProductSearchService, ElasticsearchProductSearchService>();

// Configure Kafka producer (write side event publishing)
builder.Services.AddSingleton<IProductEventPublisher, KafkaProductEventPublisher>();

// Configure Kafka consumer background service (syncs events to Elasticsearch)
builder.Services.AddHostedService<KafkaProductEventConsumer>();

// Configure outbox relay service (publishes pending outbox messages to Kafka)
builder.Services.AddHostedService<OutboxRelayService>();

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

// ────────────────────────────────────────────────────────
// READ endpoints – query Elasticsearch (CQRS read side)
// ────────────────────────────────────────────────────────

// Product search and browse
app.MapGet("/api/products", async (IProductSearchService searchService, string? search, string? category, int page = 1, int pageSize = 20) =>
{
    pageSize = RequestValidation.ValidatePageSize(pageSize);
    var result = await searchService.SearchAsync(search, category, page, pageSize);
    return Results.Ok(new { products = result.Products, total = result.Total, page = result.Page, pageSize = result.PageSize });
});

app.MapGet("/api/products/{id:guid}", async (Guid id, IProductSearchService searchService, CatalogDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));

    // Try Elasticsearch first; fall back to PostgreSQL
    var document = await searchService.GetByIdAsync(id);
    if (document is not null) return Results.Ok(document);

    var product = await db.Products.FindAsync(id);
    return product is not null && product.IsActive ? Results.Ok(product) : Results.NotFound();
});

app.MapGet("/api/products/merchant/{merchantId:guid}", async (Guid merchantId, IProductSearchService searchService, int page = 1, int pageSize = 20) =>
{
    RequestValidation.ValidateGuid(merchantId, nameof(merchantId));
    pageSize = RequestValidation.ValidatePageSize(pageSize);

    var result = await searchService.GetByMerchantAsync(merchantId, page, pageSize);
    return Results.Ok(new { products = result.Products, total = result.Total, page = result.Page, pageSize = result.PageSize });
});

// ────────────────────────────────────────────────────────
// WRITE endpoints – persist to PostgreSQL + publish Kafka event
// ────────────────────────────────────────────────────────

app.MapPost("/api/products", async (CreateProductRequest request, CatalogDbContext db, HttpContext httpContext) =>
{
    RequestValidation.Validate(request);
    RequestValidation.ValidateGuid(request.MerchantId, nameof(request.MerchantId));
    RequestValidation.ValidateRequiredString(request.Name, nameof(request.Name), 200);
    RequestValidation.ValidatePositive(request.Price, nameof(request.Price));

    var isAdmin = httpContext.User.HasClaim("role", "admin");
    if (!isAdmin)
    {
        var sub = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != request.MerchantId)
            return Results.Forbid();
    }

    var (categoryId, categoryName) = await ResolveCategoryAsync(db, request.CategoryId, request.Category);

    var product = new Product
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Description = request.Description,
        Price = request.Price,
        MerchantId = request.MerchantId,
        StockQuantity = request.StockQuantity,
        ImageUrl = request.ImageUrl,
        CategoryId = categoryId,
        Category = categoryName,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    var productEvent = new ProductEvent
    {
        ProductId = product.Id,
        EventType = ProductEventType.Created,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        MerchantId = product.MerchantId,
        StockQuantity = product.StockQuantity,
        ImageUrl = product.ImageUrl,
        Category = product.Category,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        OccurredAt = product.CreatedAt
    };

    db.Products.Add(product);
    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = Guid.NewGuid(),
        EventType = nameof(ProductEventType.Created),
        Payload = JsonSerializer.Serialize(productEvent),
        CreatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();

    return Results.Created($"/api/products/{product.Id}", product);
}).RequireAuthorization(AuthorizationPolicies.MerchantOrAdmin);

app.MapPut("/api/products/{id:guid}", async (Guid id, UpdateProductRequest request, CatalogDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    RequestValidation.Validate(request);
    RequestValidation.ValidateRequiredString(request.Name, nameof(request.Name), 200);
    RequestValidation.ValidatePositive(request.Price, nameof(request.Price));

    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    var isAdmin = httpContext.User.HasClaim("role", "admin");
    if (!isAdmin)
    {
        var sub = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != product.MerchantId)
            return Results.Forbid();
    }

    var (categoryId, categoryName) = await ResolveCategoryAsync(db, request.CategoryId, request.Category);

    product.Name = request.Name;
    product.Description = request.Description;
    product.Price = request.Price;
    product.StockQuantity = request.StockQuantity;
    product.ImageUrl = request.ImageUrl;
    product.CategoryId = categoryId;
    product.Category = categoryName;
    product.IsActive = request.IsActive;
    product.UpdatedAt = DateTime.UtcNow;

    var productEvent = new ProductEvent
    {
        ProductId = product.Id,
        EventType = ProductEventType.Updated,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        MerchantId = product.MerchantId,
        StockQuantity = product.StockQuantity,
        ImageUrl = product.ImageUrl,
        Category = product.Category,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        OccurredAt = product.UpdatedAt
    };

    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = Guid.NewGuid(),
        EventType = nameof(ProductEventType.Updated),
        Payload = JsonSerializer.Serialize(productEvent),
        CreatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();

    return Results.Ok(product);
}).RequireAuthorization(AuthorizationPolicies.MerchantOrAdmin);

app.MapDelete("/api/products/{id:guid}", async (Guid id, CatalogDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));

    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    var isAdmin = httpContext.User.HasClaim("role", "admin");
    if (!isAdmin)
    {
        var sub = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var callerMerchantId) || callerMerchantId != product.MerchantId)
            return Results.Forbid();
    }

    var productEvent = new ProductEvent
    {
        ProductId = product.Id,
        EventType = ProductEventType.Deleted,
        MerchantId = product.MerchantId,
        CreatedAt = product.CreatedAt,
        OccurredAt = DateTime.UtcNow
    };

    product.IsActive = false;
    product.UpdatedAt = DateTime.UtcNow;
    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = Guid.NewGuid(),
        EventType = nameof(ProductEventType.Deleted),
        Payload = JsonSerializer.Serialize(productEvent),
        CreatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.MerchantOrAdmin);

// ────────────────────────────────────────────────────────
// Category management endpoints
// ────────────────────────────────────────────────────────

app.MapGet("/api/categories", async (CatalogDbContext db, bool includeInactive = false) =>
{
    var query = db.Categories.AsQueryable();
    if (!includeInactive) query = query.Where(c => c.IsActive);
    var categories = await query.OrderBy(c => c.Name).ToListAsync();
    return Results.Ok(categories);
});

app.MapGet("/api/categories/{id:guid}", async (Guid id, CatalogDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    var category = await db.Categories.FindAsync(id);
    return category is not null ? Results.Ok(category) : Results.NotFound();
});

app.MapPost("/api/categories", async (CreateCategoryRequest request, CatalogDbContext db) =>
{
    RequestValidation.Validate(request);
    RequestValidation.ValidateRequiredString(request.Name, nameof(request.Name), 100);

    var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.Name) : Slugify(request.Slug);

    if (await db.Categories.AnyAsync(c => c.Name == request.Name))
        return Results.Conflict(new { error = $"Category with name '{request.Name}' already exists." });
    if (await db.Categories.AnyAsync(c => c.Slug == slug))
        return Results.Conflict(new { error = $"Category with slug '{slug}' already exists." });

    var category = new Category
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Slug = slug,
        Description = request.Description,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Categories.Add(category);
    await db.SaveChangesAsync();

    return Results.Created($"/api/categories/{category.Id}", category);
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.MapPut("/api/categories/{id:guid}", async (Guid id, UpdateCategoryRequest request, CatalogDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    RequestValidation.Validate(request);
    RequestValidation.ValidateRequiredString(request.Name, nameof(request.Name), 100);

    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();

    var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.Name) : Slugify(request.Slug);

    if (await db.Categories.AnyAsync(c => c.Id != id && c.Name == request.Name))
        return Results.Conflict(new { error = $"Category with name '{request.Name}' already exists." });
    if (await db.Categories.AnyAsync(c => c.Id != id && c.Slug == slug))
        return Results.Conflict(new { error = $"Category with slug '{slug}' already exists." });

    var oldName = category.Name;
    category.Name = request.Name;
    category.Slug = slug;
    category.Description = request.Description;
    category.IsActive = request.IsActive;
    category.UpdatedAt = DateTime.UtcNow;

    // Mirror the rename onto products that referenced this category by name.
    if (oldName != request.Name)
    {
        await db.Products
            .Where(p => p.CategoryId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Category, request.Name)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
    }

    await db.SaveChangesAsync();
    return Results.Ok(category);
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.MapDelete("/api/categories/{id:guid}", async (Guid id, CatalogDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();

    if (await db.Products.AnyAsync(p => p.CategoryId == id && p.IsActive))
        return Results.Conflict(new { error = "Cannot delete a category that still has active products. Deactivate it instead." });

    category.IsActive = false;
    category.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.Run();

// ────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────

static async Task<(Guid? Id, string? Name)> ResolveCategoryAsync(CatalogDbContext db, Guid? categoryId, string? legacyName)
{
    if (categoryId is { } id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null || !category.IsActive)
            throw new ArgumentException($"Category '{id}' does not exist or is inactive.", nameof(categoryId));
        return (category.Id, category.Name);
    }

    if (!string.IsNullOrWhiteSpace(legacyName))
    {
        var match = await db.Categories.FirstOrDefaultAsync(c => c.Name == legacyName);
        return (match?.Id, legacyName);
    }

    return (null, null);
}

static string Slugify(string value)
{
    var lower = value.Trim().ToLowerInvariant();
    var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
    var slug = new string(chars);
    while (slug.Contains("--")) slug = slug.Replace("--", "-");
    return slug.Trim('-');
}

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
