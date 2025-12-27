using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;

namespace UrbanX.Services.Payment.UnitTests;

public class PaymentDbContextTests
{
    [Fact]
    public void PaymentDbContext_ShouldConfigurePaymentEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentTestDb_" + Guid.NewGuid())
            .Options;

        // Act
        using var context = new PaymentDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Models.Payment));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.FindPrimaryKey()!.Properties.Should().Contain(p => p.Name == "Id");
    }

    [Fact]
    public async Task PaymentDbContext_ShouldAddAndRetrievePayment()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentTestDb_" + Guid.NewGuid())
            .Options;

        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.CreditCard,
            TransactionId = "TXN-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new PaymentDbContext(options))
        {
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new PaymentDbContext(options))
        {
            var savedPayment = await context.Payments.FindAsync(payment.Id);
            savedPayment.Should().NotBeNull();
            savedPayment!.Amount.Should().Be(99.99m);
            savedPayment.Status.Should().Be(PaymentStatus.Completed);
            savedPayment.TransactionId.Should().Be("TXN-123");
        }
    }

    [Fact]
    public async Task PaymentDbContext_ShouldFindPaymentByOrderId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentTestDb_" + Guid.NewGuid())
            .Options;

        var orderId = Guid.NewGuid();
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 50.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.PayPal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new PaymentDbContext(options))
        {
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new PaymentDbContext(options))
        {
            var foundPayment = await context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
            foundPayment.Should().NotBeNull();
            foundPayment!.OrderId.Should().Be(orderId);
        }
    }

    [Fact]
    public async Task PaymentDbContext_ShouldSupportMultiplePayments()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentTestDb_" + Guid.NewGuid())
            .Options;

        var payments = new[]
        {
            new Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 10.00m,
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.CreditCard,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 20.00m,
                Status = PaymentStatus.Pending,
                Method = PaymentMethod.DebitCard,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        using (var context = new PaymentDbContext(options))
        {
            context.Payments.AddRange(payments);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new PaymentDbContext(options))
        {
            var allPayments = await context.Payments.ToListAsync();
            allPayments.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task PaymentDbContext_ShouldUpdatePaymentStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentTestDb_" + Guid.NewGuid())
            .Options;

        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 75.00m,
            Status = PaymentStatus.Pending,
            Method = PaymentMethod.BankTransfer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using (var context = new PaymentDbContext(options))
        {
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new PaymentDbContext(options))
        {
            var savedPayment = await context.Payments.FindAsync(payment.Id);
            savedPayment!.Status = PaymentStatus.Completed;
            savedPayment.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new PaymentDbContext(options))
        {
            var updatedPayment = await context.Payments.FindAsync(payment.Id);
            updatedPayment!.Status.Should().Be(PaymentStatus.Completed);
        }
    }
}
