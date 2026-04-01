using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Inventory.Data;
using UrbanX.Services.Inventory.Messaging;
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

    [Fact]
    public async Task SagaCompensation_ReleasedReservations_ShouldRestoreQuantityReserved()
    {
        // When an order is cancelled after inventory has been reserved,
        // the KafkaOrderCancelledConsumer must release each reservation and
        // decrement QuantityReserved on the InventoryItem.
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: "InventoryTest_" + Guid.NewGuid())
            .Options;

        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        using (var context = new InventoryDbContext(options))
        {
            context.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                QuantityAvailable = 10,
                QuantityReserved = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.InventoryReservations.Add(new InventoryReservation
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = productId,
                Quantity = 3,
                Status = ReservationStatus.Reserved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Simulate the compensation logic (mirroring KafkaOrderCancelledConsumer)
        using (var context = new InventoryDbContext(options))
        {
            var reservations = await context.InventoryReservations
                .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved)
                .ToListAsync();

            var productIds = reservations.Select(r => r.ProductId).Distinct().ToList();
            var inventoryItems = await context.InventoryItems
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                reservation.Status = ReservationStatus.Released;
                reservation.UpdatedAt = DateTime.UtcNow;

                var inventoryItem = inventoryItems.FirstOrDefault(i => i.ProductId == reservation.ProductId);
                if (inventoryItem != null)
                    inventoryItem.QuantityReserved = Math.Max(0, inventoryItem.QuantityReserved - reservation.Quantity);
            }

            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = InventoryEventType.Released.ToString(),
                Payload = System.Text.Json.JsonSerializer.Serialize(new InventoryEvent
                {
                    OrderId = orderId,
                    EventType = InventoryEventType.Released,
                    Items = reservations.Select(r => new InventoryEventItem { ProductId = r.ProductId, Quantity = r.Quantity }).ToList(),
                    OccurredAt = DateTime.UtcNow
                }),
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

        // Assert – reservations are Released and QuantityReserved is back to 0
        using (var context = new InventoryDbContext(options))
        {
            var item = await context.InventoryItems.FirstAsync(i => i.ProductId == productId);
            Assert.Equal(0, item.QuantityReserved);

            var reservation = await context.InventoryReservations
                .FirstAsync(r => r.OrderId == orderId);
            Assert.Equal(ReservationStatus.Released, reservation.Status);

            var outbox = await context.OutboxMessages.ToListAsync();
            Assert.Single(outbox, m => m.EventType == InventoryEventType.Released.ToString());
        }
    }

    [Fact]
    public async Task SagaCompensation_AlreadyReleasedReservations_ShouldBeIdempotent()
    {
        // If the cancellation event is processed twice, no additional changes should occur.
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: "InventoryTest_" + Guid.NewGuid())
            .Options;

        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        using (var context = new InventoryDbContext(options))
        {
            context.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                QuantityAvailable = 10,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.InventoryReservations.Add(new InventoryReservation
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = productId,
                Quantity = 3,
                Status = ReservationStatus.Released, // already released
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Simulate compensation logic finding no Reserved reservations
        using (var context = new InventoryDbContext(options))
        {
            var reservations = await context.InventoryReservations
                .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved)
                .ToListAsync();

            // Nothing to release
            Assert.Empty(reservations);
        }
    }
}
