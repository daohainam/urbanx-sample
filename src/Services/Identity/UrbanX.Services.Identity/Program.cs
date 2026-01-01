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
    new("catalog.read", "Read access to catalog"),
    new("orders.read", "Read access to orders"),
    new("orders.write", "Write access to orders"),
    new("merchant.manage", "Manage merchant resources")
})
.AddInMemoryClients(new Client[]
{
    new() {
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
    },
    new() {
        ClientId = "urbanx-merchant-spa",
        ClientName = "UrbanX Merchant Portal (React)",
        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = false,
        AllowedScopes = { "openid", "profile", "email", "merchant.manage" },
        RedirectUris = { "http://localhost:5174/callback", "https://localhost:5174/callback" },
        PostLogoutRedirectUris = { "http://localhost:5174", "https://localhost:5174" },
        AllowedCorsOrigins = { "http://localhost:5174", "https://localhost:5174" },
        RequireConsent = false
    },
    new() {
        ClientId = "urbanx-merchant-blazor",
        ClientName = "UrbanX Merchant Admin (Blazor)",
        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = false,
        AllowedScopes = { "openid", "profile", "email", "merchant.manage" },
        RedirectUris = { 
            "http://localhost:5175/authentication/login-callback", 
            "https://localhost:5175/authentication/login-callback" 
        },
        PostLogoutRedirectUris = { 
            "http://localhost:5175/authentication/logout-callback", 
            "https://localhost:5175/authentication/logout-callback" 
        },
        AllowedCorsOrigins = { "http://localhost:5175", "https://localhost:5175" },
        RequireConsent = false
    }
})
.AddTestUsers(new List<Duende.IdentityServer.Test.TestUser>
{
    new() {
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
    new() {
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
