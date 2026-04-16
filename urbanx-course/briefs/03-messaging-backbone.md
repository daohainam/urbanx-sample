# Module 3: The Messaging Backbone

### Teaching Arc
- **Metaphor:** A bulletin board in a shared office. Instead of tapping everyone on the shoulder individually (tight coupling), you pin a note to the board. Anyone who cares reads it when they're ready. Nobody is interrupted. Nobody misses it.
- **Opening hook:** "What if the Inventory Service crashes while you're checking out? In a direct-call system, your order would fail instantly. With Kafka, the order message just waits on the board until Inventory comes back."
- **Key insight:** Kafka decouples services in time. Publisher doesn't wait for consumer. Consumer can catch up from any point in the message log. This is why the whole system is resilient.
- **"Why should I care?":** Understanding Kafka means you can tell an AI "add a new service that listens to payment events" and know exactly what that means — subscribe to the payment.events topic, no changes needed to the Payment Service.

### Code Snippets (pre-extracted)

**Snippet A — Kafka consumer setup** (KafkaOrderEventConsumer.cs, lines ~15-50):
```csharp
var config = new ConsumerConfig
{
    BootstrapServers = _kafkaSettings.BootstrapServers,
    GroupId = "inventory-service",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe("order.created");

while (!stoppingToken.IsCancellationRequested)
{
    var result = consumer.Consume(stoppingToken);
    var orderEvent = JsonSerializer.Deserialize<OrderEvent>(result.Message.Value);
    await ProcessOrderAsync(orderEvent, stoppingToken);
    consumer.Commit(result);
}
```

**Snippet B — Kafka producer (OutboxRelayService publishing)**:
```csharp
await _producer.ProduceAsync(
    topic: "order.created",
    message: new Message<string, string>
    {
        Key   = outboxMessage.Id.ToString(),
        Value = outboxMessage.Payload
    });
```

**Snippet C — OrderEvent schema** (OrderEvent.cs):
```csharp
public record OrderEvent(
    Guid   OrderId,
    List<OrderEventItem> Items,
    DateTime OccurredAt
);

public record OrderEventItem(
    Guid ProductId,
    int  Quantity
);
```

### Interactive Elements

- [x] **Code ↔ English translation** — Use Snippet A (Kafka consumer). Right translation lines:
  - "Connect to the Kafka server and join the group 'inventory-service'"
  - "GroupId means: if multiple Inventory instances run, they share the work — no message is processed twice"
  - "EnableAutoCommit=false: we manually confirm receipt only after successful processing — no lost messages"
  - "Subscribe to the order.created topic — listen for new orders"
  - "Loop forever: wait for a message, process it, then manually confirm (commit) it was handled"

- [x] **Group chat animation** — id: `chat-kafka`. Show the full message flow for a new order. Actors: Order Service (actor-1 color), Kafka (dark `#1E1E2E` bg with light text `#CDD6F4` initial "KF"), Inventory Service (actor-3 color). Messages:
  - msg 0, sender: order — "New order #abc123 placed! I'm NOT calling Inventory directly. I'll post to order.created."
  - msg 1, sender: order — "Outbox entry saved to my database. The relay will pick it up in under 5 seconds."
  - msg 2, sender: kafka — "Message received. Stored at offset 182, partition 0. All subscribers will get this — whenever they're ready."
  - msg 3, sender: inventory — "Got it! Reading order #abc123... 2x Wireless Headphones. Checking stock now."
  - msg 4, sender: inventory — "15 in stock, 0 reserved. Reserving 2. Publishing InventoryReserved to inventory.events."
  - msg 5, sender: kafka — "InventoryReserved stored at offset 183. Order Service is subscribed — it will pick this up next."
  - msg 6, sender: order — "InventoryReserved received. Order status still Pending. Waiting for payment."

  Use `display:none` on all messages initially (main.js controls reveal via Next Message / Play All / Replay buttons).

- [x] **Badge list** — show all 5 Kafka topics as `.badge-list` with `.badge-item` rows. Each has a `.badge-code` (topic name) and `.badge-desc` (who produces → who consumes + purpose):
  - `order.created` → "Order Service → Inventory Service: triggers stock reservation for new orders"
  - `inventory.events` → "Inventory Service → Order Service: carries Reserved or ReservationFailed results"
  - `payment.events` → "Payment Service → Order Service: carries Completed or Failed payment results"
  - `order.cancelled` → "Order Service → Inventory Service: triggers stock release when payment fails"
  - `catalog.products` → "Catalog Service → Catalog Service: syncs product writes to Elasticsearch"

- [x] **Callout box** — callout-accent: "💡 At-least-once delivery: Kafka guarantees every message is delivered at least once. If the consumer crashes mid-processing, it gets the message again on restart. Services must handle duplicates gracefully — this is called being *idempotent*. The next two modules show exactly how UrbanX achieves this."

- [x] **Quiz** — 2 questions, scenario + architecture style:
  - Q1: "The Inventory Service restarts after a 10-minute outage. What happens to order events that arrived during the outage?" Options: (a) They are lost — Kafka only holds messages for connected consumers, (b) Kafka held them in the topic — Inventory catches up from where it left off ✓, (c) The Order Service automatically retries sending them, (d) The API Gateway queued them. Right: "Correct! Kafka stores messages until each consumer group has confirmed reading them. The consumer group offset tracks exactly where Inventory last read — it resumes from there." Wrong: "Kafka's key superpower is durability. Messages aren't lost when a consumer is offline — they wait in the topic log until the consumer comes back and reads them."
  - Q2: "You want a new analytics service to track every payment without changing the Payment Service at all. How?" Options: (a) Add logging code directly to the Payment Service, (b) Subscribe the analytics service to the payment.events Kafka topic ✓, (c) Poll the Payment Service database every minute, (d) Add a new endpoint to the Payment Service for analytics. Right: "Exactly! This is the beauty of event-driven architecture — you can add new consumers without touching the producer. The Payment Service never needs to know analytics exists." Wrong: "Kafka's pub-sub model means anyone can subscribe to a topic without the producer knowing. No code changes needed in Payment Service — just add a new consumer."

- [x] **Glossary tooltips** — mark on first use:
  - "message broker" → "Software that receives messages from senders (producers) and delivers them to receivers (consumers). Like a postal sorting office — it handles routing so the sender and receiver don't need to be connected at the same time."
  - "topic" → "A named, ordered log of messages in Kafka. Producers write to it; consumers read from it. Like a TV channel — producers broadcast, consumers tune in."
  - "consumer group" → "A set of service instances that share the work of reading from a topic. Each message goes to exactly one member of the group — enabling load balancing."
  - "offset" → "A message's position in a topic. Like a bookmark in a book — Kafka tracks where each consumer group last read, so it can resume after a restart."
  - "at-least-once delivery" → "The guarantee that every message will be delivered at least once. The downside: it might be delivered more than once, so consumers must handle duplicates safely."
  - "idempotent" → "Safe to repeat. An idempotent operation has the same result whether you do it once or ten times. Releasing a stock reservation that is already released does nothing — that is idempotent."
  - "pub-sub" → "Publish-Subscribe: a messaging pattern where publishers send to a topic without knowing who reads it, and subscribers read without knowing who sent it. Complete decoupling."

### Reference Files to Read
- `references/interactive-elements.md` → "Code ↔ English Translation Blocks", "Group Chat Animation", "Permission/Config Badges", "Callout Boxes", "Multiple-Choice Quizzes", "Glossary Tooltips"
- `references/content-philosophy.md` → full file
- `references/gotchas.md` → full file

### Connections
- **Previous module:** "Seven Specialists" — introduced the services. Now shows HOW they communicate.
- **Next module:** "Never Lose a Message" — goes deeper on the Outbox pattern, which was mentioned here. This module's group chat shows "Outbox entry saved" — the next module explains why that step exists.
- **Tone/style notes:** Module bg: `var(--color-bg)` (odd module). The bulletin-board metaphor should appear in the opening text. Keep reinforcing: "no direct calls between services."
