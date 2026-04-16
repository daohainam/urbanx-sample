# Module 6: Two Databases, One Truth

### Teaching Arc
- **Metaphor:** A library with two card catalogs. The acquisitions desk uses a detailed ledger (PostgreSQL) that tracks every book accurately — author, ISBN, condition, purchase date. The public search terminal (Elasticsearch) uses a fast index built for "find me books about dragons" — it doesn't need every detail, just enough to find things quickly. When a new book arrives, the ledger is updated first, then the index is refreshed.
- **Opening hook:** "PostgreSQL is brilliant at storing products accurately. But a full-text search for 'wireless headphones' across 10,000 products? That's not what relational databases are built for. Elasticsearch is. So UrbanX uses both — each for what it does best."
- **Key insight:** CQRS separates the write model (accuracy, consistency) from the read model (speed, search). The Kafka pipeline keeps them in sync. You trade instant consistency for powerful querying.
- **"Why should I care?":** The next time an AI suggests "just add a search field to your PostgreSQL query," you can say: "Actually, let's use Elasticsearch for search and keep writes in the relational database — use CQRS." That's senior-engineer thinking.

### Code Snippets (pre-extracted)

**Snippet A — Elasticsearch full-text search** (ElasticsearchProductSearchService.cs):
```csharp
var response = await _client.SearchAsync<ProductDocument>(s => s
    .Index("catalog-products")
    .Query(q => q
        .Bool(b => b
            .Must(m => m
                .MultiMatch(mm => mm
                    .Fields(f => f
                        .Field(p => p.Name)
                        .Field(p => p.Description))
                    .Query(searchTerm)))
            .Filter(f => f
                .Term(t => t
                    .Field(p => p.IsActive)
                    .Value(true)))))
    .From((page - 1) * pageSize)
    .Size(pageSize));
```

**Snippet B — Product write triggers Outbox** (Catalog service Program.cs ~lines 180-210):
```csharp
app.MapPut("/api/products/{id}", async (Guid id, UpdateProductRequest req, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name        = req.Name;
    product.Description = req.Description;
    product.Price       = req.Price;
    product.UpdatedAt   = DateTime.UtcNow;

    await db.OutboxMessages.AddAsync(new OutboxMessage
    {
        EventType = "ProductUpdated",
        Payload   = JsonSerializer.Serialize(new ProductEvent(product))
    });

    await db.SaveChangesAsync();
    return Results.Ok(product);
});
```

**Snippet C — Kafka consumer updates Elasticsearch** (KafkaProductEventConsumer.cs ~lines 40-70):
```csharp
var productEvent = JsonSerializer.Deserialize<ProductEvent>(result.Message.Value);

var document = new ProductDocument
{
    Id          = productEvent.Id,
    Name        = productEvent.Name,
    Description = productEvent.Description,
    Price       = productEvent.Price,
    Category    = productEvent.Category,
    IsActive    = productEvent.IsActive
};

await _searchService.IndexAsync(document);
consumer.Commit(result);
```

### Interactive Elements

- [x] **Data flow animation** — id: `flow-cqrs`. Show the full write→sync path. Actors (flow-actor-1 through flow-actor-5): Merchant Browser (actor-1), PostgreSQL (actor-2, use `#336791` bg), Outbox/Kafka (actor-4), Elasticsearch (actor-3, use `#F04E98` bg), Customer Browser (actor-5). Steps:
  1. highlight flow-actor-1: "A merchant edits a product name via PUT /api/products/{id}."
  2. packet from actor-1 to actor-2 + highlight flow-actor-2: "PostgreSQL is updated atomically. Product.Name = new value. OutboxMessage also saved in same transaction."
  3. packet from actor-2 to actor-3 + highlight flow-actor-3: "Outbox Relay publishes ProductUpdated to the catalog.products Kafka topic within ~5 seconds."
  4. packet from actor-3 to actor-4 + highlight flow-actor-4: "The Catalog Service Kafka consumer receives the event and calls Elasticsearch.IndexAsync() to update the search document."
  5. packet from actor-4 to actor-5 + highlight flow-actor-5: "Customer searches for the product — Elasticsearch returns the updated name. Total lag: under 10 seconds. This is eventual consistency."

- [x] **Code ↔ English translation** — Use Snippet A (Elasticsearch query). Right translation lines:
  - "Search the catalog-products Elasticsearch index (not the PostgreSQL database)"
  - "Use a Bool query — like combining conditions in a SQL WHERE clause"
  - "Must: the search term must appear somewhere in this product"
  - "MultiMatch: look in BOTH the name field AND the description — find partial matches too"
  - "Filter: only show products where IsActive=true — hidden products never appear in search"
  - "From/Size: pagination — skip the first N results, return the next batch"

- [x] **Code ↔ English translation** — Use Snippet B (product write). Right translation lines:
  - "Update the product fields in PostgreSQL"
  - "In the same transaction, write a ProductUpdated event to the Outbox"
  - "SaveChangesAsync commits both changes atomically"
  - "The Outbox Relay will pick this up and publish to catalog.products within 5 seconds"
  - "Note: Elasticsearch is NOT updated here — it's updated asynchronously via Kafka"

- [x] **Callout box** — callout-info: "ℹ️ The trade-off you accept with CQRS: A product updated right now might show the old name in search results for up to ~10 seconds. Engineers call this *eventual consistency*. For a product catalog, this is completely fine. For a bank balance, it would not be. Choosing CQRS means accepting this trade-off consciously — and it is almost always the right call for search features."

- [x] **Callout box** (second one) — callout-accent: "💡 The same pattern, used twice: UrbanX uses Kafka for two completely different purposes: the Order Saga (coordinating services) and CQRS (syncing databases). Same infrastructure, two distinct use cases. This is why Kafka is described as the 'backbone' of the architecture."

- [x] **Quiz** — 2 questions, scenario + architecture decision style:
  - Q1: "A customer searches for 'bluetooth speaker' and sees a product with an old price ($49) even though a merchant updated it to $59 three seconds ago. Is this a bug?" Options: (a) Yes — data should always be consistent, this is a critical bug, (b) No — this is expected eventual consistency and will resolve in a few seconds ✓, (c) Yes — the Outbox pattern should prevent this, (d) No — search results always show cached data permanently. Right: "Correct! This is expected behavior in a CQRS system. The Elasticsearch index lags the PostgreSQL database by a few seconds. For a product catalog, this is an acceptable trade-off. The price will update within ~10 seconds." Wrong: "This is intentional by design. CQRS accepts a brief window where read and write models are out of sync — called eventual consistency. For search features, a few seconds of lag is a worthwhile trade-off for Elasticsearch-powered search."
  - Q2: "You want to add a feature: show the 'lowest price in each category' on the homepage. Which approach is better?" Options: (a) Query PostgreSQL with GROUP BY on every page load, (b) Pre-compute and store the lowest price in Elasticsearch when products are updated ✓, (c) Calculate it in the React frontend from all product data, (d) Add a new microservice that queries every 30 seconds. Right: "Exactly! CQRS thinking: read-optimized data belongs in the read model. Pre-compute the aggregation when data changes, store it in Elasticsearch (or a read-optimized store), serve it instantly on every page load." Wrong: "Think CQRS: heavy read operations don't belong in PostgreSQL queries on every request. The read model (Elasticsearch) should be shaped for what consumers need — pre-computed aggregations updated via Kafka events."

- [x] **Glossary tooltips** — mark on first use:
  - "CQRS" → "Command Query Responsibility Segregation — a pattern where the database used for writing (commands) is separate from the database used for reading (queries). Each is optimized for its job."
  - "eventual consistency" → "A guarantee that data across multiple stores will eventually agree, but there may be a brief window where they differ. Acceptable for most features; unacceptable for financial transactions."
  - "read model" → "A database or data structure optimized for reading — often denormalized and pre-computed for specific queries. In UrbanX, Elasticsearch is the read model for the product catalog."
  - "write model" → "The authoritative database that handles all data mutations. In UrbanX, PostgreSQL is the write model — the source of truth."
  - "full-text search" → "Finding documents that contain words or phrases, even with typos or partial matches. Google uses it. Elasticsearch is built for it. PostgreSQL LIKE queries are not."
  - "index (Elasticsearch)" → "A named collection of search documents in Elasticsearch — equivalent to a table in a relational database, but optimized for search queries."
  - "denormalized" → "Data stored in a form optimized for reading, with duplication accepted. The opposite of the normalized database design taught in SQL courses. Read models are usually denormalized."

### Reference Files to Read
- `references/interactive-elements.md` → "Message Flow / Data Flow Animation", "Code ↔ English Translation Blocks", "Callout Boxes", "Multiple-Choice Quizzes", "Glossary Tooltips"
- `references/content-philosophy.md` → full file
- `references/gotchas.md` → full file

### Connections
- **Previous module:** "The Order Saga" — used Kafka for service coordination. This module shows Kafka used for a completely different purpose: database sync.
- **Next module:** None — this is the final module. End with a summary of all 4 patterns and an invitation to explore the codebase.
- **Tone/style notes:** Module bg: `var(--color-bg-warm)` (even module). End the module with a "you made it" congratulations block and a summary of all patterns covered. Include a link to the GitHub repo.
