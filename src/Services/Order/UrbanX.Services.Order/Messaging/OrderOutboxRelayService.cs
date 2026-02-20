using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Order.Data;
using UrbanX.Services.Order.Models;

namespace UrbanX.Services.Order.Messaging;

/// <summary>
/// Background service that relays pending outbox messages to Kafka.
/// Implements the transactional outbox pattern for the Order service.
/// </summary>
public class OrderOutboxRelayService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 5;
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderOutboxRelayService> _logger;

    public OrderOutboxRelayService(IServiceScopeFactory scopeFactory, ILogger<OrderOutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order outbox relay service started");

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
                _logger.LogError(ex, "Unexpected error in order outbox relay service");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOrderEventPublisher>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending order outbox messages", pending.Count);

        foreach (var message in pending)
        {
            await PublishMessageAsync(db, publisher, message, cancellationToken);
        }
    }

    private async Task PublishMessageAsync(
        OrderDbContext db,
        IOrderEventPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload);
            if (orderCreatedEvent == null)
            {
                _logger.LogWarning("Order outbox message {Id} has null payload after deserialization; skipping", message.Id);
                message.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            await publisher.PublishAsync(orderCreatedEvent, cancellationToken);

            message.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order outbox message {Id} ({EventType}) published successfully",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            _logger.LogError(ex,
                "Failed to publish order outbox message {Id} ({EventType}), retry {Retry}/{Max}",
                message.Id, message.EventType, message.RetryCount, MaxRetries);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "Failed to persist retry count for order outbox message {Id}; will retry on next poll",
                    message.Id);
            }
        }
    }
}
