using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Inventory.Data;
using UrbanX.Services.Inventory.Models;

namespace UrbanX.Services.Inventory.IntegrationTests;

public class InventoryServiceIntegrationTests
{
    private static DbContextOptions<InventoryDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: "InventoryIntegrationTest_" + Guid.NewGuid())
            .Options;

    [Fact]
    public async Task InventoryItem_ShouldBeSavedAndRetrievedByProductId()
    {
        // Arrange
        var options = CreateOptions();
        using var context = new InventoryDbContext(options);

        var productId = Guid.NewGuid();
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            QuantityAvailable = 100,
            QuantityReserved = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        Assert.NotNull(saved);
        Assert.Equal(100, saved!.QuantityAvailable);
        Assert.Equal(0, saved.QuantityReserved);
    }

    [Fact]
    public async Task InventoryReservation_ShouldReduceAvailableStock()
    {
        // Arrange
        var options = CreateOptions();
        using var context = new InventoryDbContext(options);

        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            QuantityAvailable = 50,
            QuantityReserved = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        // Act - simulate reservation
        item.QuantityReserved += 10;
        item.UpdatedAt = DateTime.UtcNow;

        context.InventoryReservations.Add(new InventoryReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = 10,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert
        var updatedItem = await context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
        Assert.NotNull(updatedItem);
        Assert.Equal(50, updatedItem!.QuantityAvailable);
        Assert.Equal(10, updatedItem.QuantityReserved);

        var freeStock = updatedItem.QuantityAvailable - updatedItem.QuantityReserved;
        Assert.Equal(40, freeStock);

        var reservations = await context.InventoryReservations
            .Where(r => r.OrderId == orderId)
            .ToListAsync();
        Assert.Single(reservations);
        Assert.Equal(ReservationStatus.Reserved, reservations.First().Status);
    }

    [Fact]
    public async Task OutboxMessage_ShouldBeSavedWithInventoryReservation()
    {
        // Arrange
        var options = CreateOptions();
        using var context = new InventoryDbContext(options);

        // Act
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "Reserved",
            Payload = "{\"orderId\": \"test\"}",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert
        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .ToListAsync();
        Assert.Single(pending);
        Assert.Equal("Reserved", pending.First().EventType);
        Assert.Equal(0, pending.First().RetryCount);
    }

    [Fact]
    public async Task InventoryReservations_ShouldBeQueryableByOrderId()
    {
        // Arrange
        var options = CreateOptions();
        using var context = new InventoryDbContext(options);

        var orderId = Guid.NewGuid();
        var reservations = new[]
        {
            new InventoryReservation
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = Guid.NewGuid(),
                Quantity = 2,
                Status = ReservationStatus.Reserved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new InventoryReservation
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = Guid.NewGuid(),
                Quantity = 3,
                Status = ReservationStatus.Reserved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.InventoryReservations.AddRange(reservations);
        await context.SaveChangesAsync();

        // Act
        var orderReservations = await context.InventoryReservations
            .Where(r => r.OrderId == orderId)
            .ToListAsync();

        // Assert
        Assert.Equal(2, orderReservations.Count);
    }
}
