using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;
using UrbanX.Services.Payment.PaymentGateways;
using UrbanX.Services.Payment.PaymentGateways.Stripe;
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

// Configure Stripe Payment Gateway (Anti-Corruption Layer)
var stripeConfig = builder.Configuration.GetSection("Stripe").Get<StripeSettings>() 
    ?? new StripeSettings();
builder.Services.AddSingleton(stripeConfig);
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();

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
app.MapPost("/api/payments", async (UrbanX.Services.Payment.Models.Payment payment, PaymentDbContext db, IPaymentGateway paymentGateway) =>
{
    RequestValidation.ValidateGuid(payment.OrderId, nameof(payment.OrderId));
    RequestValidation.ValidatePositive(payment.Amount, nameof(payment.Amount));
    
    payment.Id = Guid.NewGuid();
    payment.Status = PaymentStatus.Processing;
    payment.CreatedAt = DateTime.UtcNow;
    payment.UpdatedAt = DateTime.UtcNow;
    
    // Process payment through Stripe gateway (Anti-Corruption Layer)
    if (payment.Method == PaymentMethod.Stripe)
    {
        var gatewayRequest = new PaymentGatewayRequest(
            OrderId: payment.OrderId,
            Amount: payment.Amount,
            Currency: "usd",
            Metadata: new Dictionary<string, string>
            {
                { "payment_id", payment.Id.ToString() }
            }
        );
        
        var result = await paymentGateway.ProcessPaymentAsync(gatewayRequest);
        
        if (result.Success)
        {
            payment.TransactionId = result.TransactionId;
            payment.Status = result.Status switch
            {
                PaymentGatewayStatus.Succeeded => PaymentStatus.Completed,
                PaymentGatewayStatus.Processing => PaymentStatus.Processing,
                PaymentGatewayStatus.Pending => PaymentStatus.Pending,
                PaymentGatewayStatus.Failed => PaymentStatus.Failed,
                PaymentGatewayStatus.Refunded => PaymentStatus.Refunded,
                _ => PaymentStatus.Failed
            };
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.TransactionId = $"FAILED-{Guid.NewGuid().ToString()[..8]}";
        }
    }
    else
    {
        // Simulate payment processing for other methods
        payment.TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8]}";
        payment.Status = PaymentStatus.Completed; // In real scenario, this would be async
    }
    
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
