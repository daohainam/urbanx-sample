# UrbanX Security Best Practices

This document outlines security best practices and considerations for running UrbanX in production.

## Table of Contents
1. [Authentication & Authorization](#authentication--authorization)
2. [Data Protection](#data-protection)
3. [Network Security](#network-security)
4. [Secrets Management](#secrets-management)
5. [Input Validation](#input-validation)
6. [Security Headers](#security-headers)
7. [Monitoring & Auditing](#monitoring--auditing)
8. [Security Checklist](#security-checklist)

## Authentication & Authorization

### Identity Server Configuration

#### Production Settings
```csharp
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = false;  // Disable in production
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = false;  // Disable in production
    
    // Use persistent key storage in production
    options.KeyManagement.Enabled = true;
});
```

#### Replace Test Users
The current implementation uses hardcoded test users which **MUST BE REMOVED** in production:

```csharp
// ❌ DO NOT USE IN PRODUCTION
.AddTestUsers(new List<TestUser> { ... });

// ✅ USE THIS INSTEAD
.AddAspNetIdentity<ApplicationUser>();  // With EF Core storage
```

#### Strong Password Policy
Configure password requirements:
```csharp
services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 4;
});
```

#### Token Configuration
```csharp
new Client
{
    // Required
    RequirePkce = true,
    RequireClientSecret = true,  // For confidential clients
    
    // Token lifetimes (in seconds)
    AccessTokenLifetime = 3600,  // 1 hour
    IdentityTokenLifetime = 300,  // 5 minutes
    RefreshTokenLifetime = 2592000,  // 30 days
    
    // Refresh token settings
    RefreshTokenUsage = TokenUsage.OneTimeOnly,
    RefreshTokenExpiration = TokenExpiration.Sliding,
    
    // Security
    RequireConsent = true,  // Enable in production
    AllowOfflineAccess = false,  // Disable if not needed
}
```

### API Authorization

#### Protect All Endpoints
```csharp
app.MapGet("/api/orders/{id}", async (Guid id, OrderDbContext db) =>
{
    // Add authorization
}).RequireAuthorization("orders.read");

app.MapPost("/api/orders", async (Order order, OrderDbContext db) =>
{
    // Add authorization and validation
}).RequireAuthorization("orders.write");
```

#### Implement Policy-Based Authorization
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("orders.read", policy =>
        policy.RequireClaim("scope", "orders.read"));
    
    options.AddPolicy("orders.write", policy =>
        policy.RequireClaim("scope", "orders.write"));
    
    options.AddPolicy("merchant.manage", policy =>
        policy.RequireClaim("scope", "merchant.manage")
              .RequireClaim("role", "merchant"));
});
```

## Data Protection

### Encrypt Sensitive Data

#### At Rest
```csharp
// Use Data Protection API for sensitive fields
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .ProtectKeysWithCertificate(certificate);

// Encrypt credit card numbers, personal data
public class CreditCard
{
    [ProtectedPersonalData]
    public string? Number { get; set; }
}
```

#### In Transit
- **Always use HTTPS/TLS in production**
- Configure minimum TLS version:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        https.SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12;
    });
});
```

### Database Security

#### Connection String Best Practices
```csharp
// ❌ DO NOT hardcode
"Host=postgres;Database=urbanx;Username=postgres;Password=MyPassword123"

// ✅ Use environment variables
var connectionString = builder.Configuration.GetConnectionString("Database");

// ✅ Even better: Use managed identity (Azure/AWS)
builder.AddNpgsqlDbContext<OrderDbContext>("orderdb", 
    configureSettings: settings => settings.UseAzureManagedIdentity = true);
```

#### Enable SSL for Database Connections
```
Host=postgres;Database=urbanx;Username=postgres;Password=${PASSWORD};SslMode=Require;Trust Server Certificate=false
```

#### Principle of Least Privilege
- Each service should have its own database user
- Grant only required permissions
- Use read-only users for read operations

```sql
-- Create service-specific user
CREATE USER order_service WITH PASSWORD 'strong_password';
GRANT CONNECT ON DATABASE urbanx_order TO order_service;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO order_service;
-- Do NOT grant DELETE or DROP
```

## Network Security

### Service Isolation

#### Use Private Networks
```yaml
# Docker Compose
networks:
  frontend:  # Public-facing services
  backend:   # Internal services only
  
services:
  gateway:
    networks:
      - frontend
      - backend
  
  order-service:
    networks:
      - backend  # Not exposed to internet
```

#### Kubernetes Network Policies
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: order-service-policy
  namespace: urbanx
spec:
  podSelector:
    matchLabels:
      app: order-service
  policyTypes:
  - Ingress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app: gateway  # Only gateway can access
```

### Firewall Rules

#### Cloud Firewall Configuration
```bash
# Azure NSG
az network nsg rule create \
  --name AllowHTTPS \
  --nsg-name urbanx-nsg \
  --priority 100 \
  --source-address-prefixes Internet \
  --destination-port-ranges 443 \
  --access Allow

# AWS Security Group
aws ec2 authorize-security-group-ingress \
  --group-id sg-xxxxx \
  --protocol tcp \
  --port 443 \
  --cidr 0.0.0.0/0
```

### Rate Limiting

#### API Gateway Rate Limiting
```csharp
// In Gateway/Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

app.UseRateLimiter();
```

## Secrets Management

### DO NOT Store Secrets in Code

#### ❌ Bad Practices
```csharp
// Never do this
var password = "MyPassword123";
var apiKey = "sk_live_abc123def456";
builder.Services.AddDbContext(options => 
    options.UseNpgsql("Host=localhost;Password=MyPassword"));
```

#### ✅ Best Practices

##### Azure Key Vault
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

##### AWS Secrets Manager
```csharp
builder.Configuration.AddSecretsManager(
    configurator: options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("urbanx/");
    });
```

##### Environment Variables (Docker/K8s)
```bash
# Use secrets, not plain env vars
kubectl create secret generic db-secret \
  --from-literal=password='$(openssl rand -base64 32)'
```

### Secret Rotation
- Rotate database passwords every 90 days
- Rotate API keys every 180 days
- Automate rotation with scripts or managed services
- Update all services after rotation

## Input Validation

### Validate All Inputs

The shared validation library is available at `src/Shared/UrbanX.Shared.Security/RequestValidation.cs`

#### Usage Example
```csharp
using UrbanX.Shared.Security;

app.MapPost("/api/cart/{customerId}/items", async (
    Guid customerId, 
    CartItem item, 
    OrderDbContext db) =>
{
    // Validate GUID
    RequestValidation.ValidateGuid(customerId, nameof(customerId));
    
    // Validate item properties
    RequestValidation.ValidateGuid(item.ProductId, nameof(item.ProductId));
    RequestValidation.ValidatePositive(item.Quantity, nameof(item.Quantity));
    RequestValidation.ValidatePositive(item.Price, nameof(item.Price));
    
    // Business logic
    // ...
});
```

### SQL Injection Prevention
- ✅ Use parameterized queries (EF Core does this automatically)
- ✅ Use ORM (Entity Framework Core)
- ❌ Never concatenate user input into SQL

```csharp
// ✅ Safe: Parameterized
var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

// ❌ Dangerous: SQL Injection vulnerability
var query = $"SELECT * FROM Orders WHERE Id = '{orderId}'";
```

### Cross-Site Scripting (XSS) Prevention
- ✅ Use Razor/Blazor encoding (automatic)
- ✅ Validate and sanitize all HTML input
- ✅ Set Content-Security-Policy header

### Cross-Site Request Forgery (CSRF)
```csharp
// Enable anti-forgery for state-changing operations
builder.Services.AddAntiforgery();

app.MapPost("/api/orders", async (Order order, OrderDbContext db) =>
{
    // ...
}).RequireAntiforgery();  // Add CSRF protection
```

## Security Headers

Security headers are automatically added by `UseProductionDefaults()` in ServiceDefaults:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Content Security Policy
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");
    
    await next();
});
```

### Additional Headers for Production

```csharp
// Strict Transport Security (HSTS)
app.UseHsts();  // Forces HTTPS for 1 year

// Permissions Policy
context.Response.Headers.Append("Permissions-Policy", 
    "geolocation=(), microphone=(), camera=()");
```

## Monitoring & Auditing

### Security Event Logging

```csharp
// Log authentication events
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;  // Enable in production for auditing
});

// Log authorization failures
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

### Audit Trail

Log security-relevant events:
- Authentication attempts (success/failure)
- Authorization failures
- Sensitive data access (orders, payments, user data)
- Configuration changes
- Admin operations

```csharp
app.MapGet("/api/orders/{id}", async (
    Guid id, 
    OrderDbContext db,
    ILogger<Program> logger,
    ClaimsPrincipal user) =>
{
    logger.LogInformation(
        "User {UserId} accessed order {OrderId}",
        user.FindFirst("sub")?.Value,
        id);
    
    // Fetch order
});
```

### Security Alerts

Configure alerts for:
- Multiple failed authentication attempts
- Authorization failures (potential attack)
- Unusual API usage patterns
- Database access anomalies
- Configuration changes

## Security Checklist

### Pre-Production Checklist

#### Authentication & Authorization
- [ ] Remove hardcoded test users from Identity Service
- [ ] Configure strong password policy
- [ ] Enable HTTPS/TLS for all services
- [ ] Add authorization to all API endpoints
- [ ] Configure proper token lifetimes
- [ ] Enable audit logging for auth events

#### Secrets & Configuration
- [ ] Move all secrets to secure vault (Azure Key Vault / AWS Secrets Manager)
- [ ] Remove secrets from appsettings.json
- [ ] Verify .env files are in .gitignore
- [ ] Configure secret rotation policy
- [ ] Use managed identities where possible

#### Input Validation
- [ ] Add validation to all POST/PUT endpoints
- [ ] Validate GUIDs, required fields, ranges
- [ ] Implement anti-forgery tokens
- [ ] Add rate limiting
- [ ] Sanitize HTML inputs

#### Network Security
- [ ] Configure firewall rules
- [ ] Implement network segmentation
- [ ] Use private networks for internal services
- [ ] Enable database SSL/TLS
- [ ] Configure CORS properly (not localhost)

#### Data Protection
- [ ] Enable encryption at rest for databases
- [ ] Use TLS 1.2+ for all connections
- [ ] Implement data classification
- [ ] Configure automatic backups
- [ ] Test backup restoration

#### Monitoring & Logging
- [ ] Configure centralized logging
- [ ] Set up security alerts
- [ ] Enable audit trails
- [ ] Configure log retention policy
- [ ] Test alerting mechanisms

#### Container Security
- [ ] Scan images for vulnerabilities (Trivy, Snyk)
- [ ] Run containers as non-root user
- [ ] Use minimal base images
- [ ] Keep base images updated
- [ ] Implement resource limits

#### Database Security
- [ ] Use strong database passwords (16+ characters)
- [ ] Create service-specific database users
- [ ] Grant minimum required permissions
- [ ] Enable database audit logging
- [ ] Restrict database network access

### Post-Deployment Checklist

- [ ] Penetration testing performed
- [ ] Security scan completed (no critical issues)
- [ ] Vulnerability scan completed
- [ ] SSL/TLS configuration verified (A+ rating)
- [ ] Security headers verified
- [ ] Authentication flows tested
- [ ] Authorization policies tested
- [ ] Rate limiting tested
- [ ] Backup and restore tested
- [ ] Incident response plan documented

## Security Contacts

For security issues, please contact:
- Security Team: security@yourdomain.com
- On-Call: +1-XXX-XXX-XXXX

## Regular Security Tasks

### Daily
- Monitor security alerts
- Review authentication logs for anomalies

### Weekly
- Review access logs
- Check for CVE announcements for dependencies
- Update container images if needed

### Monthly
- Review and rotate API keys
- Audit user permissions
- Review firewall rules
- Check SSL certificate expiration

### Quarterly
- Rotate database passwords
- Conduct security training
- Review and update security policies
- Penetration testing

### Annually
- Comprehensive security audit
- Disaster recovery drill
- Update security documentation
- Review compliance requirements

---

## Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Docker Security Best Practices](https://docs.docker.com/engine/security/)
- [Kubernetes Security](https://kubernetes.io/docs/concepts/security/)
- [IdentityServer Documentation](https://docs.duendesoftware.com/identityserver/v6)
