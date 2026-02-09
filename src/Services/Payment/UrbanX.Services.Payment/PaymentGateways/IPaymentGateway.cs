namespace UrbanX.Services.Payment.PaymentGateways;

/// <summary>
/// Anti-Corruption Layer interface for payment gateway integration.
/// Keeps UrbanX code independent from external payment providers.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Process a payment through the gateway
    /// </summary>
    /// <param name="request">Payment request with UrbanX domain model</param>
    /// <returns>Payment result with transaction details</returns>
    Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentGatewayRequest request);
    
    /// <summary>
    /// Refund a payment
    /// </summary>
    /// <param name="transactionId">Transaction ID from original payment</param>
    /// <param name="amount">Amount to refund</param>
    /// <returns>Refund result</returns>
    Task<PaymentGatewayResult> RefundPaymentAsync(string transactionId, decimal amount);
    
    /// <summary>
    /// Get payment status from gateway
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <returns>Current payment status</returns>
    Task<PaymentGatewayStatus> GetPaymentStatusAsync(string transactionId);
}

/// <summary>
/// Payment request model for UrbanX domain
/// </summary>
public record PaymentGatewayRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string? PaymentMethodId = null,
    Dictionary<string, string>? Metadata = null
);

/// <summary>
/// Payment result from gateway
/// </summary>
public record PaymentGatewayResult(
    bool Success,
    string? TransactionId,
    PaymentGatewayStatus Status,
    string? ErrorMessage = null,
    Dictionary<string, string>? AdditionalData = null
);

/// <summary>
/// Payment status enumeration for UrbanX domain
/// </summary>
public enum PaymentGatewayStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Refunded,
    Canceled
}
