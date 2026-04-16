# Module 2: Seven Specialists

### Teaching Arc
- **Metaphor:** A hospital. One doctor who does everything would be exhausted and error-prone. Instead you have specialists: the cardiologist doesn't touch ophthalmology. In UrbanX, each service is a specialist that only knows its domain.
- **Opening hook:** "UrbanX isn't one big app. It's seven small programs. If Payment crashes, your cart still works. If Catalog is overloaded with searches, orders keep processing. That's the power of specialization."
- **Key insight:** Each service owns its own database. No service can reach into another's data. They only communicate via Kafka events or the API they expose.
- **"Why should I care?":** When you ask an AI to "add a feature that shows inventory count on the product page," you now know that means the Catalog Service needs to call the Inventory Service — and you can tell the AI *exactly* that.

### Code Snippets (pre-extracted)

**Snippet A — Gateway YARP routing config** (appsettings.json, simplified):
```json
"Routes": {
  "catalog-route": {
    "ClusterId": "catalog",
    "Match": { "Path": "/api/products/{**catch-all}" }
  },
  "order-route": {
    "ClusterId": "order",
    "Match": { "Path": "/api/orders/{**catch-all}" }
  },
  "payment-route": {
    "ClusterId": "payment",
    "Match": { "Path": "/api/payments/{**catch-all}" }
  },
  "identity-route": {
    "ClusterId": "identity",
    "Match": { "Path": "/connect/{**catch-all}" }
  }
}
```

**Snippet B — JWT bearer validation** (any service Program.cs):
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity:5005";
        options.Audience = "urbanx-api";
        options.RequireHttpsMetadata = false;
    });
```

**Snippet C — Authorization policies** (Order service Program.cs):
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly",
        p => p.RequireRole("customer"));
    options.AddPolicy("MerchantOnly",
        p => p.RequireRole("merchant"));
    options.AddPolicy("CustomerOrMerchant",
        p => p.RequireRole("customer", "merchant"));
});
```

### Interactive Elements

- [x] **Code ↔ English translation** — Use Snippet A (Gateway routing). Left: the JSON config. Right translation lines:
  - "Any URL starting with /api/products goes to the Catalog Service"
  - "Anything under /api/orders or /api/cart goes to the Order Service"  
  - "Payment requests go to the Payment Service"
  - "The /connect/ path is for login — it goes to the Identity Service (Duende IdentityServer)"
  - "The Gateway itself never processes business logic — it just forwards"

- [x] **Code ↔ English translation** — Use Snippet B (JWT validation). Right translation lines:
  - "Every service validates tokens by checking with the Identity Service"
  - "Authority: the Identity Service URL — go here to verify token signatures"
  - "Audience: only accept tokens issued for this specific API"
  - "RequireHttpsMetadata=false: development only — in production, HTTPS would be required"

- [x] **Group chat animation** — id: `chat-services`. Actors and their colors: Identity (background `var(--color-actor-1)`), Order (background `var(--color-actor-2)`), Inventory (background `var(--color-actor-3)`), Payment (background `var(--color-actor-4)`), Catalog (background `var(--color-actor-5)`). Show them introducing themselves. Messages:
  - msg 0, sender: identity — "I handle logins. Give me a username and password, I'll give you a JWT token valid for 60 minutes."
  - msg 1, sender: catalog — "I manage the product catalog. Writes go to PostgreSQL, searches go to Elasticsearch. I keep them in sync."
  - msg 2, sender: order — "Shopping carts and orders live with me. I also run the Saga — coordinating inventory and payment."
  - msg 3, sender: inventory — "I track stock. When Order tells me a new order was placed, I reserve the items. If payment fails, I release them."
  - msg 4, sender: payment — "I talk to Stripe. I never share those credentials with anyone else. My job is to charge and report results."
  - msg 5, sender: identity — "And none of us call each other directly. We talk through Kafka — that's next module."

- [x] **Architecture diagram** — use `.arch-diagram` with two `.arch-zone` sections: "Frontend & Gateway" and "Backend Services". Components: Browser, API Gateway (port 5000), Identity (5005), Catalog (5001), Order (5002), Merchant (5003), Payment (5004), Inventory (dynamic). Each component has `data-desc` explaining what it does when clicked.

- [x] **Quiz** — 2 questions, architecture decisions style:
  - Q1: "You want to add a 'Favorite Products' feature where customers save products. Which service should own this data?" Options: (a) The API Gateway — it already sees all requests, (b) The Catalog Service — it owns products ✓, (c) The Identity Service — it already knows who the user is, (d) A new 'Favorites' microservice. Right: "Exactly! Favorites are about products, so they belong with the Catalog Service — the specialist that owns the product domain." Wrong: "Think about which service owns the related data. Favorites are about *products*, so the product specialist — Catalog — is the right home."
  - Q2: "The Identity Service goes down for 5 minutes. Which of these still works?" Options: (a) Nothing works — all services need auth, (b) Existing logged-in users can still browse products and place orders ✓, (c) New users can log in but existing ones cannot, (d) Only the API Gateway keeps working. Right: "Correct! Services validate JWT tokens themselves using a cached public key — they don't call Identity on every request. Already-logged-in users have valid tokens and can keep using the app." Wrong: "Here's the key: services cache the Identity public key and validate tokens locally. They only need Identity when users log in fresh."

- [x] **Glossary tooltips** — mark on first use:
  - "YARP" → "Yet Another Reverse Proxy — a Microsoft library that turns an ASP.NET app into a configurable API gateway with routing, load balancing, and rate limiting."
  - "rate limiting" → "Capping how many requests a client can make in a time window. Like a nightclub with a 100-person capacity — once full, new arrivals wait outside."
  - "OIDC" → "OpenID Connect — a login standard built on top of OAuth 2.0. It's the technology behind 'Sign in with Google' buttons."
  - "Duende IdentityServer" → "A .NET library that implements a full OAuth 2.0 / OpenID Connect server — the software that issues and validates login tokens."
  - "JWT" → "JSON Web Token — a compact, cryptographically signed credential. Contains your user ID, role, and expiry time. Like a tamper-proof wristband."
  - "authorization policy" → "A named rule that defines who can access an endpoint. CustomerOnly means only users with the customer role can call this API."
  - "reverse proxy" → "A server that sits in front of your real servers and forwards requests to them — invisible to the caller but essential for routing, security, and load balancing."

### Reference Files to Read
- `references/interactive-elements.md` → "Code ↔ English Translation Blocks", "Group Chat Animation", "Interactive Architecture Diagram", "Multiple-Choice Quizzes", "Glossary Tooltips"
- `references/content-philosophy.md` → full file
- `references/gotchas.md` → full file

### Connections
- **Previous module:** "The Shopping Journey" — established the user flow and big-picture architecture overview.
- **Next module:** "The Messaging Backbone" — dives into how services talk via Kafka. This module's group chat teases that — services say "we talk through Kafka."
- **Tone/style notes:** Still accessible. Emphasize that specialization is a decision — a vibe coder should be able to make this decision. Module bg: `var(--color-bg-warm)` (even module).
