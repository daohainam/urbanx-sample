using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var identityAuthority = builder.Configuration["services__identity__https__0"]
    ?? builder.Configuration["services__identity__http__0"]
    ?? builder.Configuration["IdentityServer:Authority"];

var allowedOrigins = ResolveAllowedOrigins(builder.Configuration);

if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(identityAuthority))
{
    throw new InvalidOperationException("IdentityServer:Authority must be configured in production.");
}

ValidateGatewayProductionConfiguration(builder.Configuration, builder.Environment, allowedOrigins);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = identityAuthority;
        options.Audience = builder.Configuration["IdentityServer:Audience"] ?? "urbanx-api";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("gateway-authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With")
              .AllowCredentials();
    });
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    var permitLimit = Math.Max(1, builder.Configuration.GetValue<int?>("RateLimit:RequestsPerMinute") ?? 100);

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = !string.IsNullOrWhiteSpace(userId) ? $"user:{userId}" : $"anon:{ipAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again later." }, token);
    };
});

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseProductionDefaults();
app.UseForwardedHeaders();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Map reverse proxy
app.MapReverseProxy();

app.Run();

static string[] ResolveAllowedOrigins(IConfiguration configuration)
{
    var fromArray = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (fromArray is { Length: > 0 })
    {
        return fromArray;
    }

    var csv = configuration["Cors:AllowedOriginsCsv"]
              ?? configuration["CORS_ALLOWED_ORIGINS"];

    if (!string.IsNullOrWhiteSpace(csv))
    {
        var values = csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (values.Length > 0)
        {
            return values;
        }
    }

    return ["http://localhost:5173", "https://localhost:5173", "http://localhost:5174", "https://localhost:5174"];
}

static void ValidateGatewayProductionConfiguration(
    IConfiguration configuration,
    IHostEnvironment environment,
    IReadOnlyCollection<string> allowedOrigins)
{
    if (!environment.IsProduction())
    {
        return;
    }

    if (allowedOrigins.Count == 0
        || allowedOrigins.Any(origin =>
            origin.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase)
            || origin.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Configure non-localhost CORS origins for production.");
    }

    var clusterAddresses = configuration.GetSection("ReverseProxy:Clusters")
        .GetChildren()
        .SelectMany(cluster => cluster.GetSection("Destinations").GetChildren())
        .Select(destination => destination["Address"])
        .Where(address => !string.IsNullOrWhiteSpace(address))
        .ToArray();

    if (clusterAddresses.Length == 0 || clusterAddresses.Any(address => address!.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Configure non-localhost reverse proxy destination addresses for production.");
    }
}
