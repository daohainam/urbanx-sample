using Confluent.Kafka;
using System.Text.Json;

namespace UrbanX.Services.Order.Messaging;

public interface IOrderEventPublisher
{
    Task PublishAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken = default);
}

public class KafkaOrderEventPublisher : IOrderEventPublisher, IDisposable
{
    private const string Topic = "order.created";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaOrderEventPublisher> _logger;

    public KafkaOrderEventPublisher(IConfiguration configuration, ILogger<KafkaOrderEventPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = orderCreatedEvent.OrderId.ToString(),
            Value = JsonSerializer.Serialize(orderCreatedEvent)
        };

        try
        {
            var result = await _producer.ProduceAsync(Topic, message, cancellationToken);
            _logger.LogInformation(
                "Published OrderCreated event for order {OrderId} to partition {Partition}",
                orderCreatedEvent.OrderId, result.Partition.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish OrderCreated event for order {OrderId}",
                orderCreatedEvent.OrderId);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
