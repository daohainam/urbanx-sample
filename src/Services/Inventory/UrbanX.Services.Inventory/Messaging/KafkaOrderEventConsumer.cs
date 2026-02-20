using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Inventory.Data;
using UrbanX.Services.Inventory.Models;

namespace UrbanX.Services.Inventory.Messaging;

/// <summary>
/// Consumes OrderCreated events from the order.created Kafka topic.
/// Checks stock availability, reserves inventory, and publishes
/// InventoryReserved or InventoryReservationFailed events as part of the order Saga.
/// </summary>
public class KafkaOrderEventConsumer : BackgroundService
{
    private const string Topic = "order.created";
    private const string ConsumerGroup = "inventory-saga-coordinator";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaOrderEventConsumer> _logger;

    public KafkaOrderEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KafkaOrderEventConsumer> logger)
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

        _logger.LogInformation("Kafka order event consumer started, subscribing to topic {Topic}", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null) continue;

                var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(consumeResult.Message.Value);
                if (orderCreatedEvent == null) continue;

                await ProcessOrderCreatedAsync(orderCreatedEvent, stoppingToken);
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
                _logger.LogError(ex, "Unexpected error processing order created event");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessOrderCreatedAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        _logger.LogInformation("Processing OrderCreated for order {OrderId} with {ItemCount} items",
            orderCreatedEvent.OrderId, orderCreatedEvent.Items.Count);

        var productIds = orderCreatedEvent.Items.Select(i => i.ProductId).ToList();
        var inventoryItems = await db.InventoryItems
            .Where(i => productIds.Contains(i.ProductId))
            .ToListAsync(cancellationToken);

        // Check if all items have sufficient stock
        var failureReasons = new List<string>();
        foreach (var orderItem in orderCreatedEvent.Items)
        {
            var inventoryItem = inventoryItems.FirstOrDefault(i => i.ProductId == orderItem.ProductId);
            if (inventoryItem == null)
            {
                failureReasons.Add($"Product {orderItem.ProductId} not found in inventory");
                continue;
            }

            var available = inventoryItem.QuantityAvailable - inventoryItem.QuantityReserved;
            if (available < orderItem.Quantity)
            {
                failureReasons.Add($"Insufficient stock for product {orderItem.ProductId}: requested {orderItem.Quantity}, available {available}");
            }
        }

        InventoryEvent inventoryEvent;

        if (failureReasons.Count > 0)
        {
            // Publish reservation failed event
            inventoryEvent = new InventoryEvent
            {
                OrderId = orderCreatedEvent.OrderId,
                EventType = InventoryEventType.ReservationFailed,
                Items = orderCreatedEvent.Items.Select(i => new InventoryEventItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList(),
                FailureReason = string.Join("; ", failureReasons),
                OccurredAt = DateTime.UtcNow
            };

            _logger.LogWarning("Inventory reservation failed for order {OrderId}: {Reason}",
                orderCreatedEvent.OrderId, inventoryEvent.FailureReason);
        }
        else
        {
            // Reserve inventory for each item
            foreach (var orderItem in orderCreatedEvent.Items)
            {
                var inventoryItem = inventoryItems.First(i => i.ProductId == orderItem.ProductId);
                inventoryItem.QuantityReserved += orderItem.Quantity;
                inventoryItem.UpdatedAt = DateTime.UtcNow;

                db.InventoryReservations.Add(new InventoryReservation
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderCreatedEvent.OrderId,
                    ProductId = orderItem.ProductId,
                    Quantity = orderItem.Quantity,
                    Status = ReservationStatus.Reserved,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            inventoryEvent = new InventoryEvent
            {
                OrderId = orderCreatedEvent.OrderId,
                EventType = InventoryEventType.Reserved,
                Items = orderCreatedEvent.Items.Select(i => new InventoryEventItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList(),
                OccurredAt = DateTime.UtcNow
            };

            _logger.LogInformation("Inventory reserved for order {OrderId}", orderCreatedEvent.OrderId);
        }

        // Write outbox message atomically with reservations
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = inventoryEvent.EventType.ToString(),
            Payload = JsonSerializer.Serialize(inventoryEvent),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
