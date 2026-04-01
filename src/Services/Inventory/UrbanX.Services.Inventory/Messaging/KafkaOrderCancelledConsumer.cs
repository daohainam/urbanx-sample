using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Inventory.Data;
using UrbanX.Services.Inventory.Models;

namespace UrbanX.Services.Inventory.Messaging;

/// <summary>
/// Consumes OrderCancelled events from the order.cancelled Kafka topic.
/// Releases any previously reserved inventory for the cancelled order
/// as a Saga compensation step when payment fails.
/// </summary>
public class KafkaOrderCancelledConsumer : BackgroundService
{
    private const string Topic = "order.cancelled";
    private const string ConsumerGroup = "inventory-saga-compensation";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaOrderCancelledConsumer> _logger;

    public KafkaOrderCancelledConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KafkaOrderCancelledConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topic);

        _logger.LogInformation("Kafka order cancelled consumer started, subscribing to topic {Topic}", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null) continue;

                var orderCancelledEvent = JsonSerializer.Deserialize<OrderCancelledEvent>(consumeResult.Message.Value);
                if (orderCancelledEvent == null) continue;

                await ProcessOrderCancelledAsync(orderCancelledEvent, stoppingToken);
                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing order cancelled event");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessOrderCancelledAsync(OrderCancelledEvent orderCancelledEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        _logger.LogInformation("Processing OrderCancelled for order {OrderId}", orderCancelledEvent.OrderId);

        // Find all active (Reserved) reservations for this order.
        // Only Reserved items need to be released; idempotent: if already Released, nothing changes.
        var reservations = await db.InventoryReservations
            .Where(r => r.OrderId == orderCancelledEvent.OrderId && r.Status == ReservationStatus.Reserved)
            .ToListAsync(cancellationToken);

        if (reservations.Count == 0)
        {
            _logger.LogInformation(
                "No active reservations found for order {OrderId}; nothing to release",
                orderCancelledEvent.OrderId);
            return;
        }

        var productIds = reservations.Select(r => r.ProductId).Distinct().ToList();
        var inventoryItems = await db.InventoryItems
            .Where(i => productIds.Contains(i.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            reservation.Status = ReservationStatus.Released;
            reservation.UpdatedAt = DateTime.UtcNow;

            var inventoryItem = inventoryItems.FirstOrDefault(i => i.ProductId == reservation.ProductId);
            if (inventoryItem != null)
            {
                inventoryItem.QuantityReserved = Math.Max(0, inventoryItem.QuantityReserved - reservation.Quantity);
                inventoryItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Publish InventoryReleased event via transactional outbox
        var inventoryEvent = new InventoryEvent
        {
            OrderId = orderCancelledEvent.OrderId,
            EventType = InventoryEventType.Released,
            Items = reservations.Select(r => new InventoryEventItem
            {
                ProductId = r.ProductId,
                Quantity = r.Quantity
            }).ToList(),
            OccurredAt = DateTime.UtcNow
        };

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = inventoryEvent.EventType.ToString(),
            Payload = JsonSerializer.Serialize(inventoryEvent),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Released {Count} reservation(s) for cancelled order {OrderId}",
            reservations.Count, orderCancelledEvent.OrderId);
    }
}
