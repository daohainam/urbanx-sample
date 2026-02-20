using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Payment.Data;
using UrbanX.Services.Payment.Models;

namespace UrbanX.Services.Payment.Messaging;

/// <summary>
/// Background service that relays pending outbox messages to Kafka.
/// Implements the transactional outbox pattern for the Payment service.
/// </summary>
public class PaymentOutboxRelayService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 5;
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentOutboxRelayService> _logger;

    public PaymentOutboxRelayService(IServiceScopeFactory scopeFactory, ILogger<PaymentOutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment outbox relay service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in payment outbox relay service");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPaymentEventPublisher>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending payment outbox messages", pending.Count);

        foreach (var message in pending)
        {
            await PublishMessageAsync(db, publisher, message, cancellationToken);
        }
    }

    private async Task PublishMessageAsync(
        PaymentDbContext db,
        IPaymentEventPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var paymentEvent = JsonSerializer.Deserialize<PaymentEvent>(message.Payload);
            if (paymentEvent == null)
            {
                _logger.LogWarning("Payment outbox message {Id} has null payload after deserialization; skipping", message.Id);
                message.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            await publisher.PublishAsync(paymentEvent, cancellationToken);

            message.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment outbox message {Id} ({EventType}) published successfully",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            _logger.LogError(ex,
                "Failed to publish payment outbox message {Id} ({EventType}), retry {Retry}/{Max}",
                message.Id, message.EventType, message.RetryCount, MaxRetries);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "Failed to persist retry count for payment outbox message {Id}; will retry on next poll",
                    message.Id);
            }
        }
    }
}
