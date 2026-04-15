using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Customer.Data;
using UrbanX.Services.Customer.Models;
using UrbanX.Services.Customer;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<CustomerDbContext>("customerdb");

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CustomerDbContext>(name: "customerdb", tags: ["ready", "db"]);

// Configure JWT bearer authentication
var identityAuthority = builder.Configuration["services__identity__https__0"]
    ?? builder.Configuration["services__identity__http__0"]
    ?? builder.Configuration["IdentityServer:Authority"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = identityAuthority;
        options.Audience = builder.Configuration["IdentityServer:Audience"] ?? "urbanx-api";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddUrbanXAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations for CustomerDbContext...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        // Seed data in development
        if (app.Environment.IsDevelopment())
        {
            await DataSeeder.SeedAsync(context);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

// ──────────────────────────────────────────────
// Customer management
// ──────────────────────────────────────────────

app.MapGet("/api/customers/{id:guid}", async (Guid id, CustomerDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerId) || callerId != id)
        return Results.Forbid();

    var customer = await db.Customers.FindAsync(id);
    return customer is not null ? Results.Ok(customer) : Results.NotFound();
}).RequireAuthorization(AuthorizationPolicies.CustomerOnly);

app.MapPost("/api/customers", async (UrbanX.Services.Customer.Models.Customer customer, CustomerDbContext db) =>
{
    RequestValidation.ValidateRequiredString(customer.FirstName, nameof(customer.FirstName), 100);
    RequestValidation.ValidateRequiredString(customer.LastName, nameof(customer.LastName), 100);
    RequestValidation.ValidateRequiredString(customer.Email, nameof(customer.Email), 150);
    RequestValidation.ValidateEmail(customer.Email);

    var emailExists = await db.Customers.AnyAsync(c => c.Email == customer.Email);
    if (emailExists)
        return Results.Conflict(new { error = "A customer with this email already exists." });

    customer.Id = Guid.NewGuid();
    customer.CreatedAt = DateTime.UtcNow;
    customer.UpdatedAt = DateTime.UtcNow;
    customer.IsActive = true;

    db.Customers.Add(customer);
    await db.SaveChangesAsync();

    return Results.Created($"/api/customers/{customer.Id}", customer);
}).RequireAuthorization(AuthorizationPolicies.CustomerOnly);

app.MapPut("/api/customers/{id:guid}", async (Guid id, UrbanX.Services.Customer.Models.Customer updatedCustomer, CustomerDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    RequestValidation.ValidateRequiredString(updatedCustomer.FirstName, nameof(updatedCustomer.FirstName), 100);
    RequestValidation.ValidateRequiredString(updatedCustomer.LastName, nameof(updatedCustomer.LastName), 100);

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerId) || callerId != id)
        return Results.Forbid();

    var customer = await db.Customers.FindAsync(id);
    if (customer == null) return Results.NotFound();

    customer.FirstName = updatedCustomer.FirstName;
    customer.LastName = updatedCustomer.LastName;
    customer.Phone = updatedCustomer.Phone;
    customer.Address = updatedCustomer.Address;
    customer.City = updatedCustomer.City;
    customer.Country = updatedCustomer.Country;
    customer.PostalCode = updatedCustomer.PostalCode;
    customer.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(customer);
}).RequireAuthorization(AuthorizationPolicies.CustomerOnly);

app.MapDelete("/api/customers/{id:guid}", async (Guid id, CustomerDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerId) || callerId != id)
        return Results.Forbid();

    var customer = await db.Customers.FindAsync(id);
    if (customer == null) return Results.NotFound();

    // Soft-delete: deactivate the customer
    customer.IsActive = false;
    customer.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.CustomerOnly);

// ──────────────────────────────────────────────
// Customer group management
// ──────────────────────────────────────────────

app.MapGet("/api/customer-groups", async (CustomerDbContext db) =>
{
    var groups = await db.CustomerGroups
        .AsNoTracking()
        .Where(g => g.IsActive)
        .OrderBy(g => g.Name)
        .ToListAsync();
    return Results.Ok(groups);
});

app.MapGet("/api/customer-groups/{id:guid}", async (Guid id, CustomerDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));

    var group = await db.CustomerGroups.FindAsync(id);
    return group is not null ? Results.Ok(group) : Results.NotFound();
});

app.MapPost("/api/customer-groups", async (CustomerGroup group, CustomerDbContext db) =>
{
    RequestValidation.ValidateRequiredString(group.Name, nameof(group.Name), 200);

    var nameExists = await db.CustomerGroups.AnyAsync(g => g.Name == group.Name);
    if (nameExists)
        return Results.Conflict(new { error = "A customer group with this name already exists." });

    group.Id = Guid.NewGuid();
    group.CreatedAt = DateTime.UtcNow;
    group.UpdatedAt = DateTime.UtcNow;
    group.IsActive = true;

    db.CustomerGroups.Add(group);
    await db.SaveChangesAsync();

    return Results.Created($"/api/customer-groups/{group.Id}", group);
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.MapPut("/api/customer-groups/{id:guid}", async (Guid id, CustomerGroup updatedGroup, CustomerDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    RequestValidation.ValidateRequiredString(updatedGroup.Name, nameof(updatedGroup.Name), 200);

    var group = await db.CustomerGroups.FindAsync(id);
    if (group == null) return Results.NotFound();

    var nameConflict = await db.CustomerGroups.AnyAsync(g => g.Name == updatedGroup.Name && g.Id != id);
    if (nameConflict)
        return Results.Conflict(new { error = "A customer group with this name already exists." });

    group.Name = updatedGroup.Name;
    group.Description = updatedGroup.Description;
    group.IsActive = updatedGroup.IsActive;
    group.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(group);
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.MapDelete("/api/customer-groups/{id:guid}", async (Guid id, CustomerDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));

    var group = await db.CustomerGroups.FindAsync(id);
    if (group == null) return Results.NotFound();

    db.CustomerGroups.Remove(group);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

// ──────────────────────────────────────────────
// Customer group membership management
// ──────────────────────────────────────────────

app.MapGet("/api/customer-groups/{groupId:guid}/members", async (Guid groupId, CustomerDbContext db) =>
{
    RequestValidation.ValidateGuid(groupId, nameof(groupId));

    var groupExists = await db.CustomerGroups.AnyAsync(g => g.Id == groupId);
    if (!groupExists) return Results.NotFound();

    var memberIds = await db.CustomerGroupMemberships
        .AsNoTracking()
        .Where(m => m.CustomerGroupId == groupId)
        .Select(m => m.CustomerId)
        .ToListAsync();

    var members = await db.Customers
        .AsNoTracking()
        .Where(c => memberIds.Contains(c.Id))
        .ToListAsync();

    return Results.Ok(members);
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.MapGet("/api/customers/{customerId:guid}/groups", async (Guid customerId, CustomerDbContext db, HttpContext httpContext) =>
{
    RequestValidation.ValidateGuid(customerId, nameof(customerId));

    var sub = httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(sub, out var callerId) || callerId != customerId)
        return Results.Forbid();

    var groupIds = await db.CustomerGroupMemberships
        .AsNoTracking()
        .Where(m => m.CustomerId == customerId)
        .Select(m => m.CustomerGroupId)
        .ToListAsync();

    var groups = await db.CustomerGroups
        .AsNoTracking()
        .Where(g => groupIds.Contains(g.Id))
        .ToListAsync();

    return Results.Ok(groups);
}).RequireAuthorization(AuthorizationPolicies.CustomerOnly);

app.MapPost("/api/customer-groups/{groupId:guid}/members/{customerId:guid}", async (Guid groupId, Guid customerId, CustomerDbContext db) =>
{
    RequestValidation.ValidateGuid(groupId, nameof(groupId));
    RequestValidation.ValidateGuid(customerId, nameof(customerId));

    var groupExists = await db.CustomerGroups.AnyAsync(g => g.Id == groupId && g.IsActive);
    if (!groupExists) return Results.NotFound(new { error = "Customer group not found." });

    var customerExists = await db.Customers.AnyAsync(c => c.Id == customerId && c.IsActive);
    if (!customerExists) return Results.NotFound(new { error = "Customer not found." });

    var alreadyMember = await db.CustomerGroupMemberships
        .AnyAsync(m => m.CustomerId == customerId && m.CustomerGroupId == groupId);
    if (alreadyMember)
        return Results.Conflict(new { error = "Customer is already a member of this group." });

    var membership = new CustomerGroupMembership
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        CustomerGroupId = groupId,
        JoinedAt = DateTime.UtcNow
    };

    db.CustomerGroupMemberships.Add(membership);
    await db.SaveChangesAsync();

    return Results.Created($"/api/customer-groups/{groupId}/members/{customerId}", membership);
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.MapDelete("/api/customer-groups/{groupId:guid}/members/{customerId:guid}", async (Guid groupId, Guid customerId, CustomerDbContext db) =>
{
    RequestValidation.ValidateGuid(groupId, nameof(groupId));
    RequestValidation.ValidateGuid(customerId, nameof(customerId));

    var membership = await db.CustomerGroupMemberships
        .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.CustomerGroupId == groupId);

    if (membership == null) return Results.NotFound();

    db.CustomerGroupMemberships.Remove(membership);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.AdminOnly);

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
