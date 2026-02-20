using Confluent.Kafka;
using System.Text.Json;
using UrbanX.Services.Catalog.Search;

namespace UrbanX.Services.Catalog.Messaging;

public class KafkaProductEventConsumer : BackgroundService
{
    private const string Topic = "catalog.products";
    private const string ConsumerGroup = "catalog-search-indexer";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaProductEventConsumer> _logger;

    public KafkaProductEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KafkaProductEventConsumer> logger)
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

        _logger.LogInformation("Kafka product event consumer started, subscribing to topic {Topic}", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null) continue;

                var productEvent = JsonSerializer.Deserialize<ProductEvent>(consumeResult.Message.Value);
                if (productEvent == null) continue;

                await ProcessEventAsync(productEvent, stoppingToken);
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
                _logger.LogError(ex, "Unexpected error processing product event");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessEventAsync(ProductEvent productEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var searchService = scope.ServiceProvider.GetRequiredService<IProductSearchService>();

        switch (productEvent.EventType)
        {
            case ProductEventType.Created:
            case ProductEventType.Updated:
                var document = new ProductDocument
                {
                    Id = productEvent.ProductId,
                    Name = productEvent.Name ?? string.Empty,
                    Description = productEvent.Description,
                    Price = productEvent.Price,
                    MerchantId = productEvent.MerchantId,
                    StockQuantity = productEvent.StockQuantity,
                    ImageUrl = productEvent.ImageUrl,
                    Category = productEvent.Category,
                    IsActive = productEvent.IsActive,
                    CreatedAt = productEvent.CreatedAt,
                    UpdatedAt = productEvent.OccurredAt
                };
                await searchService.IndexAsync(document, cancellationToken);
                _logger.LogInformation("Indexed product {ProductId} ({EventType})", productEvent.ProductId, productEvent.EventType);
                break;

            case ProductEventType.Deleted:
                await searchService.DeleteAsync(productEvent.ProductId, cancellationToken);
                _logger.LogInformation("Removed product {ProductId} from index", productEvent.ProductId);
                break;

            default:
                _logger.LogWarning("Unknown product event type: {EventType}", productEvent.EventType);
                break;
        }
    }
}
