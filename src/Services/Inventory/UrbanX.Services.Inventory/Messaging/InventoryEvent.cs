namespace UrbanX.Services.Inventory.Messaging;

public enum InventoryEventType
{
    Reserved,
    ReservationFailed,
    Released
}

public class InventoryEvent
{
    public Guid OrderId { get; set; }
    public InventoryEventType EventType { get; set; }
    public List<InventoryEventItem> Items { get; set; } = new();
    public string? FailureReason { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class InventoryEventItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

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
