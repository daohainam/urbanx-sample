using Confluent.Kafka;
using System.Text.Json;

namespace UrbanX.Services.Catalog.Messaging;

public interface IProductEventPublisher
{
    Task PublishAsync(ProductEvent productEvent, CancellationToken cancellationToken = default);
}

public class KafkaProductEventPublisher : IProductEventPublisher, IDisposable
{
    private const string Topic = "catalog.products";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProductEventPublisher> _logger;

    public KafkaProductEventPublisher(IConfiguration configuration, ILogger<KafkaProductEventPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(ProductEvent productEvent, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = productEvent.ProductId.ToString(),
            Value = JsonSerializer.Serialize(productEvent)
        };

        try
        {
            var result = await _producer.ProduceAsync(Topic, message, cancellationToken);
            _logger.LogInformation(
                "Published {EventType} event for product {ProductId} to partition {Partition}",
                productEvent.EventType, productEvent.ProductId, result.Partition.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event for product {ProductId}",
                productEvent.EventType, productEvent.ProductId);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
