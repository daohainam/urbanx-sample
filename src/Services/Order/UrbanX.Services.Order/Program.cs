using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Order.Data;
using UrbanX.Services.Order.Messaging;
using UrbanX.Services.Order.Models;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<OrderDbContext>("orderdb");

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>(name: "orderdb", tags: ["ready", "db"]);

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
builder.Services.AddAuthorization();

// Configure Kafka publisher for order events (Saga)
builder.Services.AddSingleton<IOrderEventPublisher, KafkaOrderEventPublisher>();

// Configure outbox relay service (publishes order events to Kafka)
builder.Services.AddHostedService<OrderOutboxRelayService>();

// Configure Kafka consumer for inventory responses (Saga coordinator)
builder.Services.AddHostedService<KafkaInventoryResponseConsumer>();

// Configure Kafka consumer for payment responses (Saga coordinator)
builder.Services.AddHostedService<KafkaPaymentResponseConsumer>();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations for OrderDbContext...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

// Cart endpoints
app.MapGet("/api/cart/{customerId:guid}", async (Guid customerId, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(customerId, nameof(customerId));
    
    var cart = await db.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    
    if (cart == null)
    {
        cart = new Cart { Id = Guid.NewGuid(), CustomerId = customerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
    }
    
    return Results.Ok(cart);
}).RequireAuthorization();

app.MapPost("/api/cart/{customerId:guid}/items", async (Guid customerId, CartItem item, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(customerId, nameof(customerId));
    RequestValidation.ValidateGuid(item.ProductId, nameof(item.ProductId));
    RequestValidation.ValidatePositive(item.Quantity, nameof(item.Quantity));
    RequestValidation.ValidatePositive(item.UnitPrice, nameof(item.UnitPrice));
    RequestValidation.ValidateRequiredString(item.ProductName, nameof(item.ProductName), 200);
    
    var cart = await db.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    
    if (cart == null)
    {
        cart = new Cart { Id = Guid.NewGuid(), CustomerId = customerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Carts.Add(cart);
    }
    
    var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
    if (existingItem != null)
    {
        existingItem.Quantity += item.Quantity;
    }
    else
    {
        item.Id = Guid.NewGuid();
        item.CartId = cart.Id;
        cart.Items.Add(item);
    }
    
    cart.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(cart);
}).RequireAuthorization();

app.MapDelete("/api/cart/{customerId:guid}/items/{itemId:guid}", async (Guid customerId, Guid itemId, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(customerId, nameof(customerId));
    RequestValidation.ValidateGuid(itemId, nameof(itemId));
    
    var cart = await db.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    
    if (cart == null) return Results.NotFound();
    
    var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
    if (item != null)
    {
        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
    
    return Results.Ok(cart);
}).RequireAuthorization();

// Checkout
app.MapPost("/api/orders", async (UrbanX.Services.Order.Models.Order order, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(order.CustomerId, nameof(order.CustomerId));
    RequestValidation.ValidateRequiredString(order.ShippingAddress, nameof(order.ShippingAddress), 500);
    
    if (order.Items == null || !order.Items.Any())
    {
        return Results.BadRequest("Order must contain at least one item");
    }

    foreach (var item in order.Items)
    {
        RequestValidation.ValidateGuid(item.ProductId, nameof(item.ProductId));
        RequestValidation.ValidateRequiredString(item.ProductName, nameof(item.ProductName), 200);
        RequestValidation.ValidatePositive(item.Quantity, nameof(item.Quantity));
        RequestValidation.ValidatePositive(item.UnitPrice, nameof(item.UnitPrice));
    }

    var expectedTotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
    if (Math.Abs(order.TotalAmount - expectedTotal) > 0.01m)
    {
        return Results.BadRequest("TotalAmount does not match the sum of item quantities and unit prices.");
    }
    
    order.Id = Guid.NewGuid();
    order.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
    order.Status = OrderStatus.Pending;
    order.CreatedAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;
    
    order.StatusHistory.Add(new OrderStatusHistory
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.UtcNow
    });
    
    // Publish OrderCreated event via transactional outbox for inventory Saga
    var orderCreatedEvent = new OrderCreatedEvent
    {
        OrderId = order.Id,
        Items = order.Items.Select(i => new OrderCreatedEventItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList(),
        OccurredAt = order.CreatedAt
    };

    db.Orders.Add(order);
    db.OutboxMessages.Add(new OutboxMessage
    {
        Id = Guid.NewGuid(),
        EventType = nameof(OrderCreatedEvent),
        Payload = JsonSerializer.Serialize(orderCreatedEvent),
        CreatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/orders/{order.Id}", order);
}).RequireAuthorization();

// Order tracking
app.MapGet("/api/orders/{orderId:guid}", async (Guid orderId, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(orderId, nameof(orderId));
    
    var order = await db.Orders
        .Include(o => o.Items)
        .Include(o => o.StatusHistory)
        .FirstOrDefaultAsync(o => o.Id == orderId);
    
    return order is not null ? Results.Ok(order) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/orders/customer/{customerId:guid}", async (Guid customerId, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(customerId, nameof(customerId));
    
    var orders = await db.Orders
        .Where(o => o.CustomerId == customerId)
        .Include(o => o.Items)
        .Include(o => o.StatusHistory)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
    
    return Results.Ok(orders);
}).RequireAuthorization();

app.MapPut("/api/orders/{orderId:guid}/status", async (Guid orderId, OrderStatus status, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(orderId, nameof(orderId));
    
    var order = await db.Orders
        .Include(o => o.StatusHistory)
        .FirstOrDefaultAsync(o => o.Id == orderId);
    
    if (order == null) return Results.NotFound();

    if (!OrderStatusTransitions.IsAllowed(order.Status, status))
    {
        return Results.UnprocessableEntity(new { error = $"Cannot transition order from '{order.Status}' to '{status}'." });
    }

    order.Status = status;
    order.UpdatedAt = DateTime.UtcNow;
    order.StatusHistory.Add(new OrderStatusHistory
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        Status = status,
        CreatedAt = DateTime.UtcNow
    });
    
    await db.SaveChangesAsync();
    return Results.Ok(order);
}).RequireAuthorization();

// Merchant accept checkout after payment
app.MapPost("/api/orders/{orderId:guid}/accept", async (Guid orderId, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(orderId, nameof(orderId));

    var order = await db.Orders
        .Include(o => o.StatusHistory)
        .FirstOrDefaultAsync(o => o.Id == orderId);

    if (order == null) return Results.NotFound();

    if (order.Status != OrderStatus.PaymentReceived)
        return Results.BadRequest($"Order cannot be accepted in its current status ({order.Status}). Order must be in PaymentReceived status.");

    order.Status = OrderStatus.Confirmed;
    order.UpdatedAt = DateTime.UtcNow;
    order.StatusHistory.Add(new OrderStatusHistory
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        Status = OrderStatus.Confirmed,
        Note = "Order accepted by merchant",
        CreatedAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
    return Results.Ok(order);
}).RequireAuthorization();

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
