using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Inventory.Data;
using UrbanX.Services.Inventory.Messaging;
using UrbanX.Services.Inventory.Models;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<InventoryDbContext>("inventorydb");

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>(name: "inventorydb", tags: ["ready", "db"]);

// Configure Kafka publisher
builder.Services.AddSingleton<IInventoryEventPublisher, KafkaInventoryEventPublisher>();

// Configure Kafka consumer background service (consumes order.created events)
builder.Services.AddHostedService<KafkaOrderEventConsumer>();

// Configure Kafka consumer for order cancellation (Saga compensation: releases reserved inventory)
builder.Services.AddHostedService<KafkaOrderCancelledConsumer>();

// Configure outbox relay service (publishes pending outbox messages to Kafka)
builder.Services.AddHostedService<OutboxRelayService>();

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
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations for InventoryDbContext...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

// Inventory management endpoints

app.MapGet("/api/inventory/{productId:guid}", async (Guid productId, InventoryDbContext db) =>
{
    RequestValidation.ValidateGuid(productId, nameof(productId));

    var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
    return item is not null ? Results.Ok(item) : Results.NotFound();
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.MapPost("/api/inventory", async (InventoryItem item, InventoryDbContext db) =>
{
    RequestValidation.ValidateGuid(item.ProductId, nameof(item.ProductId));

    var existing = await db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
    if (existing != null)
    {
        return Results.Conflict($"Inventory item for product {item.ProductId} already exists");
    }

    item.Id = Guid.NewGuid();
    item.QuantityReserved = 0;
    item.CreatedAt = DateTime.UtcNow;
    item.UpdatedAt = DateTime.UtcNow;

    db.InventoryItems.Add(item);
    await db.SaveChangesAsync();

    return Results.Created($"/api/inventory/{item.ProductId}", item);
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.MapPut("/api/inventory/{productId:guid}", async (Guid productId, InventoryItem update, InventoryDbContext db) =>
{
    RequestValidation.ValidateGuid(productId, nameof(productId));

    var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
    if (item == null) return Results.NotFound();

    item.QuantityAvailable = update.QuantityAvailable;
    item.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(item);
}).RequireAuthorization(AuthorizationPolicies.MerchantOnly);

app.MapGet("/api/inventory/reservations/{orderId:guid}", async (Guid orderId, InventoryDbContext db) =>
{
    RequestValidation.ValidateGuid(orderId, nameof(orderId));

    var reservations = await db.InventoryReservations
        .AsNoTracking()
        .Where(r => r.OrderId == orderId)
        .ToListAsync();

    return Results.Ok(reservations);
}).RequireAuthorization(AuthorizationPolicies.CustomerOrMerchant);

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
