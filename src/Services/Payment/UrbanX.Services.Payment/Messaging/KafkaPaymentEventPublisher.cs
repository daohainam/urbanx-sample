using Confluent.Kafka;
using System.Text.Json;

namespace UrbanX.Services.Payment.Messaging;

public interface IPaymentEventPublisher
{
    Task PublishAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default);
}

public class KafkaPaymentEventPublisher : IPaymentEventPublisher, IDisposable
{
    private const string Topic = "payment.events";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaPaymentEventPublisher> _logger;

    public KafkaPaymentEventPublisher(IConfiguration configuration, ILogger<KafkaPaymentEventPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = paymentEvent.OrderId.ToString(),
            Value = JsonSerializer.Serialize(paymentEvent)
        };

        try
        {
            var result = await _producer.ProduceAsync(Topic, message, cancellationToken);
            _logger.LogInformation(
                "Published {EventType} event for order {OrderId} to partition {Partition}",
                paymentEvent.EventType, paymentEvent.OrderId, result.Partition.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event for order {OrderId}",
                paymentEvent.EventType, paymentEvent.OrderId);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
