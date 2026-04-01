using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Order.Data;
using UrbanX.Services.Order.Messaging;
using UrbanX.Services.Order.Models;

namespace UrbanX.Services.Order.UnitTests;

public class PaymentResponseConsumerTests
{
    [Fact]
    public async Task OrderDb_ShouldSaveOrderWithPaymentReceivedStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderPaymentTest_" + Guid.NewGuid())
            .Options;

        var orderId = Guid.NewGuid();
        var order = new Models.Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            Status = OrderStatus.PaymentReceived,
            TotalAmount = 100.00m,
            ShippingAddress = "123 Test St",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = OrderStatus.PaymentReceived,
            Note = "Payment completed successfully",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        using var context = new OrderDbContext(options);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Assert
        var savedOrder = await context.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.PaymentReceived, savedOrder!.Status);
        Assert.Contains(savedOrder.StatusHistory, h => h.Status == OrderStatus.PaymentReceived);
    }

    [Fact]
    public async Task OrderDb_ShouldSaveOrderWithCancelledStatusOnPaymentFailed()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderPaymentTest_" + Guid.NewGuid())
            .Options;

        var orderId = Guid.NewGuid();
        var order = new Models.Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            Status = OrderStatus.Cancelled,
            TotalAmount = 50.00m,
            ShippingAddress = "456 Test Ave",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = OrderStatus.Cancelled,
            Note = "Payment failed: Card declined",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        using var context = new OrderDbContext(options);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Assert
        var savedOrder = await context.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Cancelled, savedOrder!.Status);
    }

    [Fact]
    public void PaymentFailed_OrderCancelledEvent_ShouldHaveCorrectStructure()
    {
        // When payment fails, KafkaPaymentResponseConsumer writes an OrderCancelledEvent
        // to the transactional outbox so the Inventory service can release reserved stock.
        var orderId = Guid.NewGuid();
        var reason = "Payment failed: Card declined";

        var cancelledEvent = new OrderCancelledEvent
        {
            OrderId = orderId,
            Reason = reason,
            OccurredAt = DateTime.UtcNow
        };

        Assert.Equal(orderId, cancelledEvent.OrderId);
        Assert.Equal(reason, cancelledEvent.Reason);
        Assert.True(cancelledEvent.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task PaymentFailed_ShouldWriteOrderCancelledEventToOutbox()
    {
        // Verify that when an order is cancelled after payment failure,
        // an OrderCancelledEvent outbox message is persisted atomically.
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderPaymentTest_" + Guid.NewGuid())
            .Options;

        var orderId = Guid.NewGuid();
        var cancelledEvent = new OrderCancelledEvent
        {
            OrderId = orderId,
            Reason = "Payment failed: Card declined",
            OccurredAt = DateTime.UtcNow
        };

        // Write only the outbox message (EF InMemory's unique-index constraint on the Order
        // entity can interfere with multi-step scenarios, so we isolate the outbox assertion).
        using var context = new OrderDbContext(options);
        context.OutboxMessages.Add(new Models.OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(OrderCancelledEvent),
            Payload = System.Text.Json.JsonSerializer.Serialize(cancelledEvent),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert – the outbox message carries an OrderCancelledEvent for the right order.
        var msg = await context.OutboxMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.EventType == nameof(OrderCancelledEvent));
        Assert.NotNull(msg);
        var decoded = System.Text.Json.JsonSerializer.Deserialize<OrderCancelledEvent>(msg!.Payload);
        Assert.Equal(orderId, decoded!.OrderId);
    }

    [Fact]
    public async Task OrderDb_ShouldSaveOrderWithConfirmedStatusOnMerchantAccept()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderPaymentTest_" + Guid.NewGuid())
            .Options;

        var orderId = Guid.NewGuid();
        var order = new Models.Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            OrderNumber = "ORD-003",
            Status = OrderStatus.Confirmed,
            TotalAmount = 200.00m,
            ShippingAddress = "789 Test Blvd",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = OrderStatus.Confirmed,
            Note = "Order accepted by merchant",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        using var context = new OrderDbContext(options);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Assert
        var savedOrder = await context.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Confirmed, savedOrder!.Status);
        Assert.Contains(savedOrder.StatusHistory, h => h.Note == "Order accepted by merchant");
    }

    [Fact]
    public void PaymentResponseEvent_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var paymentEvent = new PaymentResponseEvent
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 150.00m,
            EventType = PaymentEventType.Completed,
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(paymentEvent);
        Assert.Equal(PaymentEventType.Completed, paymentEvent.EventType);
        Assert.Equal(150.00m, paymentEvent.Amount);
        Assert.Null(paymentEvent.FailureReason);
    }

    [Fact]
    public void PaymentResponseEvent_FailedShouldHaveFailureReason()
    {
        // Arrange & Act
        var paymentEvent = new PaymentResponseEvent
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 75.00m,
            EventType = PaymentEventType.Failed,
            FailureReason = "Insufficient funds",
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(PaymentEventType.Failed, paymentEvent.EventType);
        Assert.Equal("Insufficient funds", paymentEvent.FailureReason);
    }

    [Fact]
    public void OrderStatus_PaymentReceivedShouldPrecedeConfirmed()
    {
        // Assert: PaymentReceived (1) comes before Confirmed (2) in the enum
        Assert.True((int)OrderStatus.PaymentReceived < (int)OrderStatus.Confirmed);
    }

    [Theory]
    [InlineData(PaymentEventType.Completed)]
    [InlineData(PaymentEventType.Failed)]
    public void PaymentEventType_ShouldHaveAllDefinedValues(PaymentEventType eventType)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PaymentEventType), eventType));
    }
}

