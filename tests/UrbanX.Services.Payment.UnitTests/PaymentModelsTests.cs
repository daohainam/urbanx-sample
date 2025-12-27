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
            Method = PaymentMethod.CreditCard,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(payment);
        Assert.Equal(99.99m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(PaymentMethod.CreditCard, payment.Method);
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
            Method = PaymentMethod.PayPal,
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
            Method = PaymentMethod.DebitCard,
            TransactionId = "TXN-12345",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("TXN-12345", payment.TransactionId);
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
    public void PaymentMethod_ShouldHaveAllDefinedValues(PaymentMethod method)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PaymentMethod), method));
    }

    [Fact]
    public void Payment_ShouldSupportAllPaymentMethods()
    {
        // Arrange & Act
        var payments = new[]
        {
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 10m, Status = PaymentStatus.Completed, Method = PaymentMethod.CreditCard, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 20m, Status = PaymentStatus.Completed, Method = PaymentMethod.DebitCard, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 30m, Status = PaymentStatus.Completed, Method = PaymentMethod.PayPal, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 40m, Status = PaymentStatus.Completed, Method = PaymentMethod.BankTransfer, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        // Assert
        Assert.Equal(4, payments.Length);
        var methods = payments.Select(p => p.Method).ToList();
        Assert.Equal(methods.Count, methods.Distinct().Count());
    }

    [Fact]
    public void Payment_ShouldSupportAllStatuses()
    {
        // Arrange & Act
        var payments = new[]
        {
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 10m, Status = PaymentStatus.Pending, Method = PaymentMethod.CreditCard, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 20m, Status = PaymentStatus.Processing, Method = PaymentMethod.CreditCard, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 30m, Status = PaymentStatus.Completed, Method = PaymentMethod.CreditCard, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 40m, Status = PaymentStatus.Failed, Method = PaymentMethod.CreditCard, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Models.Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 50m, Status = PaymentStatus.Refunded, Method = PaymentMethod.CreditCard, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        // Assert
        Assert.Equal(5, payments.Length);
        var statuses = payments.Select(p => p.Status).ToList();
        Assert.Equal(statuses.Count, statuses.Distinct().Count());
    }
}
