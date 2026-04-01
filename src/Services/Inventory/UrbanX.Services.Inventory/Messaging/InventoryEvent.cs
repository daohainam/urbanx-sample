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

// Note: OrderCancelledEvent is intentionally duplicated from the Order service.
// Each microservice owns its own copy of event DTOs to remain independently deployable.
// The JSON schema must stay in sync across services; if schema evolution is needed,
// add fields with default values to maintain backward compatibility.
public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
}
