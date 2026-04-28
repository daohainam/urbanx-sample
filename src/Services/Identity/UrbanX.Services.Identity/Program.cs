// ============================================================================
// UrbanX Identity Service – Program.cs
// ============================================================================
// This service is the central authentication and authorisation authority for
// the entire UrbanX platform. It implements:
//
//   • OpenID Connect (OIDC) – identity layer on top of OAuth2, provides ID tokens
//     and a /connect/userinfo endpoint with standard claims (sub, name, email…).
//   • OAuth2 – authorisation framework; issues access tokens that protect the
//     downstream microservices (Order, Catalog, Payment, Merchant, Inventory).
//   • ASP.NET Core Identity – production user store backed by PostgreSQL via EF Core.
//     Handles password hashing (PBKDF2-SHA256), account lockout, email confirmation,
//     and role management.
//   • Duende IdentityServer – the OIDC/OAuth2 protocol engine layered on top of
//     ASP.NET Identity. Provides the discovery document, JWKS, token issuance,
//     introspection, revocation and session management endpoints.
//
// ──────────────────────────────────────────────────────────────────────────────
// Key OIDC / OAuth2 concepts used in this service
// ──────────────────────────────────────────────────────────────────────────────
// Authorization Code + PKCE (Proof Key for Code Exchange)
//   The SPA applications (urbanx-spa, urbanx-merchant-spa) use this grant type.
//   PKCE prevents authorisation code interception attacks that are a concern for
//   public clients (browser or native apps that cannot keep a client secret).
//   Flow:
//     1. SPA generates code_verifier (random) and code_challenge = SHA256(code_verifier).
//     2. SPA → GET /connect/authorize?response_type=code&client_id=…&code_challenge=…
//     3. IdentityServer redirects user to /Account/Login if not authenticated.
//     4. User authenticates; IdentityServer issues authorisation code.
//     5. SPA → POST /connect/token with code + code_verifier; gets id_token + access_token.
//
// Client Credentials
//   For machine-to-machine communication between services (no user involved).
//   Currently no internal service clients are defined, but the pattern is supported.
//
// Scopes
//   OAuth2 scopes partition the API surface. Each downstream service validates that the
//   access token contains the required scope before serving the request.
//     catalog.read    – read product catalogue
//     orders.read     – read orders
//     orders.write    – create / update orders
//     merchant.manage – manage merchant resources
//
// Tokens
//   ID Token   – a JWT for the SPA that proves the user is authenticated.
//               Contains: sub (user id), name, email, role.
//   Access Token – a JWT (or reference token) passed as Bearer in API requests.
//               Contains: sub, scope, aud (audience), exp, iat.
//   Refresh Token – optional long-lived token to obtain new access tokens without
//               re-authentication. Not currently enabled on the public SPA clients
//               to limit attack surface.
//
// ============================================================================

using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Identity;
using UrbanX.Services.Identity.Data;
using UrbanX.Services.Identity.Models;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire service defaults (OpenTelemetry, health checks, service discovery) ─
builder.AddServiceDefaults();

// ── Open API (Scalar UI) – development only ───────────────────────────────────
builder.Services.AddOpenApi();

// ── Razor Pages – required for the interactive login / logout UI ──────────────
// OAuth2 / OIDC Authorization Code flow requires a browser-facing login page.
// Razor Pages (not MVC controllers) are the recommended approach for
// IdentityServer UI pages because they keep each page self-contained.
builder.Services.AddRazorPages();

// ── PostgreSQL database via Aspire-managed connection string ──────────────────
// The connection string is injected by .NET Aspire as "identitydb".
// Aspire also handles connection resiliency and OpenTelemetry tracing for EF Core.
builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

// Add a readiness health check on the database connection
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "identitydb", tags: ["ready", "db"]);

// ── ASP.NET Core Identity ─────────────────────────────────────────────────────
// Identity manages user accounts, password hashing, lockout, roles and claims.
// It is completely separate from IdentityServer; IdentityServer uses it as its
// user store via AddAspNetIdentity<ApplicationUser>() below.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // ── Password policy ────────────────────────────────────────────────────
        // These are the production-grade minimums. Increase RequiredLength for
        // higher-security environments.
        options.Password.RequiredLength = 10;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;

        // ── Lockout policy ─────────────────────────────────────────────────────
        // After 5 consecutive failed attempts the account is locked for 15 minutes.
        // Duende IdentityServer surfaces this via the UserLoginFailureEvent so it
        // appears in the audit log.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // ── Email uniqueness ───────────────────────────────────────────────────
        options.User.RequireUniqueEmail = true;

        // ── Email confirmation ─────────────────────────────────────────────────
        // Set to true in a real production environment once email sending is wired up.
        // Currently false so seed users and newly registered users can log in immediately.
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()  // Use PostgreSQL via EF Core
    .AddDefaultTokenProviders();                        // Password reset / email confirmation tokens

// ── Duende IdentityServer ─────────────────────────────────────────────────────
// IdentityServer is the OIDC/OAuth2 protocol layer. It sits in front of ASP.NET
// Identity and handles all the cryptographic token operations, discovery document,
// JWKS endpoint, consent UI etc.
builder.Services.AddIdentityServer(options =>
    {
        // Emit error events always (e.g. invalid client, invalid grant)
        options.Events.RaiseErrorEvents = true;
        // Information and success events only in development to reduce log volume
        options.Events.RaiseInformationEvents = builder.Environment.IsDevelopment();
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = builder.Environment.IsDevelopment();

        // ── Key management ─────────────────────────────────────────────────────
        // Duende's automatic key management rotates signing keys periodically.
        // Keys are persisted in the database so they survive restarts and are
        // shared across multiple instances in a load-balanced deployment.
        // Production key storage: the DataProtection keys table in PostgreSQL.
        options.KeyManagement.Enabled = true;
    })
    // ── User store integration ─────────────────────────────────────────────────
    // AddAspNetIdentity wires up:
    //   - IUserClaimsPrincipalFactory → reads user claims from ApplicationUser + roles
    //   - IResourceOwnerPasswordValidator → validates Resource Owner Password credentials
    //     (not used here; we use the interactive login page instead)
    //   - IProfileService → emits claims into tokens from the user store
    // This replaces the development-only AddTestUsers() call.
    .AddAspNetIdentity<ApplicationUser>()
    // ── In-memory identity resources ──────────────────────────────────────────
    // Identity resources correspond to OIDC scopes that return user claims.
    //   openid  – required; causes the "sub" claim to be included in the ID token.
    //   profile – name, family_name, given_name, website, etc.
    //   email   – email, email_verified claims.
    .AddInMemoryIdentityResources(
    [
        new IdentityResources.OpenId(),   // openid scope → sub claim
        new IdentityResources.Profile(),  // profile scope → name, given_name, etc.
        new IdentityResources.Email()     // email scope  → email, email_verified
    ])
    // ── API scopes ────────────────────────────────────────────────────────────
    // API scopes model the permissions on downstream APIs. A client requests the
    // scopes it needs; IdentityServer includes them in the access token's "scope"
    // claim. Each microservice validates that its required scope is present.
    .AddInMemoryApiScopes(
    [
        new ApiScope("catalog.read",    "Read access to the product catalogue"),
        new ApiScope("catalog.write",   "Write access to the product catalogue (admin)"),
        new ApiScope("orders.read",     "Read access to orders"),
        new ApiScope("orders.write",    "Write access to orders (create / update)"),
        new ApiScope("merchant.manage", "Manage merchant resources and orders")
    ])
    // ── Clients ───────────────────────────────────────────────────────────────
    // A "client" in OAuth2 terminology is an application that requests tokens.
    // Each client has a fixed set of allowed scopes and redirect URIs.
    // Redirect URIs and CORS origins are read from configuration so they can be
    // changed per-environment without rebuilding the image.
    .AddInMemoryClients(
    [
        // ── Customer SPA (Authorization Code + PKCE) ──────────────────────────
        new Client
        {
            ClientId = "urbanx-spa",
            ClientName = "UrbanX Customer SPA",

            // PKCE Authorization Code – the only recommended grant for browser apps.
            // Do NOT use Implicit grant (deprecated, tokens exposed in URL fragment).
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,

            // Public client: no client secret. SPAs cannot keep secrets.
            RequireClientSecret = false,

            AllowedScopes =
            [
                "openid", "profile", "email",
                "catalog.read", "orders.read", "orders.write"
            ],

            // Redirect URIs are registered per-environment.
            // NEVER use wildcards – always enumerate exact URIs.
            RedirectUris = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXSpa:RedirectUris",
                ["http://localhost:5173/callback", "https://localhost:5173/callback"]),

            PostLogoutRedirectUris = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXSpa:PostLogoutRedirectUris",
                ["http://localhost:5173", "https://localhost:5173"]),

            AllowedCorsOrigins = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXSpa:AllowedCorsOrigins",
                ["http://localhost:5173", "https://localhost:5173"]),

            // Require explicit user consent in production (users see the consent screen
            // listing which scopes the app is requesting). Disabled in development
            // for a faster dev loop.
            RequireConsent = !builder.Environment.IsDevelopment(),

            // Access token lifetime: 60 minutes. Short enough to limit exposure if leaked.
            AccessTokenLifetime = 3600,

            // Sliding refresh token (not enabled here). Enable when offline_access scope
            // is added and the UX requires seamless background token renewal.
            AllowOfflineAccess = false
        },
        // ── Merchant Portal SPA (Authorization Code + PKCE) ───────────────────
        new Client
        {
            ClientId = "urbanx-merchant-spa",
            ClientName = "UrbanX Merchant Portal",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,
            AllowedScopes = ["openid", "profile", "email", "merchant.manage"],

            RedirectUris = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXMerchantSpa:RedirectUris",
                ["http://localhost:5174/callback", "https://localhost:5174/callback"]),

            PostLogoutRedirectUris = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXMerchantSpa:PostLogoutRedirectUris",
                ["http://localhost:5174", "https://localhost:5174"]),

            AllowedCorsOrigins = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXMerchantSpa:AllowedCorsOrigins",
                ["http://localhost:5174", "https://localhost:5174"]),

            RequireConsent = !builder.Environment.IsDevelopment(),
            AccessTokenLifetime = 3600,
            AllowOfflineAccess = false
        },
        // ── Management Portal (Blazor Server, Authorization Code + PKCE, confidential) ──
        // Server-side Blazor app that runs on the host. It can keep a client secret,
        // so we use a confidential client for defence-in-depth.
        new Client
        {
            ClientId = "urbanx-admin",
            ClientName = "UrbanX Management Portal",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = true,
            ClientSecrets =
            {
                new Secret(
                    (builder.Configuration["IdentityServer:Clients:UrbanXAdmin:ClientSecret"]
                        ?? "dev-admin-secret-change-me").Sha256())
            },

            AllowedScopes =
            [
                "openid", "profile", "email",
                "catalog.read", "catalog.write"
            ],

            RedirectUris = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXAdmin:RedirectUris",
                ["http://localhost:5006/signin-oidc", "https://localhost:5006/signin-oidc"]),

            PostLogoutRedirectUris = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXAdmin:PostLogoutRedirectUris",
                ["http://localhost:5006/signout-callback-oidc", "https://localhost:5006/signout-callback-oidc"]),

            AllowedCorsOrigins = GetListFromConfig(builder.Configuration,
                "IdentityServer:Clients:UrbanXAdmin:AllowedCorsOrigins",
                ["http://localhost:5006", "https://localhost:5006"]),

            RequireConsent = false,
            AccessTokenLifetime = 3600,
            AllowOfflineAccess = false
        }
    ]);

// ── Authorization policies for management API endpoints ──────────────────────
builder.Services.AddUrbanXAuthorization();

ValidateProductionIdentityConfiguration(builder.Configuration, builder.Environment);

// ── Build the application ─────────────────────────────────────────────────────
var app = builder.Build();

// ── Health / default endpoints ────────────────────────────────────────────────
app.MapDefaultEndpoints();

// ── Developer conveniences ─────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseProductionDefaults();

// ── Middleware pipeline ───────────────────────────────────────────────────────
// Order matters: routing → static files → IdentityServer → auth → endpoints.
app.UseStaticFiles();  // Serve CSS/JS for login pages if placed in wwwroot/
app.UseRouting();
app.UseIdentityServer(); // Must come before UseAuthentication/UseAuthorization.
                         // UseIdentityServer() registers both authentication and
                         // the IdentityServer endpoints (/connect/*, /.well-known/*).
app.UseAuthorization();  // Must come after UseAuthentication (called by UseIdentityServer)
app.MapRazorPages();     // Maps /Account/Login, /Account/Logout, etc.

// ── Database migration ────────────────────────────────────────────────────────
// Apply any pending EF Core migrations on startup. In a zero-downtime deployment
// strategy, run migrations as a separate init container / job before rolling out
// new pods. MigrateAsync() is idempotent and safe to call on every startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations for ApplicationDbContext…");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

// ── Seed initial data ─────────────────────────────────────────────────────────
// Creates roles and default users if the database is empty.
// Safe to call on every startup; each user/role is only created if absent.
var seedDefaultUsers = builder.Configuration.GetValue<bool?>("IdentityServer:SeedDefaultUsers")
    ?? app.Environment.IsDevelopment();

if (seedDefaultUsers)
{
    await SeedData.EnsureSeedDataAsync(app.Services);
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Skipping identity default user seeding (IdentityServer:SeedDefaultUsers=false).");
}

// ── User management API ───────────────────────────────────────────────────────
// These endpoints allow programmatic user management (e.g., customer self-registration,
// admin user creation). They are not part of the OIDC/OAuth2 protocol, but are
// the "management plane" of the identity service.

// POST /api/account/register
// Self-registration for new customer accounts.
// Returns 201 Created with the new user's ID on success.
// Returns 400 Bad Request with validation errors on failure.
app.MapPost("/api/account/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    // Validate inputs
    RequestValidation.ValidateEmail(request.Email);
    RequestValidation.ValidateRequiredString(request.Password, nameof(request.Password), 100);
    RequestValidation.ValidateRequiredString(request.FullName, nameof(request.FullName), 200);

    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        FullName = request.FullName,
        // Self-registration always creates customers; merchants are created by admins
        Role = "customer",
        CreatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
    {
        return Results.ValidationProblem(
            result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
    }

    await userManager.AddToRoleAsync(user, "customer");

    return Results.Created($"/api/account/{user.Id}", new { userId = user.Id, email = user.Email });
});

// GET /api/account/{userId}
// Returns the public profile of a user. Requires authentication.
app.MapGet("/api/account/{userId}", async (
    string userId,
    UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(userId))
        return Results.BadRequest("userId is required");

    var user = await userManager.FindByIdAsync(userId);
    if (user == null) return Results.NotFound();

    return Results.Ok(new
    {
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.CreatedAt
    });
}).RequireAuthorization();

// POST /api/account/admin/create
// Admin-only endpoint to create merchant or admin accounts.
// Requires the caller to be authenticated with the "admin" role.
app.MapPost("/api/account/admin/create", async (
    AdminCreateUserRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    RequestValidation.ValidateEmail(request.Email);
    RequestValidation.ValidateRequiredString(request.Password, nameof(request.Password), 100);
    RequestValidation.ValidateRequiredString(request.FullName, nameof(request.FullName), 200);
    RequestValidation.ValidateRequiredString(request.Role, nameof(request.Role), 20);

    if (request.Role is not ("customer" or "merchant" or "admin"))
        return Results.BadRequest("Role must be one of: customer, merchant, admin");

    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        EmailConfirmed = true,
        FullName = request.FullName,
        Role = request.Role,
        CreatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
    {
        return Results.ValidationProblem(
            result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
    }

    await userManager.AddToRoleAsync(user, request.Role);

    return Results.Created($"/api/account/{user.Id}", new { userId = user.Id, email = user.Email });
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.Run();

// ── Helper: read comma-separated list from config or fall back to defaults ─────
static List<string> GetListFromConfig(IConfiguration config, string key, List<string> defaults)
{
    var value = config[key];
    if (string.IsNullOrWhiteSpace(value)) return defaults;
    return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

static void ValidateProductionIdentityConfiguration(IConfiguration config, IHostEnvironment environment)
{
    if (!environment.IsProduction())
    {
        return;
    }

    var requiredKeys = new[]
    {
        "IdentityServer:IssuerUri",
        "IdentityServer:Clients:UrbanXSpa:RedirectUris",
        "IdentityServer:Clients:UrbanXSpa:PostLogoutRedirectUris",
        "IdentityServer:Clients:UrbanXSpa:AllowedCorsOrigins",
        "IdentityServer:Clients:UrbanXMerchantSpa:RedirectUris",
        "IdentityServer:Clients:UrbanXMerchantSpa:PostLogoutRedirectUris",
        "IdentityServer:Clients:UrbanXMerchantSpa:AllowedCorsOrigins"
    };

    var missing = requiredKeys.Where(key => string.IsNullOrWhiteSpace(config[key])).ToArray();
    if (missing.Length > 0)
    {
        throw new InvalidOperationException(
            $"Missing required production identity configuration keys: {string.Join(", ", missing)}");
    }
}

// ── Request models ────────────────────────────────────────────────────────────

/// <summary>Registration request model for POST /api/account/register.</summary>
record RegisterRequest(string Email, string Password, string FullName);

/// <summary>Admin create-user request model for POST /api/account/admin/create.</summary>
record AdminCreateUserRequest(string Email, string Password, string FullName, string Role);
