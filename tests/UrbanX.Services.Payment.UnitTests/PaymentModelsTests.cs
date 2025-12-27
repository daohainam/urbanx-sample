using FluentAssertions;
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
        payment.Should().NotBeNull();
        payment.Amount.Should().Be(99.99m);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Method.Should().Be(PaymentMethod.CreditCard);
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
        payment.TransactionId.Should().BeNull();
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
        payment.TransactionId.Should().Be("TXN-12345");
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
        Enum.IsDefined(typeof(PaymentStatus), status).Should().BeTrue();
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.PayPal)]
    [InlineData(PaymentMethod.BankTransfer)]
    public void PaymentMethod_ShouldHaveAllDefinedValues(PaymentMethod method)
    {
        // Assert
        Enum.IsDefined(typeof(PaymentMethod), method).Should().BeTrue();
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
        payments.Should().HaveCount(4);
        payments.Select(p => p.Method).Should().OnlyHaveUniqueItems();
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
        payments.Should().HaveCount(5);
        payments.Select(p => p.Status).Should().OnlyHaveUniqueItems();
    }
}
