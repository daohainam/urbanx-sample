namespace UrbanX.Services.Order.Messaging;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public List<OrderCreatedEventItem> Items { get; set; } = new();
    public DateTime OccurredAt { get; set; }
}

public class OrderCreatedEventItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public enum InventoryEventType
{
    Reserved,
    ReservationFailed,
    Released
}

public class InventoryResponseEvent
{
    public Guid OrderId { get; set; }
    public InventoryEventType EventType { get; set; }
    public string? FailureReason { get; set; }
    public DateTime OccurredAt { get; set; }
}
