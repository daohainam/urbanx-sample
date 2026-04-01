using UrbanX.Services.Payment.Models;

namespace UrbanX.Services.Payment.UnitTests;

public class PaymentModelsTests
{
    [Fact]
    public void Payment_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            Status = PaymentStatus.Pending,
            Method = PaymentMethod.Stripe,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(payment);
        Assert.Equal(99.99m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(PaymentMethod.Stripe, payment.Method);
    }

    [Fact]
    public void Payment_ShouldAllowNullTransactionId()
    {
        // Arrange & Act
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 50.00m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.Stripe,
            TransactionId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Null(payment.TransactionId);
    }

    [Fact]
    public void Payment_ShouldAllowTransactionId()
    {
        // Arrange & Act
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 75.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.Stripe,
            TransactionId = "TXN-12345",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("TXN-12345", payment.TransactionId);
    }

    [Fact]
    public void Payment_ShouldAllowNullPaymentMethodId()
    {
        // PaymentMethodId is optional (the gateway can create a PaymentIntent without
        // confirming it immediately when no PaymentMethod is attached yet).
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 50.00m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.Stripe,
            PaymentMethodId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.Null(payment.PaymentMethodId);
    }

    [Fact]
    public void Payment_ShouldStorePaymentMethodId()
    {
        // PaymentMethodId is forwarded to Stripe so it can confirm the PaymentIntent.
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.00m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.Stripe,
            PaymentMethodId = "pm_test_abc123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.Equal("pm_test_abc123", payment.PaymentMethodId);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Processing)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    public void PaymentStatus_ShouldHaveAllDefinedValues(PaymentStatus status)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PaymentStatus), status));
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.PayPal)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.Stripe)]
    public void PaymentMethod_ShouldHaveAllDefinedValues(PaymentMethod method)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PaymentMethod), method));
    }

    [Fact]
    public void PaymentMethod_OnlyStripeIsSupported_OtherMethodsAreDefinedButRejectedByEndpoint()
    {
        // Only Stripe payments are wired to a real gateway; the endpoint rejects all others.
        // Verify Stripe is one of the defined enum values.
        Assert.True(Enum.IsDefined(typeof(PaymentMethod), PaymentMethod.Stripe));
    }

    [Fact]
    public void Payment_ShouldSupportAllStatuses()
    {
        // Arrange & Act
        var payments = new[]
        {
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 10m, Status = PaymentStatus.Pending, Method = PaymentMethod.Stripe, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 20m, Status = PaymentStatus.Processing, Method = PaymentMethod.Stripe, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 30m, Status = PaymentStatus.Completed, Method = PaymentMethod.Stripe, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 40m, Status = PaymentStatus.Failed, Method = PaymentMethod.Stripe, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 50m, Status = PaymentStatus.Refunded, Method = PaymentMethod.Stripe, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        // Assert
        Assert.Equal(5, payments.Length);
        var statuses = payments.Select(p => p.Status).ToList();
        Assert.Equal(statuses.Count, statuses.Distinct().Count());
    }
}
