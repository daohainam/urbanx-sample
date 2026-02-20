namespace UrbanX.Services.Order.Models;

/// <summary>
/// Defines the allowed order status transitions for the fulfilment lifecycle.
/// Saga-controlled states (Pending, PaymentReceived) are intentionally absent
/// so that they cannot be overridden through the manual status endpoint.
/// </summary>
public static class OrderStatusTransitions
{
    public static readonly IReadOnlyDictionary<OrderStatus, IReadOnlySet<OrderStatus>> Allowed =
        new Dictionary<OrderStatus, IReadOnlySet<OrderStatus>>
        {
            { OrderStatus.Confirmed,      new HashSet<OrderStatus> { OrderStatus.Preparing,      OrderStatus.Cancelled } },
            { OrderStatus.Preparing,      new HashSet<OrderStatus> { OrderStatus.ReadyForPickup, OrderStatus.Cancelled } },
            { OrderStatus.ReadyForPickup, new HashSet<OrderStatus> { OrderStatus.InTransit,      OrderStatus.Cancelled } },
            { OrderStatus.InTransit,      new HashSet<OrderStatus> { OrderStatus.Delivered,      OrderStatus.Cancelled } },
        };

    public static bool IsAllowed(OrderStatus from, OrderStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}
