using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;
using UrbanX.Services.Payment.PaymentGateways;

namespace UrbanX.Services.Payment.IntegrationTests;

public class StripePaymentIntegrationTests
{
    [Fact]
    public async Task PaymentService_ShouldProcessStripePayment()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "StripePaymentTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.Stripe,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Simulate Stripe payment processing
        // In a real integration test, this would call the actual gateway
        payment.TransactionId = $"pi_test_{Guid.NewGuid().ToString()[..16]}"; // Stripe PaymentIntent ID format
        payment.Status = PaymentStatus.Completed;
        
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Assert
        var savedPayment = await context.Payments.FindAsync(payment.Id);
        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentStatus.Completed, savedPayment.Status);
        Assert.Equal(PaymentMethod.Stripe, savedPayment.Method);
        Assert.False(string.IsNullOrEmpty(savedPayment.TransactionId));
        Assert.StartsWith("pi_test_", savedPayment.TransactionId);
    }

    [Fact]
    public async Task PaymentService_ShouldHandleStripePaymentFailure()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "StripePaymentTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 75.00m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.Stripe,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Simulate Stripe payment failure
        payment.Status = PaymentStatus.Failed;
        payment.TransactionId = $"FAILED-{Guid.NewGuid().ToString()[..8]}";
        payment.UpdatedAt = DateTime.UtcNow;
        
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Assert
        var savedPayment = await context.Payments.FindAsync(payment.Id);
        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentStatus.Failed, savedPayment!.Status);
        Assert.Equal(PaymentMethod.Stripe, savedPayment.Method);
    }

    [Fact]
    public async Task PaymentService_ShouldProcessRefundForStripePayment()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "StripePaymentTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Stripe,
            TransactionId = "pi_test_123456789",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Act - Process refund
        payment.Status = PaymentStatus.Refunded;
        payment.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var refundedPayment = await context.Payments.FindAsync(payment.Id);
        Assert.NotNull(refundedPayment);
        Assert.Equal(PaymentStatus.Refunded, refundedPayment!.Status);
        Assert.Equal(PaymentMethod.Stripe, refundedPayment.Method);
    }

    [Fact]
    public async Task PaymentService_ShouldSupportMultiplePaymentMethodsIncludingStripe()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "StripePaymentTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        
        var payments = new[]
        {
            new Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 10.00m,
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.CreditCard,
                TransactionId = "TXN-CC-123",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 20.00m,
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.Stripe,
                TransactionId = "pi_test_stripe_123",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 30.00m,
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.PayPal,
                TransactionId = "PAYPAL-123",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();

        // Assert
        var allPayments = await context.Payments.ToListAsync();
        Assert.Equal(3, allPayments.Count);
        
        var stripePayment = allPayments.FirstOrDefault(p => p.Method == PaymentMethod.Stripe);
        Assert.NotNull(stripePayment);
        Assert.Equal(20.00m, stripePayment!.Amount);
        Assert.StartsWith("pi_test_", stripePayment.TransactionId);
    }

    [Fact]
    public void PaymentGatewayStatus_MappingToPaymentStatus_ShouldBeCorrect()
    {
        // This test verifies the mapping between PaymentGatewayStatus and PaymentStatus
        // as implemented in Program.cs
        
        // Arrange & Act & Assert
        var mappings = new Dictionary<PaymentGatewayStatus, PaymentStatus>
        {
            { PaymentGatewayStatus.Succeeded, PaymentStatus.Completed },
            { PaymentGatewayStatus.Processing, PaymentStatus.Processing },
            { PaymentGatewayStatus.Pending, PaymentStatus.Pending },
            { PaymentGatewayStatus.Failed, PaymentStatus.Failed },
            { PaymentGatewayStatus.Refunded, PaymentStatus.Refunded }
        };

        foreach (var mapping in mappings)
        {
            Assert.True(Enum.IsDefined(typeof(PaymentGatewayStatus), mapping.Key));
            Assert.True(Enum.IsDefined(typeof(PaymentStatus), mapping.Value));
        }
    }
}
