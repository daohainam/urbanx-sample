using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Order.Data;
using UrbanX.Services.Order.Models;

namespace UrbanX.Services.Order.IntegrationTests;

public class OrderServiceIntegrationTests
{
    [Fact]
    public async Task OrderService_ShouldCreateAndManageCart()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new OrderDbContext(options);
        var customerId = Guid.NewGuid();
        
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 2,
            UnitPrice = 25.00m,
            MerchantId = Guid.NewGuid()
        };

        cart.Items.Add(cartItem);

        // Act
        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        // Assert
        var savedCart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        Assert.NotNull(savedCart);
        Assert.Equal(1, savedCart!.Items.Count);
        Assert.Equal(2, savedCart.Items.First().Quantity);
    }

    [Fact]
    public async Task OrderService_ShouldCreateOrderWithItems()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new OrderDbContext(options);
        
        var order = new Models.Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}",
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            ShippingAddress = "123 Test St",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 2,
            UnitPrice = 50.00m,
            MerchantId = Guid.NewGuid()
        };

        order.Items.Add(orderItem);

        // Act
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Assert
        var savedOrder = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        Assert.NotNull(savedOrder);
        Assert.Equal(1, savedOrder!.Items.Count);
        Assert.Equal(100.00m, savedOrder.TotalAmount);
    }

    [Fact]
    public async Task OrderService_ShouldGetOrdersByCustomer()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new OrderDbContext(options);
        var customerId = Guid.NewGuid();
        
        var orders = new List<Models.Order>
        {
            new Models.Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                OrderNumber = "ORD-001",
                Status = OrderStatus.Delivered,
                TotalAmount = 100.00m,
                ShippingAddress = "123 Test St",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Models.Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                OrderNumber = "ORD-002",
                Status = OrderStatus.Pending,
                TotalAmount = 50.00m,
                ShippingAddress = "123 Test St",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        // Act
        var customerOrders = await context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Assert
        Assert.Equal(2, customerOrders.Count);
        Assert.Equal("ORD-002", customerOrders.First().OrderNumber);
    }
}
