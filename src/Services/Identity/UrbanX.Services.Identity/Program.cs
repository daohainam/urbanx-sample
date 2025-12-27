using Duende.IdentityServer.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Add IdentityServer
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
})
.AddInMemoryIdentityResources(new IdentityResource[]
{
    new IdentityResources.OpenId(),
    new IdentityResources.Profile(),
    new IdentityResources.Email()
})
.AddInMemoryApiScopes(new ApiScope[]
{
    new ApiScope("catalog.read", "Read access to catalog"),
    new ApiScope("orders.read", "Read access to orders"),
    new ApiScope("orders.write", "Write access to orders"),
    new ApiScope("merchant.manage", "Manage merchant resources")
})
.AddInMemoryClients(new Client[]
{
    new Client
    {
        ClientId = "urbanx-spa",
        ClientName = "UrbanX SPA",
        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = false,
        AllowedScopes = { "openid", "profile", "email", "catalog.read", "orders.read", "orders.write", "merchant.manage" },
        RedirectUris = { "http://localhost:5173/callback", "https://localhost:5173/callback" },
        PostLogoutRedirectUris = { "http://localhost:5173", "https://localhost:5173" },
        AllowedCorsOrigins = { "http://localhost:5173", "https://localhost:5173" },
        RequireConsent = false
    }
})
.AddTestUsers(new List<Duende.IdentityServer.Test.TestUser>
{
    new Duende.IdentityServer.Test.TestUser
    {
        SubjectId = "1",
        Username = "customer@test.com",
        Password = "Password123!",
        Claims = new[]
        {
            new System.Security.Claims.Claim("name", "Test Customer"),
            new System.Security.Claims.Claim("email", "customer@test.com"),
            new System.Security.Claims.Claim("role", "customer")
        }
    },
    new Duende.IdentityServer.Test.TestUser
    {
        SubjectId = "2",
        Username = "merchant@test.com",
        Password = "Password123!",
        Claims = new[]
        {
            new System.Security.Claims.Claim("name", "Test Merchant"),
            new System.Security.Claims.Claim("email", "merchant@test.com"),
            new System.Security.Claims.Claim("role", "merchant")
        }
    }
});

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseIdentityServer();

app.Run();
