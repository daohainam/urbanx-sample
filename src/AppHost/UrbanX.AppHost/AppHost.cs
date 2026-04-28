var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithPgWeb();

var catalogDb = postgres.AddDatabase("catalogdb", "urbanx_catalog");
var orderDb = postgres.AddDatabase("orderdb", "urbanx_order");
var merchantDb = postgres.AddDatabase("merchantdb", "urbanx_merchant");
var paymentDb = postgres.AddDatabase("paymentdb", "urbanx_payment");
var inventoryDb = postgres.AddDatabase("inventorydb", "urbanx_inventory");
var identityDb = postgres.AddDatabase("identitydb", "urbanx_identity");

// Add Kafka
var kafka = builder.AddKafka("kafka");

// Add Services
var identityService = builder.AddProject<Projects.UrbanX_Services_Identity>("identity")
    .WithReference(identityDb)
    .WaitFor(identityDb);

var catalogService = builder.AddProject<Projects.UrbanX_Services_Catalog>("catalog")
    .WithReference(catalogDb)
    .WithReference(kafka)
    .WithReference(identityService)
    .WaitFor(catalogDb)
    .WaitFor(kafka)
    .WaitFor(identityService);

var orderService = builder.AddProject<Projects.UrbanX_Services_Order>("order")
    .WithReference(orderDb)
    .WithReference(kafka)
    .WithReference(identityService)
    .WaitFor(orderDb)
    .WaitFor(kafka)
    .WaitFor(identityService);

var merchantService = builder.AddProject<Projects.UrbanX_Services_Merchant>("merchant")
    .WithReference(merchantDb)
    .WithReference(identityService)
    .WaitFor(merchantDb)
    .WaitFor(identityService);

var paymentService = builder.AddProject<Projects.UrbanX_Services_Payment>("payment")
    .WithReference(paymentDb)
    .WithReference(kafka)
    .WithReference(identityService)
    .WaitFor(paymentDb)
    .WaitFor(kafka)
    .WaitFor(identityService);

var inventoryService = builder.AddProject<Projects.UrbanX_Services_Inventory>("inventory")
    .WithReference(inventoryDb)
    .WithReference(kafka)
    .WithReference(identityService)
    .WaitFor(inventoryDb)
    .WaitFor(kafka)
    .WaitFor(identityService);

// Add Gateway with references to all services
var gateway = builder.AddProject<Projects.UrbanX_Gateway>("gateway")
    .WithReference(catalogService)
    .WithReference(orderService)
    .WithReference(merchantService)
    .WithReference(paymentService)
    .WithReference(inventoryService)
    .WithReference(identityService)
    .WaitFor(catalogService)
    .WaitFor(orderService)
    .WaitFor(merchantService)
    .WaitFor(paymentService)
    .WaitFor(inventoryService)
    .WaitFor(identityService);

var frontend = builder.AddViteApp("frontend", "../../frontend/urbanx-react")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

// Management portal (Blazor Server admin app). The OIDC client secret defaults
// to a dev value; override via the ADMIN_OIDC_CLIENT_SECRET environment variable
// (also propagated to the Identity service so both ends agree).
var adminClientSecret = builder.Configuration["ADMIN_OIDC_CLIENT_SECRET"] ?? "dev-admin-secret-change-me";

var management = builder.AddProject<Projects.UrbanX_Management_Web>("management")
    .WithReference(catalogService)
    .WithReference(identityService)
    .WaitFor(catalogService)
    .WaitFor(identityService)
    .WithEnvironment("Authentication__Oidc__ClientSecret", adminClientSecret)
    // Pin to localhost:5006 (matches the redirect URIs registered for the
    // urbanx-admin OIDC client). isProxied=false bypasses Aspire's reverse
    // proxy so the browser sees exactly this URL.
    //.WithHttpEndpoint(port: 5006, name: "http", isProxied: false)
    .WithExternalHttpEndpoints();

identityService.WithEnvironment("IdentityServer__Clients__UrbanXAdmin__ClientSecret", adminClientSecret);

builder.Build().Run();
