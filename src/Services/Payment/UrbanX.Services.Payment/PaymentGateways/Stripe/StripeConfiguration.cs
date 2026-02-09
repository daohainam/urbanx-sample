namespace UrbanX.Services.Payment.PaymentGateways.Stripe;

/// <summary>
/// Stripe-specific configuration for UrbanX
/// </summary>
public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
