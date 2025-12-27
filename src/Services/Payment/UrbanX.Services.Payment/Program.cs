using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<PaymentDbContext>("paymentdb");

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}

// Payment processing
app.MapPost("/api/payments", async (UrbanX.Services.Payment.Models.Payment payment, PaymentDbContext db) =>
{
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
    var payment = await db.Payments.FindAsync(id);
    return payment is not null ? Results.Ok(payment) : Results.NotFound();
});

app.MapGet("/api/payments/order/{orderId:guid}", async (Guid orderId, PaymentDbContext db) =>
{
    var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    return payment is not null ? Results.Ok(payment) : Results.NotFound();
});

app.Run();
