using Duende.IdentityServer.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Add IdentityServer
var identityServerBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    // Only log information and success events in development
    options.Events.RaiseInformationEvents = builder.Environment.IsDevelopment();
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = builder.Environment.IsDevelopment();
    
    // Use persistent key storage in production
    options.KeyManagement.Enabled = true;
})
.AddInMemoryIdentityResources(
[
    new IdentityResources.OpenId(),
    new IdentityResources.Profile(),
    new IdentityResources.Email()
])
.AddInMemoryApiScopes(
[
    new("catalog.read", "Read access to catalog"),
    new("orders.read", "Read access to orders"),
    new("orders.write", "Write access to orders"),
    new("merchant.manage", "Manage merchant resources")
])
.AddInMemoryClients(
[
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
        // Enable consent in production
        RequireConsent = !builder.Environment.IsDevelopment()
    },
    new() {
        ClientId = "urbanx-merchant-spa",
        ClientName = "UrbanX Merchant Portal",
        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = false,
        AllowedScopes = { "openid", "profile", "email", "merchant.manage" },
        RedirectUris = { "http://localhost:5174/callback", "https://localhost:5174/callback" },
        PostLogoutRedirectUris = { "http://localhost:5174", "https://localhost:5174" },
        AllowedCorsOrigins = { "http://localhost:5174", "https://localhost:5174" },
        // Enable consent in production
        RequireConsent = !builder.Environment.IsDevelopment()
    }
]);

// WARNING: Test users are ONLY for development and MUST be replaced in production
// with proper user storage using ASP.NET Identity + Entity Framework Core
// See SECURITY.md for implementation guidance
if (builder.Environment.IsDevelopment())
{
    identityServerBuilder.AddTestUsers(new List<Duende.IdentityServer.Test.TestUser>
    {
        new() {
            SubjectId = "1",
            Username = "customer@test.com",
            Password = "Password123!",
            Claims =
            [
                new System.Security.Claims.Claim("name", "Test Customer"),
                new System.Security.Claims.Claim("email", "customer@test.com"),
                new System.Security.Claims.Claim("role", "customer")
            ]
        },
        new() {
            SubjectId = "2",
            Username = "merchant@test.com",
            Password = "Password123!",
            Claims =
            [
                new System.Security.Claims.Claim("name", "Test Merchant"),
                new System.Security.Claims.Claim("email", "merchant@test.com"),
                new System.Security.Claims.Claim("role", "merchant")
            ]
        }
    });
}
else
{
    // TODO: In production, implement proper user management
    // Replace with: identityServerBuilder.AddAspNetIdentity<ApplicationUser>();
    // See SECURITY.md for detailed implementation guidance
    throw new InvalidOperationException(
        "Production identity management not configured. " +
        "Test users are disabled in production for security. " +
        "Please implement ASP.NET Identity with Entity Framework Core. " +
        "See SECURITY.md for guidance.");
}

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
