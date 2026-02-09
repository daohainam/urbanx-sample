using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;
using UrbanX.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<PaymentDbContext>("paymentdb");

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentDbContext>(name: "paymentdb", tags: new[] { "ready", "db" });

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations for PaymentDbContext...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

// Payment processing
app.MapPost("/api/payments", async (UrbanX.Services.Payment.Models.Payment payment, PaymentDbContext db) =>
{
    RequestValidation.ValidateGuid(payment.OrderId, nameof(payment.OrderId));
    RequestValidation.ValidatePositive(payment.Amount, nameof(payment.Amount));
    
    payment.Id = Guid.NewGuid();
    payment.Status = PaymentStatus.Processing;
    payment.CreatedAt = DateTime.UtcNow;
    payment.UpdatedAt = DateTime.UtcNow;
    
    // Simulate payment processing
    payment.TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8]}";
    payment.Status = PaymentStatus.Completed; // In real scenario, this would be async
    
    db.Payments.Add(payment);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/payments/{payment.Id}", payment);
});

app.MapGet("/api/payments/{id:guid}", async (Guid id, PaymentDbContext db) =>
{
    RequestValidation.ValidateGuid(id, nameof(id));
    
    var payment = await db.Payments.FindAsync(id);
    return payment is not null ? Results.Ok(payment) : Results.NotFound();
});

app.MapGet("/api/payments/order/{orderId:guid}", async (Guid orderId, PaymentDbContext db) =>
{
    RequestValidation.ValidateGuid(orderId, nameof(orderId));
    
    var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    return payment is not null ? Results.Ok(payment) : Results.NotFound();
});

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }
