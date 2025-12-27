using UrbanX.Services.Order.Models;

namespace UrbanX.Services.Order.UnitTests;

public class OrderModelsTests
{
    [Fact]
    public void Cart_ShouldInitializeWithEmptyItems()
    {
        // Arrange & Act
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        cart.Assert.NotNull(Items);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void CartItem_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 2,
            UnitPrice = 25.00m,
            MerchantId = Guid.NewGuid()
        };

        // Assert
        Assert.Equal("Test Product", cartItem.ProductName);
        Assert.Equal(2, cartItem.Quantity);
        Assert.Equal(25.00m, cartItem.UnitPrice);
    }

    [Fact]
    public void Order_ShouldInitializeWithEmptyCollections()
    {
        // Arrange & Act
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

        // Assert
        order.Assert.NotNull(Items);
        Assert.Empty(order.Items);
        order.Assert.NotNull(StatusHistory);
        Assert.Empty(order.StatusHistory);
    }

    [Fact]
    public void OrderItem_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 3,
            UnitPrice = 50.00m,
            MerchantId = Guid.NewGuid()
        };

        // Assert
        Assert.Equal("Test Product", orderItem.ProductName);
        Assert.Equal(3, orderItem.Quantity);
        Assert.Equal(50.00m, orderItem.UnitPrice);
    }

    [Fact]
    public void OrderStatusHistory_ShouldAllowNullNote()
    {
        // Arrange & Act
        var statusHistory = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Status = OrderStatus.Confirmed,
            Note = null,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(OrderStatus.Confirmed, statusHistory.Status);
        Assert.Null(statusHistory.Note);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.PaymentReceived)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.ReadyForPickup)]
    [InlineData(OrderStatus.InTransit)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void OrderStatus_ShouldHaveAllDefinedValues(OrderStatus status)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(OrderStatus), status));
    }

    [Fact]
    public void Order_ShouldAllowAddingItems()
    {
        // Arrange
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

        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 1,
            UnitPrice = 100.00m,
            MerchantId = Guid.NewGuid()
        };

        // Act
        order.Items.Add(item);

        // Assert
        Assert.Equal(1, order.Items.Count);
        Assert.Equal(item, order.Items.First());
    }

    [Fact]
    public void Order_ShouldAllowAddingStatusHistory()
    {
        // Arrange
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

        var statusHistory = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        order.StatusHistory.Add(statusHistory);

        // Assert
        Assert.Equal(1, order.StatusHistory.Count);
        Assert.Equal(OrderStatus.Confirmed, order.StatusHistory.First().Status);
    }
}
