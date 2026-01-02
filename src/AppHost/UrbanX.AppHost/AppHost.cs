var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var catalogDb = postgres.AddDatabase("catalogdb", "urbanx_catalog");
var orderDb = postgres.AddDatabase("orderdb", "urbanx_order");
var merchantDb = postgres.AddDatabase("merchantdb", "urbanx_merchant");
var paymentDb = postgres.AddDatabase("paymentdb", "urbanx_payment");

// Add Kafka
var kafka = builder.AddKafka("kafka");

// Add Services
var catalogService = builder.AddProject<Projects.UrbanX_Services_Catalog>("catalog")
    .WithReference(catalogDb)
    .WithReference(kafka);

var orderService = builder.AddProject<Projects.UrbanX_Services_Order>("order")
    .WithReference(orderDb)
    .WithReference(kafka);

var merchantService = builder.AddProject<Projects.UrbanX_Services_Merchant>("merchant")
    .WithReference(merchantDb);

var paymentService = builder.AddProject<Projects.UrbanX_Services_Payment>("payment")
    .WithReference(paymentDb);

var identityService = builder.AddProject<Projects.UrbanX_Services_Identity>("identity");

// Add Gateway with references to all services
var gateway = builder.AddProject<Projects.UrbanX_Gateway>("gateway")
    .WithReference(catalogService)
    .WithReference(orderService)
    .WithReference(merchantService)
    .WithReference(paymentService)
    .WithReference(identityService);

// Add Blazor Frontend
var frontend = builder.AddProject<Projects.UrbanX_Frontend>("frontend")
    .WithReference(gateway)
    .WithExternalHttpEndpoints();

// Add Admin Frontend
var admin = builder.AddProject<Projects.UrbanX_Admin>("admin")
    .WithReference(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();
