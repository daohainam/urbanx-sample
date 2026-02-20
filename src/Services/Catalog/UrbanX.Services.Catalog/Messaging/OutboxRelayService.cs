using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.Messaging;

/// <summary>
/// Background service that relays pending outbox messages to Kafka.
/// Implements the transactional outbox pattern: messages are written
/// atomically with domain changes and then published asynchronously.
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
        _logger.LogInformation("Outbox relay service started");

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
                _logger.LogError(ex, "Unexpected error in outbox relay service");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IProductEventPublisher>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending outbox messages", pending.Count);

        foreach (var message in pending)
        {
            await PublishMessageAsync(db, publisher, message, cancellationToken);
        }
    }

    private async Task PublishMessageAsync(
        CatalogDbContext db,
        IProductEventPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        ProductEvent? productEvent = null;
        try
        {
            productEvent = JsonSerializer.Deserialize<ProductEvent>(message.Payload);
            if (productEvent == null)
            {
                _logger.LogWarning("Outbox message {Id} has null payload after deserialization; skipping", message.Id);
                message.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            await publisher.PublishAsync(productEvent, cancellationToken);

            // Mark as processed only after a successful publish.
            // If SaveChangesAsync fails here, the message remains pending and
            // will be republished on the next poll — this is the intended
            // at-least-once delivery guarantee.
            message.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Outbox message {Id} ({EventType}) published successfully",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            _logger.LogError(ex,
                "Failed to publish outbox message {Id} ({EventType}), retry {Retry}/{Max}",
                message.Id, message.EventType, message.RetryCount, MaxRetries);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "Failed to persist retry count for outbox message {Id}; will retry on next poll",
                    message.Id);
            }
        }
    }
}
