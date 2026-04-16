# Module 4: Never Lose a Message

### Teaching Arc
- **Metaphor:** A surgeon's checklist. Before surgery, every step is written on a form — even if the power goes out, you know exactly what was done and what comes next. The Outbox table is that checklist: every event is written down before anything is acted on.
- **Opening hook:** "Imagine your order gets saved to the database — and then the server crashes before the message reaches Kafka. The order exists, but Inventory never heard about it. Stock is never reserved. What do you do?"
- **Key insight:** Writing to two different systems (database + Kafka) can never be atomic. The Outbox pattern solves this by writing the event to the *same database* as the order, then publishing from there. One transaction, two guarantees.
- **"Why should I care?":** This pattern is why UrbanX is resilient. When you build event-driven features, the Outbox is what makes them production-safe. Tell your AI "use the Transactional Outbox pattern" and it knows exactly what architecture to generate.

### Code Snippets (pre-extracted)

**Snippet A — Atomic write: order + outbox in same transaction** (Order service Program.cs ~lines 210-230):
```csharp
await db.Orders.AddAsync(order);
await db.OutboxMessages.AddAsync(new OutboxMessage
{
    Id          = Guid.NewGuid(),
    EventType   = "OrderCreated",
    Payload     = JsonSerializer.Serialize(orderEvent),
    CreatedAt   = DateTime.UtcNow,
    ProcessedAt = null,
    RetryCount  = 0
});
await db.SaveChangesAsync();
```

**Snippet B — OutboxRelayService polling loop** (OutboxRelayService.cs ~lines 51-100):
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var pending = await _db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .Take(50)
            .ToListAsync(stoppingToken);

        foreach (var msg in pending)
        {
            try
            {
                await _producer.ProduceAsync(msg.EventType, msg.Payload);
                msg.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                msg.RetryCount++;
            }
        }

        await _db.SaveChangesAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
}
```

**Snippet C — Idempotent consumer** (KafkaOrderCancelledConsumer.cs ~lines 88-101):
```csharp
var reservations = await db.InventoryReservations
    .Where(r => r.OrderId == orderId
             && r.Status  == ReservationStatus.Reserved)
    .ToListAsync();

if (reservations.Count == 0) return;

foreach (var r in reservations)
{
    r.Status = ReservationStatus.Released;
    item.QuantityReserved -= r.Quantity;
}
```

### Interactive Elements

- [x] **Numbered step cards** — "The Dangerous Moment Without Outbox" — show 4 step cards using `.step-cards`:
  1. "Order saved to database ✓" — p: "The Order record is written to PostgreSQL successfully."
  2. "💥 Server crashes here" — p: "A power failure, OOM kill, deployment restart — any crash at this moment is catastrophic."
  3. "Kafka message never sent ✗" — p: "The code that publishes to Kafka never runs."
  4. "Order exists, Inventory never knew ✗" — p: "The order is confirmed but stock is never reserved. The customer expects delivery that will never come."

- [x] **Code ↔ English translation** — Use Snippet A (atomic write). Right translation lines:
  - "Save the new Order entity to the Orders table"
  - "In the same database transaction, save a new OutboxMessage"
  - "ProcessedAt=null means: not yet published to Kafka"
  - "RetryCount=0: no delivery attempts yet"
  - "SaveChangesAsync commits BOTH the order AND the outbox entry atomically — either both succeed or neither does"
  - "If the server crashes after this line: both records exist. The relay will pick up and publish on restart."

- [x] **Code ↔ English translation** — Use Snippet B (relay loop). Right translation lines:
  - "This background service runs forever until the app stops"
  - "Find messages not yet published (ProcessedAt=null) with fewer than 5 failed attempts"
  - "Take up to 50 at a time — process in batches for efficiency"
  - "Publish each one to Kafka using the EventType as the topic name"
  - "Mark as processed by setting ProcessedAt to now"
  - "If publishing fails: increment RetryCount — it will be retried on the next poll"
  - "Wait 5 seconds, then poll again — repeat forever"

- [x] **Callout box** — callout-accent: "💡 At-least-once, handled gracefully: If the relay publishes to Kafka but crashes before marking ProcessedAt, the message will be published again on restart. The consumer receives a duplicate. That is why consumers must be idempotent — Snippet C shows how."

- [x] **Code ↔ English translation** — Use Snippet C (idempotent consumer). Right translation lines:
  - "Find all reservations for this order that are still in Reserved status"
  - "If there are none — they were already released — exit immediately. Do nothing."
  - "This single check makes the whole operation idempotent: processing the same message 10 times has the same result as processing it once."
  - "Only if reservations exist: release each one"

- [x] **Quiz** — 2 questions, debugging + scenario style:
  - Q1: "The OutboxRelayService has been publishing messages. You notice an outbox record with RetryCount=4 and ProcessedAt=null. What does this mean?" Options: (a) The message was successfully published 4 times, (b) Kafka delivery has failed 4 times — one more failure and this message is abandoned ✓, (c) The message is still being processed, (d) The record is being held for audit purposes. Right: "Exactly! RetryCount tracks delivery failures. At 5, the relay stops trying. This record needs investigation — check your Kafka connection or message format." Wrong: "RetryCount counts failures, not successes. ProcessedAt=null means it has never been successfully published. RetryCount=4 means it has failed 4 times and has one attempt left."
  - Q2: "A developer removes the Outbox pattern and instead publishes to Kafka directly after SaveChangesAsync. It works fine in testing. When would this break in production?" Options: (a) It would never break — SaveChangesAsync and ProduceAsync are both reliable, (b) When Kafka is temporarily unavailable, the order is saved but the event is never published ✓, (c) Only if the database is under high load, (d) It would break immediately — direct publishing is not allowed. Right: "Correct! This is exactly the dangerous moment the Outbox pattern prevents. A temporary Kafka hiccup or network glitch between SaveChanges and ProduceAsync silently drops the event — with no retry." Wrong: "The problem is time. Between SaveChangesAsync and ProduceAsync, anything can go wrong: server restart, Kafka timeout, network blip. Without the Outbox, that event is gone forever."

- [x] **Glossary tooltips** — mark on first use:
  - "Transactional Outbox" → "A pattern where you write an event to a table in the same database transaction as your business data — then a background worker publishes it to the message broker. Guarantees events are never lost."
  - "atomic transaction" → "A database operation where everything succeeds or everything fails together — no partial results. Like a bank transfer: both debit and credit happen or neither does."
  - "background service" → "A program that runs continuously in the background, doing work on a schedule or in response to events — without being directly called by a user request."
  - "at-least-once" → "A delivery guarantee: the message will be delivered at least once, but might be delivered more than once. Compare with exactly-once, which is much harder to achieve."
  - "idempotent" → "Safe to repeat. If you run an idempotent operation twice, the second run has no effect. Essential for handling duplicate messages in distributed systems."

### Reference Files to Read
- `references/interactive-elements.md` → "Numbered Step Cards", "Code ↔ English Translation Blocks", "Callout Boxes", "Multiple-Choice Quizzes", "Glossary Tooltips"
- `references/content-philosophy.md` → full file
- `references/gotchas.md` → full file

### Connections
- **Previous module:** "The Messaging Backbone" — introduced Kafka and mentioned the Outbox in passing. Now we explain WHY it exists.
- **Next module:** "The Order Saga" — the Outbox is how all saga events are published reliably. This module proves events can be trusted; next module shows what those events orchestrate.
- **Tone/style notes:** Module bg: `var(--color-bg-warm)` (even module). The dangerous-moment narrative should be visceral. Make the learner feel the risk before showing the solution.
