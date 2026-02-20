using Confluent.Kafka;
using System.Text.Json;

namespace UrbanX.Services.Inventory.Messaging;

public interface IInventoryEventPublisher
{
    Task PublishAsync(InventoryEvent inventoryEvent, CancellationToken cancellationToken = default);
}

public class KafkaInventoryEventPublisher : IInventoryEventPublisher, IDisposable
{
    private const string Topic = "inventory.events";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaInventoryEventPublisher> _logger;

    public KafkaInventoryEventPublisher(IConfiguration configuration, ILogger<KafkaInventoryEventPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(InventoryEvent inventoryEvent, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = inventoryEvent.OrderId.ToString(),
            Value = JsonSerializer.Serialize(inventoryEvent)
        };

        try
        {
            var result = await _producer.ProduceAsync(Topic, message, cancellationToken);
            _logger.LogInformation(
                "Published {EventType} event for order {OrderId} to partition {Partition}",
                inventoryEvent.EventType, inventoryEvent.OrderId, result.Partition.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event for order {OrderId}",
                inventoryEvent.EventType, inventoryEvent.OrderId);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
