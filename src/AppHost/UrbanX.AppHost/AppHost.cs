var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var catalogDb = postgres.AddDatabase("catalogdb", "urbanx_catalog");
var orderDb = postgres.AddDatabase("orderdb", "urbanx_order");
var merchantDb = postgres.AddDatabase("merchantdb", "urbanx_merchant");
var paymentDb = postgres.AddDatabase("paymentdb", "urbanx_payment");
var inventoryDb = postgres.AddDatabase("inventorydb", "urbanx_inventory");

// Add Kafka
var kafka = builder.AddKafka("kafka");

// Add Services
var identityService = builder.AddProject<Projects.UrbanX_Services_Identity>("identity");

var catalogService = builder.AddProject<Projects.UrbanX_Services_Catalog>("catalog")
    .WithReference(catalogDb)
    .WithReference(kafka)
    .WithReference(identityService);

var orderService = builder.AddProject<Projects.UrbanX_Services_Order>("order")
    .WithReference(orderDb)
    .WithReference(kafka)
    .WithReference(identityService);

var merchantService = builder.AddProject<Projects.UrbanX_Services_Merchant>("merchant")
    .WithReference(merchantDb)
    .WithReference(identityService);

var paymentService = builder.AddProject<Projects.UrbanX_Services_Payment>("payment")
    .WithReference(paymentDb)
    .WithReference(identityService);

var inventoryService = builder.AddProject<Projects.UrbanX_Services_Inventory>("inventory")
    .WithReference(inventoryDb)
    .WithReference(kafka);

// Add Gateway with references to all services
var gateway = builder.AddProject<Projects.UrbanX_Gateway>("gateway")
    .WithReference(catalogService)
    .WithReference(orderService)
    .WithReference(merchantService)
    .WithReference(paymentService)
    .WithReference(inventoryService)
    .WithReference(identityService);

var frontend = builder.AddViteApp("frontend", "../../frontend/urbanx-react")
    .WithReference(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();
