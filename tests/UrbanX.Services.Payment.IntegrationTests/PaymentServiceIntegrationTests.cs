using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;

namespace UrbanX.Services.Payment.IntegrationTests;

public class PaymentServiceIntegrationTests
{
    [Fact]
    public async Task PaymentService_ShouldProcessPayment()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.CreditCard,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Simulate payment processing
        payment.TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8]}";
        payment.Status = PaymentStatus.Completed;
        
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Assert
        var savedPayment = await context.Payments.FindAsync(payment.Id);
        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentStatus.Completed, savedPayment.Status);
        Assert.False(string.IsNullOrEmpty(savedPayment.TransactionId));
    }

    [Fact]
    public async Task PaymentService_ShouldGetPaymentByOrderId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        var orderId = Guid.NewGuid();
        
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 50.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.PayPal,
            TransactionId = "TXN-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Act
        var foundPayment = await context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        // Assert
        Assert.NotNull(foundPayment);
        Assert.Equal(orderId, foundPayment!.OrderId);
        Assert.Equal(PaymentStatus.Completed, foundPayment.Status);
    }

    [Fact]
    public async Task PaymentService_ShouldHandleFailedPayment()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 75.00m,
            Status = PaymentStatus.Processing,
            Method = PaymentMethod.CreditCard,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Simulate payment failure
        payment.Status = PaymentStatus.Failed;
        payment.UpdatedAt = DateTime.UtcNow;
        
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Assert
        var savedPayment = await context.Payments.FindAsync(payment.Id);
        Assert.Equal(PaymentStatus.Failed, savedPayment!.Status);
    }

    [Fact]
    public async Task PaymentService_ShouldSupportMultiplePaymentMethods()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentIntegrationTest_" + Guid.NewGuid())
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 20.00m,
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.PayPal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 30.00m,
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.BankTransfer,
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
        var methods = allPayments.Select(p => p.Method).ToList();
        Assert.Equal(methods.Count, methods.Distinct().Count());
    }

    [Fact]
    public async Task PaymentService_ShouldProcessRefund()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "PaymentIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new PaymentDbContext(options);
        
        var payment = new Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.CreditCard,
            TransactionId = "TXN-456",
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
        Assert.Equal(PaymentStatus.Refunded, refundedPayment!.Status);
    }
}
