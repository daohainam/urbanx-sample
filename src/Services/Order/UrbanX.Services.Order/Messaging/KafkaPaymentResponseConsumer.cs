using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Order.Data;

namespace UrbanX.Services.Order.Messaging;

/// <summary>
/// Consumes payment response events from the payment.events Kafka topic.
/// Updates order status based on whether payment was successfully processed,
/// completing the payment step of the Saga for the order checkout process.
/// </summary>
public class KafkaPaymentResponseConsumer : BackgroundService
{
    private const string Topic = "payment.events";
    private const string ConsumerGroup = "order-payment-saga-coordinator";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaPaymentResponseConsumer> _logger;

    public KafkaPaymentResponseConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KafkaPaymentResponseConsumer> logger)
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

        _logger.LogInformation("Kafka payment response consumer started, subscribing to topic {Topic}", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null) continue;

                var paymentEvent = JsonSerializer.Deserialize<PaymentResponseEvent>(consumeResult.Message.Value);
                if (paymentEvent == null) continue;

                await ProcessPaymentResponseAsync(paymentEvent, stoppingToken);
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
                _logger.LogError(ex, "Unexpected error processing payment response event");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessPaymentResponseAsync(PaymentResponseEvent paymentEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == paymentEvent.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found when processing payment response", paymentEvent.OrderId);
            return;
        }

        var (newStatus, note) = paymentEvent.EventType switch
        {
            PaymentEventType.Completed => (Models.OrderStatus.PaymentReceived, "Payment completed successfully"),
            PaymentEventType.Failed => (Models.OrderStatus.Cancelled, $"Payment failed: {paymentEvent.FailureReason}"),
            _ => (order.Status, null)
        };

        if (newStatus == order.Status)
        {
            _logger.LogDebug("Order {OrderId} status unchanged for payment event {EventType}", paymentEvent.OrderId, paymentEvent.EventType);
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
            "Order {OrderId} status updated to {Status} based on payment event {EventType}",
            paymentEvent.OrderId, newStatus, paymentEvent.EventType);
    }
}
