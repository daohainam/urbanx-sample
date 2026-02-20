using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Inventory.Data;
using UrbanX.Services.Inventory.Models;

namespace UrbanX.Services.Inventory.Messaging;

/// <summary>
/// Background service that relays pending outbox messages to Kafka.
/// Implements the transactional outbox pattern for the Inventory service.
/// </summary>
public class OutboxRelayService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 5;
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxRelayService> _logger;

    public OutboxRelayService(IServiceScopeFactory scopeFactory, ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory outbox relay service started");

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
                _logger.LogError(ex, "Unexpected error in inventory outbox relay service");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IInventoryEventPublisher>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending inventory outbox messages", pending.Count);

        foreach (var message in pending)
        {
            await PublishMessageAsync(db, publisher, message, cancellationToken);
        }
    }

    private async Task PublishMessageAsync(
        InventoryDbContext db,
        IInventoryEventPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var inventoryEvent = JsonSerializer.Deserialize<InventoryEvent>(message.Payload);
            if (inventoryEvent == null)
            {
                _logger.LogWarning("Inventory outbox message {Id} has null payload after deserialization; skipping", message.Id);
                message.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            await publisher.PublishAsync(inventoryEvent, cancellationToken);

            message.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Inventory outbox message {Id} ({EventType}) published successfully",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            _logger.LogError(ex,
                "Failed to publish inventory outbox message {Id} ({EventType}), retry {Retry}/{Max}",
                message.Id, message.EventType, message.RetryCount, MaxRetries);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "Failed to persist retry count for inventory outbox message {Id}; will retry on next poll",
                    message.Id);
            }
        }
    }
}
