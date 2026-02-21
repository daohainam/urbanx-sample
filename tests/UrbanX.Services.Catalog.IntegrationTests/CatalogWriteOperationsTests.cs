using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Messaging;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.IntegrationTests;

public class CatalogWriteOperationsTests
{
    private static CatalogDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task CatalogService_ShouldCreateProduct()
    {
        // Arrange
        using var context = CreateContext("CatalogCreate_" + Guid.NewGuid());
        var merchantId = Guid.NewGuid();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "New Product",
            Description = "A brand new product",
            Price = 49.99m,
            MerchantId = merchantId,
            StockQuantity = 100,
            Category = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Products.FindAsync(product.Id);
        Assert.NotNull(saved);
        Assert.Equal("New Product", saved!.Name);
        Assert.Equal(49.99m, saved.Price);
        Assert.Equal(merchantId, saved.MerchantId);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task CatalogService_ShouldUpdateProduct()
    {
        // Arrange
        using var context = CreateContext("CatalogUpdate_" + Guid.NewGuid());
        var merchantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        context.Products.Add(new Product
        {
            Id = productId,
            Name = "Original Name",
            Price = 29.99m,
            MerchantId = merchantId,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var product = await context.Products.FindAsync(productId);
        Assert.NotNull(product);
        product!.Name = "Updated Name";
        product.Price = 39.99m;
        product.StockQuantity = 20;
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Products.FindAsync(productId);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(39.99m, updated.Price);
        Assert.Equal(20, updated.StockQuantity);
    }

    [Fact]
    public async Task CatalogService_ShouldDeleteProduct()
    {
        // Arrange
        using var context = CreateContext("CatalogDelete_" + Guid.NewGuid());
        var productId = Guid.NewGuid();

        context.Products.Add(new Product
        {
            Id = productId,
            Name = "Product To Delete",
            Price = 19.99m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var product = await context.Products.FindAsync(productId);
        Assert.NotNull(product);
        context.Products.Remove(product!);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.Products.FindAsync(productId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task CatalogService_ShouldSoftDeleteProductByDeactivating()
    {
        // Arrange
        using var context = CreateContext("CatalogSoftDelete_" + Guid.NewGuid());
        var productId = Guid.NewGuid();

        context.Products.Add(new Product
        {
            Id = productId,
            Name = "Active Product",
            Price = 99.99m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act - deactivate the product
        var product = await context.Products.FindAsync(productId);
        Assert.NotNull(product);
        product!.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert - product exists but is not returned in active queries
        var allProducts = await context.Products.ToListAsync();
        var activeProducts = await context.Products.Where(p => p.IsActive).ToListAsync();

        Assert.Single(allProducts);
        Assert.Empty(activeProducts);
    }

    [Fact]
    public async Task CatalogService_ShouldSoftDeleteProductBySettingIsActiveFalse()
    {
        // Arrange
        using var context = CreateContext("CatalogSoftDeleteEndpoint_" + Guid.NewGuid());
        var productId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();

        context.Products.Add(new Product
        {
            Id = productId,
            Name = "Product To Soft Delete",
            Price = 19.99m,
            MerchantId = merchantId,
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act – simulate the endpoint's soft delete: set IsActive=false instead of Remove
        var product = await context.Products.FindAsync(productId);
        Assert.NotNull(product);
        product!.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert – product still exists in the database but is inactive
        var softDeleted = await context.Products.FindAsync(productId);
        Assert.NotNull(softDeleted);
        Assert.False(softDeleted!.IsActive);
        Assert.Equal(merchantId, softDeleted.MerchantId);
    }

    [Fact]
    public void ProductEvent_ShouldConstructWithCreatedEventType()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var productEvent = new ProductEvent
        {
            ProductId = productId,
            EventType = ProductEventType.Created,
            Name = "Test Product",
            Price = 10.00m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 5,
            IsActive = true,
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(productId, productEvent.ProductId);
        Assert.Equal(ProductEventType.Created, productEvent.EventType);
        Assert.Equal("Test Product", productEvent.Name);
    }

    [Fact]
    public void ProductEvent_ShouldCaptureDeletedEventType()
    {
        // Arrange & Act
        var productEvent = new ProductEvent
        {
            ProductId = Guid.NewGuid(),
            EventType = ProductEventType.Deleted,
            MerchantId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(ProductEventType.Deleted, productEvent.EventType);
    }

    [Fact]
    public void ProductEvent_ShouldCaptureUpdatedEventType()
    {
        // Arrange & Act
        var productEvent = new ProductEvent
        {
            ProductId = Guid.NewGuid(),
            EventType = ProductEventType.Updated,
            Name = "Updated Product",
            Price = 25.00m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 15,
            IsActive = true,
            OccurredAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(ProductEventType.Updated, productEvent.EventType);
        Assert.Equal("Updated Product", productEvent.Name);
    }
}
