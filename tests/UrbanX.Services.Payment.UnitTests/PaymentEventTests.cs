using UrbanX.Services.Payment.Messaging;

namespace UrbanX.Services.Payment.UnitTests;

public class PaymentEventTests
{
    [Fact]
    public void PaymentEvent_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var paymentEvent = new PaymentEvent
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            EventType = PaymentEventType.Completed,
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(paymentEvent);
        Assert.Equal(99.99m, paymentEvent.Amount);
        Assert.Equal(PaymentEventType.Completed, paymentEvent.EventType);
        Assert.Null(paymentEvent.FailureReason);
    }

    [Fact]
    public void PaymentEvent_ShouldAllowFailureReason()
    {
        // Arrange & Act
        var paymentEvent = new PaymentEvent
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 50.00m,
            EventType = PaymentEventType.Failed,
            FailureReason = "Card declined",
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(PaymentEventType.Failed, paymentEvent.EventType);
        Assert.Equal("Card declined", paymentEvent.FailureReason);
    }

    [Theory]
    [InlineData(PaymentEventType.Completed)]
    [InlineData(PaymentEventType.Failed)]
    public void PaymentEventType_ShouldHaveAllDefinedValues(PaymentEventType eventType)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PaymentEventType), eventType));
    }

    [Fact]
    public void PaymentEvent_CompletedEventShouldHaveNullFailureReason()
    {
        // Arrange & Act
        var paymentEvent = new PaymentEvent
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            EventType = PaymentEventType.Completed,
            FailureReason = null,
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(PaymentEventType.Completed, paymentEvent.EventType);
        Assert.Null(paymentEvent.FailureReason);
    }
}
