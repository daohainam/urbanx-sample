using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor.Services;
using UrbanX.Management.Web.Components;
using UrbanX.Management.Web.Services;
using UrbanX.Management.Web.Services.Auth;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Razor Components (Blazor Server, interactive) ──────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── MudBlazor ─────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// ── Authentication: cookie session + OIDC code+PKCE against Identity service ──
var identityAuthority = builder.Configuration["services__identity__https__0"]
    ?? builder.Configuration["services__identity__http__0"]
    ?? builder.Configuration["Authentication:Oidc:Authority"]
    ?? "http://localhost:5005";

var clientId = builder.Configuration["Authentication:Oidc:ClientId"] ?? "urbanx-admin";
var clientSecret = builder.Configuration["Authentication:Oidc:ClientSecret"] ?? "dev-admin-secret-change-me";

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = identityAuthority;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("catalog.read");
        options.Scope.Add("catalog.write");

        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";

        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
    });

// ── Authorization: AdminOnly is the fallback policy for the entire app ────────
builder.Services.AddUrbanXAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("role", "admin")
        .Build();
});

// ── Backend API client (catalog), with bearer-token forwarding ────────────────
builder.Services.AddTransient<TokenForwardingHandler>();

builder.Services.AddHttpClient<CatalogApiClient>(client =>
    {
        var configuredBase = builder.Configuration["Services:Catalog:BaseUrl"];
        client.BaseAddress = new Uri(configuredBase ?? "http://catalog");
    })
    .AddHttpMessageHandler<TokenForwardingHandler>()
    .AddStandardResilienceHandler();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Login / logout endpoints
app.MapGet("/login", (string? returnUrl, HttpContext ctx) =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]));

app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
