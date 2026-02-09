using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Order.Data;
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
    .AddDbContextCheck<OrderDbContext>(name: "orderdb", tags: new[] { "ready", "db" });

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
});

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
});

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
});

// Checkout
app.MapPost("/api/orders", async (UrbanX.Services.Order.Models.Order order, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(order.CustomerId, nameof(order.CustomerId));
    RequestValidation.ValidatePositive(order.TotalAmount, nameof(order.TotalAmount));
    
    if (order.Items == null || !order.Items.Any())
    {
        return Results.BadRequest("Order must contain at least one item");
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
    
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/orders/{order.Id}", order);
});

// Order tracking
app.MapGet("/api/orders/{orderId:guid}", async (Guid orderId, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(orderId, nameof(orderId));
    
    var order = await db.Orders
        .Include(o => o.Items)
        .Include(o => o.StatusHistory)
        .FirstOrDefaultAsync(o => o.Id == orderId);
    
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

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
});

app.MapPut("/api/orders/{orderId:guid}/status", async (Guid orderId, OrderStatus status, OrderDbContext db) =>
{
    RequestValidation.ValidateGuid(orderId, nameof(orderId));
    
    var order = await db.Orders
        .Include(o => o.StatusHistory)
        .FirstOrDefaultAsync(o => o.Id == orderId);
    
    if (order == null) return Results.NotFound();
    
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
});

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
