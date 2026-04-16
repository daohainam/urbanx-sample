# Module 1: The Shopping Journey

### Teaching Arc
- **Metaphor:** A behind-the-scenes documentary of a movie you already loved — you know the ending (the order arrives), now see the machinery that made it happen.
- **Opening hook:** "You've placed orders before. You clicked 'Buy', got a confirmation, and a package showed up. Here's what actually happened in those few seconds."
- **Key insight:** Placing one order silently triggers 6 different programs working together, each expert at its one job.
- **"Why should I care?":** When you tell an AI to "add a checkout feature," you now know it touches the Order Service, kicks off an inventory check, and triggers a payment flow — so you can describe *exactly* what you want built.

### Code Snippets (pre-extracted)

No code translation needed in this module — it is visual/conceptual. Use flow animations and pattern cards instead.

### Interactive Elements

- [x] **Data flow animation** — actors: Browser, API Gateway, Order Service, Kafka, Inventory Service, Payment Service. Steps (use exact IDs: flow-actor-1 through flow-actor-6):
  1. highlight flow-actor-1 (Browser): "You click 'Place Order' in the React app — your browser sends POST /api/orders to port 5000."
  2. packet from actor-1 to actor-2 + highlight flow-actor-2 (Gateway): "The API Gateway receives it, checks your JWT login token, and routes it to the Order Service at port 5002."
  3. highlight flow-actor-3 (Order Service): "Order Service saves a new Order (status: Pending) to its PostgreSQL database. At the same time, it writes an OrderCreated event to its Outbox table — same transaction, never lost."
  4. packet from actor-3 to actor-4 + highlight flow-actor-4 (Kafka): "The Outbox Relay worker picks up the event and publishes it to the order.created Kafka topic within 5 seconds."
  5. packet from actor-4 to actor-5 + highlight flow-actor-5 (Inventory): "Inventory Service reads the event and reserves the stock. Payment Service waits for you to submit payment."
  6. packet from actor-5 to actor-3 + highlight flow-actor-3 (Order Service): "Order status updates to PaymentReceived. The saga is complete. Your order is confirmed."

- [x] **Pattern cards** — 4 cards introducing the 4 big patterns this course covers. Use `.pattern-cards` grid with stagger animation. Cards:
  1. "Transactional Outbox" — icon 📬 — "Events are saved to the database before being sent, so they're never lost even if the server crashes."
  2. "Choreography Saga" — icon 🎭 — "Multiple services coordinate without a conductor — each reacts to events and passes the baton."
  3. "CQRS" — icon ✂️ — "Reading and writing use separate databases optimized for each job."
  4. "Event-Driven Architecture" — icon 📡 — "Services talk by broadcasting events, not by calling each other directly."

- [x] **Quiz** — 2 questions, tracing style:
  - Q1: "You add a product to your cart. Which service handles that request?" Options: (a) Catalog Service, (b) Order Service ✓, (c) Payment Service, (d) Identity Service. Right: "Exactly! The cart lives in the Order Service — it manages both carts and orders because they share the same domain." Wrong: "Not quite — the Catalog Service handles product search and listings, but the shopping cart belongs to the Order Service."
  - Q2: "A merchant updates a product name. Which database gets updated first?" Options: (a) Elasticsearch, (b) Both at exactly the same time, (c) PostgreSQL first, then Elasticsearch via Kafka ✓, (d) The API Gateway cache. Right: "Correct! PostgreSQL is always the source of truth. Elasticsearch is updated a few seconds later via Kafka — this is called eventual consistency." Wrong: "PostgreSQL is the write database — it always gets changes first. Elasticsearch catches up via Kafka events."

- [x] **Glossary tooltips** — mark these terms on first use:
  - "microservices" → "Small, independent programs that each do one job. Instead of one big app that does everything, you have specialists that work together."
  - "API Gateway" → "The single front door to your whole system. Every request goes through here first, so you can add security, routing, and rate limiting in one place."
  - "Kafka" → "A message broker — like a postal service for software. Services publish messages to named channels (topics), and other services subscribe to read them."
  - "JWT token" → "JSON Web Token — a cryptographically signed credential that proves who you are and what you're allowed to do. Like a wristband at a concert."
  - "PostgreSQL" → "A powerful relational database — like a very organized spreadsheet system. Great for structured data that needs to stay consistent."
  - "Outbox table" → "A special database table that stores events-to-be-sent alongside business data in the same transaction — guaranteeing they're never lost."
  - "Elasticsearch" → "A database built for search. While PostgreSQL stores data accurately, Elasticsearch finds it fast — even with typos or partial words."

### Reference Files to Read
- `references/interactive-elements.md` → "Message Flow / Data Flow Animation", "Pattern/Feature Cards", "Multiple-Choice Quizzes", "Glossary Tooltips", "Numbered Step Cards"
- `references/content-philosophy.md` → full file
- `references/gotchas.md` → full file

### Connections
- **Previous module:** None — this is the opening. Start wide with the product experience.
- **Next module:** "Seven Specialists" — goes deeper on each service. This module sets up the question "who does what?" that the next one answers.
- **Tone/style notes:** Warm, excited tone. This is the "hook" module — make the learner feel clever for understanding complex architecture. Accent color: vermillion `#D94F30`. All actor colors: Browser=actor-1 (vermillion), Gateway=actor-2 (teal), Order=actor-3 (plum), Kafka=actor-4 (golden), Inventory=actor-5 (forest), Payment=actor-6 (use inline style `#2A7B9B`).
