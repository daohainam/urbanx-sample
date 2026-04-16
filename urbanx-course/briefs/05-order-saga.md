# Module 5: The Order Saga

### Teaching Arc
- **Metaphor:** A relay race. The Order Service passes the baton to Inventory ("reserve the stock"), which passes it to the customer ("pay now"), which passes it to Payment ("process the charge"), which passes it back to Order ("you're confirmed"). If a runner drops the baton, the team runs the race in reverse to undo what was done.
- **Opening hook:** "Fulfilling an order requires three things to happen in three different databases: reserve inventory, charge payment, and confirm the order. If any one fails, the others must undo their work. That's not possible with a normal database transaction."
- **Key insight:** The Choreography Saga replaces an impossible distributed transaction with a chain of events — each service does its job and broadcasts the result. There is no central controller. Services react autonomously.
- **"Why should I care?":** When you add a new step to the order flow — say, "notify the warehouse" — you just subscribe to the right Kafka event. No changes to existing services. The saga grows naturally.

### Code Snippets (pre-extracted)

**Snippet A — Order state machine** (OrderStatusTransitions.cs):
```csharp
public static readonly IReadOnlyDictionary<OrderStatus, IReadOnlySet<OrderStatus>> Allowed =
    new Dictionary<OrderStatus, IReadOnlySet<OrderStatus>>
    {
        [OrderStatus.Confirmed]      = new HashSet<OrderStatus> { OrderStatus.Preparing,      OrderStatus.Cancelled },
        [OrderStatus.Preparing]      = new HashSet<OrderStatus> { OrderStatus.ReadyForPickup, OrderStatus.Cancelled },
        [OrderStatus.ReadyForPickup] = new HashSet<OrderStatus> { OrderStatus.InTransit,      OrderStatus.Cancelled },
        [OrderStatus.InTransit]      = new HashSet<OrderStatus> { OrderStatus.Delivered,      OrderStatus.Cancelled },
    };
```

**Snippet B — Payment failure triggers compensation** (KafkaPaymentResponseConsumer.cs ~lines 50-80):
```csharp
if (paymentEvent.EventType == "Failed")
{
    order.Status = OrderStatus.Cancelled;
    order.AddStatusHistory(OrderStatus.Cancelled, "Payment failed");

    await db.OutboxMessages.AddAsync(new OutboxMessage
    {
        EventType = "OrderCancelled",
        Payload   = JsonSerializer.Serialize(new OrderCancelledEvent
        {
            OrderId    = order.Id,
            OccurredAt = DateTime.UtcNow
        })
    });

    await db.SaveChangesAsync();
}
```

**Snippet C — Inventory compensation** (KafkaOrderCancelledConsumer.cs ~lines 70-110):
```csharp
var reservations = await db.InventoryReservations
    .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved)
    .ToListAsync();

if (reservations.Count == 0) return;

foreach (var reservation in reservations)
{
    reservation.Status = ReservationStatus.Released;
    var item = await db.InventoryItems
        .FirstOrDefaultAsync(i => i.ProductId == reservation.ProductId);
    if (item != null)
        item.QuantityReserved -= reservation.Quantity;
}
await db.SaveChangesAsync();
```

### Interactive Elements

- [x] **Data flow animation** — id: `flow-saga`. Show the full happy-path saga. Actors (use exact IDs flow-actor-1 through flow-actor-4): Order Service (actor-1 color), Kafka (dark bg, text #CDD6F4), Inventory Service (actor-3 color), Payment Service (actor-5 color). Steps:
  1. highlight flow-actor-1: "Customer places order. Order Service saves it (Status: Pending) + writes OrderCreated to Outbox."
  2. packet from actor-1 to actor-2 + highlight flow-actor-2: "Outbox Relay publishes OrderCreated to order.created Kafka topic."
  3. packet from actor-2 to actor-3 + highlight flow-actor-3: "Inventory Service reads the event. Checks stock — 15 available. Reserves 2. Publishes InventoryReserved to inventory.events."
  4. packet from actor-3 to actor-2 + highlight flow-actor-2: "Kafka holds the InventoryReserved event for the Order Service."
  5. packet from actor-2 to actor-1 + highlight flow-actor-1: "Order Service reads InventoryReserved. Order still Pending — waiting for payment."
  6. highlight flow-actor-4: "Customer submits payment. Payment Service calls Stripe API. Card approved. Publishes PaymentCompleted to payment.events."
  7. packet from actor-4 to actor-2 + highlight flow-actor-2: "Kafka holds PaymentCompleted for Order Service."
  8. packet from actor-2 to actor-1 + highlight flow-actor-1: "Order Service reads PaymentCompleted. Updates status to PaymentReceived. Saga complete! Merchant will fulfill the order."

- [x] **Code ↔ English translation** — Use Snippet A (state machine). Right translation lines:
  - "This is a whitelist: only these status transitions are allowed"
  - "Confirmed can go to Preparing or Cancelled — nothing else"
  - "You cannot skip from Confirmed directly to Delivered"
  - "Every service update checks this table before changing status"
  - "Invalid transitions throw an error — protecting the order from entering impossible states"

- [x] **Group chat animation** — id: `chat-saga-fail`. Show the FAILURE path (payment declined). Actors: Order Service (actor-1), Kafka (dark), Inventory (actor-3), Payment (actor-5). Messages:
  - msg 0, sender: order — "Card submitted. Waiting for Payment Service result..."
  - msg 1, sender: payment — "Called Stripe. Card declined: insufficient funds. Publishing PaymentFailed to payment.events."
  - msg 2, sender: order — "PaymentFailed received. Setting order status to Cancelled. Publishing OrderCancelled to order.cancelled."
  - msg 3, sender: inventory — "OrderCancelled received for order #abc123. Finding Reserved reservations..."
  - msg 4, sender: inventory — "Found 2x Wireless Headphones reserved. Releasing. QuantityReserved back to 0."
  - msg 5, sender: inventory — "Done. Stock is available again for other customers."
  - msg 6, sender: order — "Compensation complete. The system is back to exactly the state it was before the order. No stock locked, no charge made."

- [x] **Code ↔ English translation** — Use Snippet B (payment failure triggers compensation). Right translation lines:
  - "Payment event type is Failed — the card was declined or an error occurred"
  - "Update the order status to Cancelled"
  - "Record this in the status history: when it happened and why"
  - "Write an OrderCancelled event to the Outbox — in the same transaction as the status update"
  - "The Outbox guarantees Inventory will hear about this cancellation, even if the server restarts now"

- [x] **Callout box** — callout-accent: "💡 Choreography vs. Orchestration: In choreography (UrbanX's approach), each service reacts to events independently — like a jazz ensemble improvising together. In orchestration, a central 'order manager' service tells each service what to do — like a conductor. Choreography is more resilient (no single point of failure) but harder to visualize. That's what this animation is for."

- [x] **Quiz** — 2 questions, scenario + debugging style:
  - Q1: "A new government regulation requires UrbanX to notify a tax authority system every time a payment completes. Where should this code live?" Options: (a) In the Payment Service — it knows about payments, (b) In the Order Service — it manages the saga, (c) In a new Tax Notification Service that subscribes to payment.events ✓, (d) In the API Gateway — it sees all transactions. Right: "Exactly! This is the extensibility win of choreography. A new Tax Notification Service subscribes to payment.events — zero changes to Payment Service, zero changes to Order Service. Clean separation." Wrong: "Think about what the saga pattern buys you. Any service can subscribe to payment.events without changing the Payment Service. A dedicated Tax Service is the clean, decoupled answer."
  - Q2: "An order is stuck in Preparing status. The merchant wants to move it directly to Delivered to skip the other steps. Will the code allow this?" Options: (a) Yes — the merchant has admin access, (b) Yes — status updates are unrestricted for merchants, (c) No — the state machine only allows Preparing → ReadyForPickup → InTransit → Delivered ✓, (d) No — only the system can change order status. Right: "Correct! The state machine prevents impossible jumps. This protects data integrity — if Preparing jumped directly to Delivered, the shipment and transit tracking would be skipped entirely." Wrong: "Check the state machine in Snippet A. Preparing can only go to ReadyForPickup or Cancelled. The code throws an error for any other transition — even from admin users."

- [x] **Glossary tooltips** — mark on first use:
  - "Saga pattern" → "A way to manage multi-step operations across separate services. Each step publishes an event; if a step fails, previous steps are undone via compensating transactions."
  - "choreography" → "Each service reacts to events independently, with no central coordinator. Like a flock of birds that moves together without a leader."
  - "orchestration" → "A central coordinator tells each service what to do and in what order. Like a conductor directing an orchestra. More control but a single point of failure."
  - "compensation" → "Undoing the effects of a previous successful step when a later step fails. The distributed equivalent of a database rollback — but done via events."
  - "state machine" → "A system that can only be in one defined state at a time, with explicit rules about which transitions are allowed. Prevents objects from entering impossible states."
  - "distributed transaction" → "An operation that must be atomic across multiple separate databases or services — notoriously hard to implement correctly, which is why Sagas exist."

### Reference Files to Read
- `references/interactive-elements.md` → "Message Flow / Data Flow Animation", "Group Chat Animation", "Code ↔ English Translation Blocks", "Callout Boxes", "Multiple-Choice Quizzes", "Glossary Tooltips"
- `references/content-philosophy.md` → full file
- `references/gotchas.md` → full file

### Connections
- **Previous module:** "Never Lose a Message" — established that events are reliably published via Outbox. Now shows WHAT those events orchestrate.
- **Next module:** "Two Databases, One Truth" — introduces CQRS. The Saga uses Kafka, and CQRS also uses Kafka for a completely different purpose.
- **Tone/style notes:** Module bg: `var(--color-bg)` (odd module). The failure path chat should feel like a smooth recovery — not a crisis. The system handles it gracefully.
