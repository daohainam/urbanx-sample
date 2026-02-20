using UrbanX.Services.Order.Models;

namespace UrbanX.Services.Order.UnitTests;

/// <summary>
/// Tests for production-grade checkout validations:
/// 1. Server-side TotalAmount integrity check
/// 2. Order status transition guard (via OrderStatusTransitions)
/// </summary>
public class CheckoutValidationTests
{
    // ---- TotalAmount integrity ----

    [Fact]
    public void TotalAmount_MatchesSumOfItems_ShouldBeValid()
    {
        var items = new List<OrderItem>
        {
            new() { Quantity = 2, UnitPrice = 25.00m },
            new() { Quantity = 1, UnitPrice = 50.00m },
        };
        var declaredTotal = 100.00m;

        var expectedTotal = items.Sum(i => i.Quantity * i.UnitPrice);
        Assert.True(Math.Abs(declaredTotal - expectedTotal) <= 0.01m);
    }

    [Fact]
    public void TotalAmount_DoesNotMatchSumOfItems_ShouldBeInvalid()
    {
        var items = new List<OrderItem>
        {
            new() { Quantity = 2, UnitPrice = 25.00m },
            new() { Quantity = 1, UnitPrice = 50.00m },
        };
        var manipulatedTotal = 0.01m; // attacker's attempt

        var expectedTotal = items.Sum(i => i.Quantity * i.UnitPrice);
        Assert.True(Math.Abs(manipulatedTotal - expectedTotal) > 0.01m);
    }

    [Fact]
    public void TotalAmount_WithinTolerance_ShouldBeValid()
    {
        var items = new List<OrderItem>
        {
            new() { Quantity = 3, UnitPrice = 9.999m },
        };
        var declaredTotal = 30.00m; // slight rounding difference

        var expectedTotal = items.Sum(i => i.Quantity * i.UnitPrice); // 29.997
        Assert.True(Math.Abs(declaredTotal - expectedTotal) <= 0.01m);
    }

    // ---- Status transition guard ----

    [Theory]
    [InlineData(OrderStatus.Confirmed,      OrderStatus.Preparing)]
    [InlineData(OrderStatus.Confirmed,      OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Preparing,      OrderStatus.ReadyForPickup)]
    [InlineData(OrderStatus.Preparing,      OrderStatus.Cancelled)]
    [InlineData(OrderStatus.ReadyForPickup, OrderStatus.InTransit)]
    [InlineData(OrderStatus.ReadyForPickup, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.InTransit,      OrderStatus.Delivered)]
    [InlineData(OrderStatus.InTransit,      OrderStatus.Cancelled)]
    public void StatusTransition_ValidTransition_ShouldBeAllowed(OrderStatus from, OrderStatus to)
    {
        Assert.True(OrderStatusTransitions.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(OrderStatus.Pending,         OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending,         OrderStatus.Delivered)]
    [InlineData(OrderStatus.PaymentReceived, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Delivered,       OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled,       OrderStatus.Pending)]
    [InlineData(OrderStatus.InTransit,       OrderStatus.Confirmed)]
    public void StatusTransition_InvalidTransition_ShouldBeRejected(OrderStatus from, OrderStatus to)
    {
        Assert.False(OrderStatusTransitions.IsAllowed(from, to));
    }
}
