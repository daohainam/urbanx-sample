using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Order.Data;
using UrbanX.Services.Order.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<OrderDbContext>("orderdb");

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}

// Cart endpoints
app.MapGet("/api/cart/{customerId:guid}", async (Guid customerId, OrderDbContext db) =>
{
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
    var order = await db.Orders
        .Include(o => o.Items)
        .Include(o => o.StatusHistory)
        .FirstOrDefaultAsync(o => o.Id == orderId);
    
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.MapGet("/api/orders/customer/{customerId:guid}", async (Guid customerId, OrderDbContext db) =>
{
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
