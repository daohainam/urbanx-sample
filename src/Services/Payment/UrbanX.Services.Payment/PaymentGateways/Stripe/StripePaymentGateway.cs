using Stripe;

namespace UrbanX.Services.Payment.PaymentGateways.Stripe;

/// <summary>
/// Anti-Corruption Layer adapter for Stripe payment gateway.
/// Translates between UrbanX domain models and Stripe API models.
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly StripeSettings _settings;

    public StripePaymentGateway(
        ILogger<StripePaymentGateway> logger,
        StripeSettings settings)
    {
        _logger = logger;
        _settings = settings;
        
        // Configure Stripe API key
        global::Stripe.StripeConfiguration.ApiKey = settings.SecretKey;
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentGatewayRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment via Stripe for Order {OrderId}, Amount: {Amount}", 
                request.OrderId, request.Amount);

            var paymentIntentService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = ConvertToStripeAmount(request.Amount, request.Currency),
                Currency = request.Currency.ToLowerInvariant(),
                PaymentMethod = request.PaymentMethodId,
                Confirm = !string.IsNullOrEmpty(request.PaymentMethodId),
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };
            
            // Add OrderId to metadata
            options.Metadata["order_id"] = request.OrderId.ToString();

            var paymentIntent = await paymentIntentService.CreateAsync(options);
            
            _logger.LogInformation("Stripe PaymentIntent created: {PaymentIntentId}, Status: {Status}", 
                paymentIntent.Id, paymentIntent.Status);

            return new PaymentGatewayResult(
                Success: paymentIntent.Status == "succeeded" || paymentIntent.Status == "processing",
                TransactionId: paymentIntent.Id,
                Status: MapStripeStatusToGatewayStatus(paymentIntent.Status),
                AdditionalData: new Dictionary<string, string>
                {
                    { "stripe_payment_intent_id", paymentIntent.Id },
                    { "stripe_status", paymentIntent.Status },
                    { "client_secret", paymentIntent.ClientSecret ?? string.Empty }
                }
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment processing failed for Order {OrderId}: {Message}", 
                request.OrderId, ex.Message);
            
            return new PaymentGatewayResult(
                Success: false,
                TransactionId: null,
                Status: PaymentGatewayStatus.Failed,
                ErrorMessage: ex.Message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment for Order {OrderId}", request.OrderId);
            
            return new PaymentGatewayResult(
                Success: false,
                TransactionId: null,
                Status: PaymentGatewayStatus.Failed,
                ErrorMessage: "An unexpected error occurred while processing the payment"
            );
        }
    }

    public async Task<PaymentGatewayResult> RefundPaymentAsync(string transactionId, decimal amount, string? currency = null)
    {
        try
        {
            _logger.LogInformation("Processing refund via Stripe for Transaction {TransactionId}, Amount: {Amount}", 
                transactionId, amount);

            var refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                // Only specify amount if currency is provided, otherwise Stripe will use full payment amount
                Amount = currency != null ? ConvertToStripeAmount(amount, currency) : null
            };

            var refund = await refundService.CreateAsync(options);
            
            _logger.LogInformation("Stripe Refund created: {RefundId}, Status: {Status}", 
                refund.Id, refund.Status);

            return new PaymentGatewayResult(
                Success: refund.Status == "succeeded",
                TransactionId: refund.Id,
                Status: refund.Status == "succeeded" ? PaymentGatewayStatus.Refunded : PaymentGatewayStatus.Failed,
                AdditionalData: new Dictionary<string, string>
                {
                    { "stripe_refund_id", refund.Id },
                    { "stripe_status", refund.Status }
                }
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for Transaction {TransactionId}: {Message}", 
                transactionId, ex.Message);
            
            return new PaymentGatewayResult(
                Success: false,
                TransactionId: null,
                Status: PaymentGatewayStatus.Failed,
                ErrorMessage: ex.Message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error refunding payment for Transaction {TransactionId}", transactionId);
            
            return new PaymentGatewayResult(
                Success: false,
                TransactionId: null,
                Status: PaymentGatewayStatus.Failed,
                ErrorMessage: "An unexpected error occurred while processing the refund"
            );
        }
    }

    public async Task<PaymentGatewayStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(transactionId);
            
            return MapStripeStatusToGatewayStatus(paymentIntent.Status);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get payment status from Stripe for Transaction {TransactionId}", 
                transactionId);
            return PaymentGatewayStatus.Failed;
        }
    }

    /// <summary>
    /// Convert decimal amount to Stripe's smallest currency unit (cents)
    /// Note: This implementation assumes 2 decimal places for most currencies.
    /// Some currencies like JPY, KRW use 0 decimal places.
    /// For production use, implement proper currency-specific handling using Stripe's currency list.
    /// </summary>
    private static long ConvertToStripeAmount(decimal amount, string currency)
    {
        // Zero-decimal currencies (no conversion needed)
        var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF",
            "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        if (zeroDecimalCurrencies.Contains(currency))
        {
            return (long)amount;
        }

        // Most currencies use 2 decimal places (cents)
        return (long)(amount * 100);
    }

    /// <summary>
    /// Map Stripe payment status to UrbanX PaymentGatewayStatus
    /// Anti-Corruption Layer translation
    /// </summary>
    private static PaymentGatewayStatus MapStripeStatusToGatewayStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => PaymentGatewayStatus.Pending,
            "requires_confirmation" => PaymentGatewayStatus.Pending,
            "requires_action" => PaymentGatewayStatus.Pending,
            "processing" => PaymentGatewayStatus.Processing,
            "succeeded" => PaymentGatewayStatus.Succeeded,
            "canceled" => PaymentGatewayStatus.Canceled,
            _ => PaymentGatewayStatus.Failed
        };
    }
}
