using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Messaging;
using UrbanX.Services.Payment.Models;

namespace UrbanX.Services.Payment.UnitTests;

public class PaymentOutboxTests
{
    [Fact]
    public async Task PaymentDbContext_ShouldAddAndRetrieveOutboxMessage()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentOutboxTest_" + Guid.NewGuid())
            .Options;

        var paymentEvent = new PaymentEvent
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            EventType = PaymentEventType.Completed,
            OccurredAt = DateTime.UtcNow
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(PaymentEventType.Completed),
            Payload = JsonSerializer.Serialize(paymentEvent),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new PaymentDbContext(options))
        {
            context.OutboxMessages.Add(outboxMessage);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new PaymentDbContext(options))
        {
            var savedMessage = await context.OutboxMessages.FindAsync(outboxMessage.Id);
            Assert.NotNull(savedMessage);
            Assert.Equal(nameof(PaymentEventType.Completed), savedMessage!.EventType);
            Assert.Null(savedMessage.ProcessedAt);
            Assert.Equal(0, savedMessage.RetryCount);
        }
    }

    [Fact]
    public async Task PaymentDbContext_ShouldSavePaymentAndOutboxMessageAtomically()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentOutboxTest_" + Guid.NewGuid())
            .Options;

        var orderId = Guid.NewGuid();
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 75.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.CreditCard,
            TransactionId = "TXN-ABC123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var paymentEvent = new PaymentEvent
        {
            PaymentId = payment.Id,
            OrderId = orderId,
            Amount = payment.Amount,
            EventType = PaymentEventType.Completed,
            OccurredAt = payment.UpdatedAt
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = PaymentEventType.Completed.ToString(),
            Payload = JsonSerializer.Serialize(paymentEvent),
            CreatedAt = DateTime.UtcNow
        };

        // Act - save both atomically
        using (var context = new PaymentDbContext(options))
        {
            context.Payments.Add(payment);
            context.OutboxMessages.Add(outboxMessage);
            await context.SaveChangesAsync();
        }

        // Assert - both should exist
        using (var context = new PaymentDbContext(options))
        {
            var savedPayment = await context.Payments.FindAsync(payment.Id);
            var savedOutbox = await context.OutboxMessages.FindAsync(outboxMessage.Id);

            Assert.NotNull(savedPayment);
            Assert.NotNull(savedOutbox);
            Assert.Equal(orderId, savedPayment!.OrderId);
            Assert.Equal(PaymentStatus.Completed, savedPayment.Status);
            Assert.Null(savedOutbox!.ProcessedAt);
        }
    }

    [Fact]
    public async Task PaymentDbContext_ShouldQueryPendingOutboxMessages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentOutboxTest_" + Guid.NewGuid())
            .Options;

        var paymentEvent = new PaymentEvent
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 50.00m,
            EventType = PaymentEventType.Completed,
            OccurredAt = DateTime.UtcNow
        };

        var pendingMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = PaymentEventType.Completed.ToString(),
            Payload = JsonSerializer.Serialize(paymentEvent),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        var processedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = PaymentEventType.Failed.ToString(),
            Payload = JsonSerializer.Serialize(paymentEvent),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };

        using (var context = new PaymentDbContext(options))
        {
            context.OutboxMessages.AddRange(pendingMessage, processedMessage);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new PaymentDbContext(options))
        {
            var pending = await context.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .ToListAsync();

            // Assert
            Assert.Single(pending);
            Assert.Equal(pendingMessage.Id, pending.First().Id);
        }
    }
}
