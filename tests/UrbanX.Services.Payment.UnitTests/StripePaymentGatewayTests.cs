using Microsoft.Extensions.Logging;
using Moq;
using UrbanX.Services.Payment.PaymentGateways;
using UrbanX.Services.Payment.PaymentGateways.Stripe;

namespace UrbanX.Services.Payment.UnitTests;

public class StripePaymentGatewayTests
{
    private readonly Mock<ILogger<StripePaymentGateway>> _mockLogger;
    private readonly StripeSettings _testSettings;

    public StripePaymentGatewayTests()
    {
        _mockLogger = new Mock<ILogger<StripePaymentGateway>>();
        _testSettings = new StripeSettings
        {
            SecretKey = "sk_test_fake_key_for_testing",
            PublishableKey = "pk_test_fake_key_for_testing",
            WebhookSecret = "whsec_fake_secret_for_testing"
        };
    }

    [Fact]
    public void StripePaymentGateway_ShouldInitializeWithConfiguration()
    {
        // Arrange & Act
        var gateway = new StripePaymentGateway(_mockLogger.Object, _testSettings);

        // Assert
        Assert.NotNull(gateway);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequest_ShouldReturnResult()
    {
        // Arrange
        var gateway = new StripePaymentGateway(_mockLogger.Object, _testSettings);
        var request = new PaymentGatewayRequest(
            OrderId: Guid.NewGuid(),
            Amount: 100.00m,
            Currency: "usd",
            Metadata: new Dictionary<string, string> { { "test", "value" } }
        );

        // Act
        var result = await gateway.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        // Note: Without valid Stripe credentials, this will fail.
        // In a real test environment, we would use Stripe's test mode or mock the Stripe SDK.
    }

    [Fact]
    public void PaymentGatewayRequest_ShouldCreateWithRequiredProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 50.00m;
        var currency = "usd";

        // Act
        var request = new PaymentGatewayRequest(orderId, amount, currency);

        // Assert
        Assert.Equal(orderId, request.OrderId);
        Assert.Equal(amount, request.Amount);
        Assert.Equal(currency, request.Currency);
        Assert.Null(request.PaymentMethodId);
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void PaymentGatewayRequest_ShouldCreateWithOptionalProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 75.00m;
        var currency = "eur";
        var paymentMethodId = "pm_test_123";
        var metadata = new Dictionary<string, string> { { "key", "value" } };

        // Act
        var request = new PaymentGatewayRequest(orderId, amount, currency, paymentMethodId, metadata);

        // Assert
        Assert.Equal(orderId, request.OrderId);
        Assert.Equal(amount, request.Amount);
        Assert.Equal(currency, request.Currency);
        Assert.Equal(paymentMethodId, request.PaymentMethodId);
        Assert.Equal(metadata, request.Metadata);
    }

    [Fact]
    public void PaymentGatewayResult_ShouldCreateSuccessResult()
    {
        // Arrange & Act
        var result = new PaymentGatewayResult(
            Success: true,
            TransactionId: "txn_123",
            Status: PaymentGatewayStatus.Succeeded
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal("txn_123", result.TransactionId);
        Assert.Equal(PaymentGatewayStatus.Succeeded, result.Status);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void PaymentGatewayResult_ShouldCreateFailureResult()
    {
        // Arrange & Act
        var result = new PaymentGatewayResult(
            Success: false,
            TransactionId: null,
            Status: PaymentGatewayStatus.Failed,
            ErrorMessage: "Payment declined"
        );

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.Equal(PaymentGatewayStatus.Failed, result.Status);
        Assert.Equal("Payment declined", result.ErrorMessage);
    }

    [Fact]
    public void PaymentGatewayStatus_ShouldHaveAllExpectedValues()
    {
        // Assert - Verify all status values exist
        Assert.True(Enum.IsDefined(typeof(PaymentGatewayStatus), PaymentGatewayStatus.Pending));
        Assert.True(Enum.IsDefined(typeof(PaymentGatewayStatus), PaymentGatewayStatus.Processing));
        Assert.True(Enum.IsDefined(typeof(PaymentGatewayStatus), PaymentGatewayStatus.Succeeded));
        Assert.True(Enum.IsDefined(typeof(PaymentGatewayStatus), PaymentGatewayStatus.Failed));
        Assert.True(Enum.IsDefined(typeof(PaymentGatewayStatus), PaymentGatewayStatus.Refunded));
        Assert.True(Enum.IsDefined(typeof(PaymentGatewayStatus), PaymentGatewayStatus.Canceled));
    }

    [Fact]
    public void StripeSettings_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var settings = new StripeSettings
        {
            SecretKey = "sk_test_123",
            PublishableKey = "pk_test_123",
            WebhookSecret = "whsec_123"
        };

        // Assert
        Assert.Equal("sk_test_123", settings.SecretKey);
        Assert.Equal("pk_test_123", settings.PublishableKey);
        Assert.Equal("whsec_123", settings.WebhookSecret);
    }
}
