using UrbanX.Services.Inventory.Models;

namespace UrbanX.Services.Inventory.UnitTests;

public class InventoryModelsTests
{
    [Fact]
    public void InventoryItem_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            QuantityAvailable = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(0, item.QuantityReserved);
        Assert.Equal(100, item.QuantityAvailable);
    }

    [Fact]
    public void InventoryItem_AvailableStockIsQuantityAvailableMinusReserved()
    {
        // Arrange
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            QuantityAvailable = 50,
            QuantityReserved = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var freeStock = item.QuantityAvailable - item.QuantityReserved;

        // Assert
        Assert.Equal(40, freeStock);
    }

    [Fact]
    public void InventoryReservation_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var reservation = new InventoryReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = 5,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(orderId, reservation.OrderId);
        Assert.Equal(productId, reservation.ProductId);
        Assert.Equal(5, reservation.Quantity);
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
    }

    [Theory]
    [InlineData(ReservationStatus.Reserved)]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Released)]
    public void ReservationStatus_ShouldHaveAllDefinedValues(ReservationStatus status)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ReservationStatus), status));
    }

    [Fact]
    public void OutboxMessage_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "Reserved",
            Payload = "{\"orderId\": \"test\"}",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("Reserved", message.EventType);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.ProcessedAt);
    }
}
