using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Messaging;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.IntegrationTests;

public class OutboxPatternTests
{
    private static CatalogDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task CreateProduct_ShouldAtomicallySaveProductAndOutboxMessage()
    {
        // Arrange
        using var context = CreateContext("Outbox_Create_" + Guid.NewGuid());
        var productId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Price = 49.99m,
            MerchantId = merchantId,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var productEvent = new ProductEvent
        {
            ProductId = productId,
            EventType = ProductEventType.Created,
            Name = product.Name,
            Price = product.Price,
            MerchantId = merchantId,
            StockQuantity = product.StockQuantity,
            IsActive = true,
            CreatedAt = now,
            OccurredAt = now
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(ProductEventType.Created),
            Payload = JsonSerializer.Serialize(productEvent),
            CreatedAt = now
        };

        // Act – save product and outbox in one SaveChangesAsync
        context.Products.Add(product);
        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync();

        // Assert – both are saved
        var savedProduct = await context.Products.FindAsync(productId);
        var savedOutbox = await context.OutboxMessages.FirstOrDefaultAsync(m => m.EventType == nameof(ProductEventType.Created));

        Assert.NotNull(savedProduct);
        Assert.NotNull(savedOutbox);
        Assert.Equal(nameof(ProductEventType.Created), savedOutbox!.EventType);
        Assert.Null(savedOutbox.ProcessedAt);
        Assert.Equal(0, savedOutbox.RetryCount);
    }

    [Fact]
    public async Task UpdateProduct_ShouldAtomicallySaveChangesAndOutboxMessage()
    {
        // Arrange
        using var context = CreateContext("Outbox_Update_" + Guid.NewGuid());
        var productId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        context.Products.Add(new Product
        {
            Id = productId,
            Name = "Original",
            Price = 10.00m,
            MerchantId = merchantId,
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();

        // Act
        var product = await context.Products.FindAsync(productId);
        product!.Name = "Updated";
        product.UpdatedAt = DateTime.UtcNow;

        var productEvent = new ProductEvent
        {
            ProductId = productId,
            EventType = ProductEventType.Updated,
            Name = product.Name,
            Price = product.Price,
            MerchantId = product.MerchantId,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            OccurredAt = product.UpdatedAt
        };

        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(ProductEventType.Updated),
            Payload = JsonSerializer.Serialize(productEvent),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert
        var updatedProduct = await context.Products.FindAsync(productId);
        var outboxMessage = await context.OutboxMessages.FirstOrDefaultAsync();

        Assert.Equal("Updated", updatedProduct!.Name);
        Assert.NotNull(outboxMessage);
        Assert.Equal(nameof(ProductEventType.Updated), outboxMessage!.EventType);
        Assert.Null(outboxMessage.ProcessedAt);
    }

    [Fact]
    public async Task DeleteProduct_ShouldAtomicallySaveDeleteAndOutboxMessage()
    {
        // Arrange
        using var context = CreateContext("Outbox_Delete_" + Guid.NewGuid());
        var productId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        context.Products.Add(new Product
        {
            Id = productId,
            Name = "To Delete",
            Price = 5.00m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();

        // Act
        var product = await context.Products.FindAsync(productId);
        var productEvent = new ProductEvent
        {
            ProductId = productId,
            EventType = ProductEventType.Deleted,
            MerchantId = product!.MerchantId,
            CreatedAt = product.CreatedAt,
            OccurredAt = DateTime.UtcNow
        };

        context.Products.Remove(product);
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(ProductEventType.Deleted),
            Payload = JsonSerializer.Serialize(productEvent),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert – product is gone but outbox message exists
        var deletedProduct = await context.Products.FindAsync(productId);
        var outboxMessage = await context.OutboxMessages.FirstOrDefaultAsync();

        Assert.Null(deletedProduct);
        Assert.NotNull(outboxMessage);
        Assert.Equal(nameof(ProductEventType.Deleted), outboxMessage!.EventType);
        Assert.Null(outboxMessage.ProcessedAt);
    }

    [Fact]
    public async Task SoftDeleteProduct_ShouldAtomicallySaveIsActiveFalseAndOutboxMessage()
    {
        // Arrange
        using var context = CreateContext("Outbox_SoftDelete_" + Guid.NewGuid());
        var productId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        context.Products.Add(new Product
        {
            Id = productId,
            Name = "To Soft Delete",
            Price = 5.00m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();

        // Act – simulate the soft-delete path used by the endpoint
        var product = await context.Products.FindAsync(productId);
        var productEvent = new ProductEvent
        {
            ProductId = productId,
            EventType = ProductEventType.Deleted,
            MerchantId = product!.MerchantId,
            CreatedAt = product.CreatedAt,
            OccurredAt = DateTime.UtcNow
        };

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(ProductEventType.Deleted),
            Payload = JsonSerializer.Serialize(productEvent),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert – product still exists in DB but is inactive; outbox message exists
        var softDeleted = await context.Products.FindAsync(productId);
        var outboxMessage = await context.OutboxMessages.FirstOrDefaultAsync();

        Assert.NotNull(softDeleted);
        Assert.False(softDeleted!.IsActive);
        Assert.NotNull(outboxMessage);
        Assert.Equal(nameof(ProductEventType.Deleted), outboxMessage!.EventType);
        Assert.Null(outboxMessage.ProcessedAt);
    }

    [Fact]
    public async Task OutboxMessage_AfterProcessing_ShouldHaveProcessedAtSet()
    {
        // Arrange
        using var context = CreateContext("Outbox_Processed_" + Guid.NewGuid());
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var outboxMessage = new OutboxMessage
        {
            Id = messageId,
            EventType = nameof(ProductEventType.Created),
            Payload = "{}",
            CreatedAt = now
        };

        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync();

        // Act – simulate relay marking message as processed
        var message = await context.OutboxMessages.FindAsync(messageId);
        Assert.NotNull(message);
        message!.ProcessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var processedMessage = await context.OutboxMessages.FindAsync(messageId);
        Assert.NotNull(processedMessage!.ProcessedAt);
    }

    [Fact]
    public async Task OutboxMessage_OnFailure_ShouldIncrementRetryCount()
    {
        // Arrange
        using var context = CreateContext("Outbox_Retry_" + Guid.NewGuid());
        var messageId = Guid.NewGuid();

        var outboxMessage = new OutboxMessage
        {
            Id = messageId,
            EventType = nameof(ProductEventType.Created),
            Payload = "{}",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync();

        // Act – simulate relay incrementing retry count on failure
        var message = await context.OutboxMessages.FindAsync(messageId);
        message!.RetryCount++;
        await context.SaveChangesAsync();

        // Assert
        var retriedMessage = await context.OutboxMessages.FindAsync(messageId);
        Assert.Equal(1, retriedMessage!.RetryCount);
        Assert.Null(retriedMessage.ProcessedAt);
    }

    [Fact]
    public async Task OutboxMessages_QueryPending_ShouldReturnOnlyUnprocessed()
    {
        // Arrange
        using var context = CreateContext("Outbox_Pending_" + Guid.NewGuid());
        var now = DateTime.UtcNow;

        context.OutboxMessages.AddRange(
            new OutboxMessage { Id = Guid.NewGuid(), EventType = "Created", Payload = "{}", CreatedAt = now },
            new OutboxMessage { Id = Guid.NewGuid(), EventType = "Updated", Payload = "{}", CreatedAt = now },
            new OutboxMessage { Id = Guid.NewGuid(), EventType = "Deleted", Payload = "{}", CreatedAt = now, ProcessedAt = now }
        );
        await context.SaveChangesAsync();

        // Act – query as OutboxRelayService would
        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Assert
        Assert.Equal(2, pending.Count);
        Assert.All(pending, m => Assert.Null(m.ProcessedAt));
    }

    [Fact]
    public async Task OutboxMessage_PayloadShouldDeserializeToProductEvent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        var productEvent = new ProductEvent
        {
            ProductId = productId,
            EventType = ProductEventType.Created,
            Name = "Test",
            Price = 9.99m,
            MerchantId = merchantId,
            StockQuantity = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            OccurredAt = DateTime.UtcNow
        };

        var payload = JsonSerializer.Serialize(productEvent);

        // Act – simulate relay deserializing the payload
        var deserialized = JsonSerializer.Deserialize<ProductEvent>(payload);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(productId, deserialized!.ProductId);
        Assert.Equal(ProductEventType.Created, deserialized.EventType);
        Assert.Equal("Test", deserialized.Name);
        Assert.Equal(merchantId, deserialized.MerchantId);
    }
}
