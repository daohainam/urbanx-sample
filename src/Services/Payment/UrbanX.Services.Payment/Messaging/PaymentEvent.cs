namespace UrbanX.Services.Payment.Messaging;

public enum PaymentEventType
{
    Completed,
    Failed
}

public class PaymentEvent
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentEventType EventType { get; set; }
    public string? FailureReason { get; set; }
    public DateTime OccurredAt { get; set; }
}
