using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Order.Data;
using UrbanX.Services.Order.Models;

namespace UrbanX.Services.Order.UnitTests;

public class OrderDbContextTests
{
    [Fact]
    public void OrderDbContext_ShouldConfigureEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderTestDb_" + Guid.NewGuid())
            .Options;

        // Act
        using var context = new OrderDbContext(options);
        var cartEntityType = context.Model.FindEntityType(typeof(Cart));
        var orderEntityType = context.Model.FindEntityType(typeof(Models.Order));

        // Assert
        cartEntityType.Should().NotBeNull();
        orderEntityType.Should().NotBeNull();
        cartEntityType!.FindPrimaryKey()!.Properties.Should().Contain(p => p.Name == "Id");
        orderEntityType!.FindPrimaryKey()!.Properties.Should().Contain(p => p.Name == "Id");
    }

    [Fact]
    public async Task OrderDbContext_ShouldAddAndRetrieveCart()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderTestDb_" + Guid.NewGuid())
            .Options;

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new OrderDbContext(options))
        {
            context.Carts.Add(cart);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new OrderDbContext(options))
        {
            var savedCart = await context.Carts.FindAsync(cart.Id);
            savedCart.Should().NotBeNull();
            savedCart!.CustomerId.Should().Be(cart.CustomerId);
        }
    }

    [Fact]
    public async Task OrderDbContext_ShouldAddCartWithItems()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderTestDb_" + Guid.NewGuid())
            .Options;

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
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
        using (var context = new OrderDbContext(options))
        {
            context.Carts.Add(cart);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new OrderDbContext(options))
        {
            var savedCart = await context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cart.Id);
            savedCart.Should().NotBeNull();
            savedCart!.Items.Should().HaveCount(1);
            savedCart.Items.First().ProductName.Should().Be("Test Product");
        }
    }

    [Fact]
    public async Task OrderDbContext_ShouldAddAndRetrieveOrder()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderTestDb_" + Guid.NewGuid())
            .Options;

        var order = new Models.Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            ShippingAddress = "123 Test St",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new OrderDbContext(options))
        {
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new OrderDbContext(options))
        {
            var savedOrder = await context.Orders.FindAsync(order.Id);
            savedOrder.Should().NotBeNull();
            savedOrder!.OrderNumber.Should().Be("ORD-001");
            savedOrder.Status.Should().Be(OrderStatus.Pending);
        }
    }

    [Fact]
    public async Task OrderDbContext_ShouldAddOrderWithItemsAndHistory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderTestDb_" + Guid.NewGuid())
            .Options;

        var order = new Models.Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            Status = OrderStatus.Pending,
            TotalAmount = 150.00m,
            ShippingAddress = "456 Test Ave",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            ProductName = "Product A",
            Quantity = 3,
            UnitPrice = 50.00m,
            MerchantId = Guid.NewGuid()
        };

        var statusHistory = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order.Items.Add(orderItem);
        order.StatusHistory.Add(statusHistory);

        // Act
        using (var context = new OrderDbContext(options))
        {
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new OrderDbContext(options))
        {
            var savedOrder = await context.Orders
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == order.Id);
            savedOrder.Should().NotBeNull();
            savedOrder!.Items.Should().HaveCount(1);
            savedOrder.Items.First().ProductName.Should().Be("Product A");
            savedOrder.StatusHistory.Should().HaveCount(1);
            savedOrder.StatusHistory.First().Status.Should().Be(OrderStatus.Pending);
        }
    }

    [Fact]
    public async Task OrderDbContext_ShouldCascadeDeleteCartItems()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderTestDb_" + Guid.NewGuid())
            .Options;

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 1,
            UnitPrice = 10.00m,
            MerchantId = Guid.NewGuid()
        };

        cart.Items.Add(cartItem);

        using (var context = new OrderDbContext(options))
        {
            context.Carts.Add(cart);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new OrderDbContext(options))
        {
            var savedCart = await context.Carts.Include(c => c.Items).FirstAsync(c => c.Id == cart.Id);
            context.Carts.Remove(savedCart);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new OrderDbContext(options))
        {
            var deletedCart = await context.Carts.FindAsync(cart.Id);
            var deletedItem = await context.CartItems.FindAsync(cartItem.Id);
            deletedCart.Should().BeNull();
            deletedItem.Should().BeNull();
        }
    }
}
