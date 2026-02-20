using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Order.Data;

namespace UrbanX.Services.Order.Messaging;

/// <summary>
/// Consumes inventory response events from the inventory.events Kafka topic.
/// Updates order status based on whether inventory was successfully reserved,
/// completing the Saga for the order checking process.
/// </summary>
public class KafkaInventoryResponseConsumer : BackgroundService
{
    private const string Topic = "inventory.events";
    private const string ConsumerGroup = "order-saga-coordinator";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaInventoryResponseConsumer> _logger;

    public KafkaInventoryResponseConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KafkaInventoryResponseConsumer> logger)
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

        _logger.LogInformation("Kafka inventory response consumer started, subscribing to topic {Topic}", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null) continue;

                var inventoryEvent = JsonSerializer.Deserialize<InventoryResponseEvent>(consumeResult.Message.Value);
                if (inventoryEvent == null) continue;

                await ProcessInventoryResponseAsync(inventoryEvent, stoppingToken);
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
                _logger.LogError(ex, "Unexpected error processing inventory response event");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessInventoryResponseAsync(InventoryResponseEvent inventoryEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == inventoryEvent.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found when processing inventory response", inventoryEvent.OrderId);
            return;
        }

        var (newStatus, note) = inventoryEvent.EventType switch
        {
            InventoryEventType.Reserved => (Models.OrderStatus.Confirmed, "Inventory reserved successfully"),
            InventoryEventType.ReservationFailed => (Models.OrderStatus.Cancelled, $"Inventory reservation failed: {inventoryEvent.FailureReason}"),
            _ => (order.Status, null)
        };

        if (newStatus == order.Status)
        {
            _logger.LogDebug("Order {OrderId} status unchanged for inventory event {EventType}", inventoryEvent.OrderId, inventoryEvent.EventType);
            return;
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        order.StatusHistory.Add(new Models.OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = newStatus,
            Note = note,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} status updated to {Status} based on inventory event {EventType}",
            inventoryEvent.OrderId, newStatus, inventoryEvent.EventType);
    }
}
