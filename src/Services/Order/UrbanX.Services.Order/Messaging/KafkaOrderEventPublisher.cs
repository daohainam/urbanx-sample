using Confluent.Kafka;
using System.Text.Json;

namespace UrbanX.Services.Order.Messaging;

public interface IOrderEventPublisher
{
    Task PublishAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken = default);
    Task PublishCancellationAsync(OrderCancelledEvent orderCancelledEvent, CancellationToken cancellationToken = default);
}

public class KafkaOrderEventPublisher : IOrderEventPublisher, IDisposable
{
    private const string OrderCreatedTopic = "order.created";
    private const string OrderCancelledTopic = "order.cancelled";

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
            var result = await _producer.ProduceAsync(OrderCreatedTopic, message, cancellationToken);
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

    public async Task PublishCancellationAsync(OrderCancelledEvent orderCancelledEvent, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = orderCancelledEvent.OrderId.ToString(),
            Value = JsonSerializer.Serialize(orderCancelledEvent)
        };

        try
        {
            var result = await _producer.ProduceAsync(OrderCancelledTopic, message, cancellationToken);
            _logger.LogInformation(
                "Published OrderCancelled event for order {OrderId} to partition {Partition}",
                orderCancelledEvent.OrderId, result.Partition.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish OrderCancelled event for order {OrderId}",
                orderCancelledEvent.OrderId);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
