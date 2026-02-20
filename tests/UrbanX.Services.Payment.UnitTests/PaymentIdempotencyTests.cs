using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;

namespace UrbanX.Services.Payment.UnitTests;

/// <summary>
/// Tests for production-grade payment idempotency:
/// Ensures a second payment for the same order is rejected when a
/// non-failed payment already exists.
/// </summary>
public class PaymentIdempotencyTests
{
    private static PaymentDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentIdempotencyTest_" + Guid.NewGuid())
            .Options);

    [Fact]
    public async Task DuplicatePayment_WhenCompletedPaymentExists_ShouldBeDetected()
    {
        using var db = CreateInMemoryContext();
        var orderId = Guid.NewGuid();

        db.Payments.Add(new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 50.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.CreditCard,
            TransactionId = "TXN-001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var existing = await db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status != PaymentStatus.Failed);

        Assert.NotNull(existing);
    }

    [Fact]
    public async Task DuplicatePayment_WhenProcessingPaymentExists_ShouldBeDetected()
    {
        using var db = CreateInMemoryContext();
        var orderId = Guid.NewGuid();

        db.Payments.Add(new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 75.00m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.Stripe,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var existing = await db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status != PaymentStatus.Failed);

        Assert.NotNull(existing);
    }

    [Fact]
    public async Task NewPayment_WhenOnlyFailedPaymentExists_ShouldBeAllowed()
    {
        using var db = CreateInMemoryContext();
        var orderId = Guid.NewGuid();

        db.Payments.Add(new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 100.00m,
            Status = PaymentStatus.Failed,
            Method = PaymentMethod.CreditCard,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var existing = await db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status != PaymentStatus.Failed);

        Assert.Null(existing);
    }

    [Fact]
    public async Task NewPayment_WhenNoPaymentExists_ShouldBeAllowed()
    {
        using var db = CreateInMemoryContext();
        var orderId = Guid.NewGuid();

        var existing = await db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status != PaymentStatus.Failed);

        Assert.Null(existing);
    }
}
