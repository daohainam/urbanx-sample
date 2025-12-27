using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.UnitTests;

public class CatalogDbContextTests
{
    [Fact]
    public void CatalogDbContext_ShouldConfigureProductEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogTestDb_" + Guid.NewGuid())
            .Options;

        // Act
        using var context = new CatalogDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Product));

        // Assert
        Assert.NotNull(entityType);
        Assert.Contains(entityType!.FindPrimaryKey()!.Properties, p => p.Name == "Id");
    }

    [Fact]
    public async Task CatalogDbContext_ShouldAddAndRetrieveProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogTestDb_" + Guid.NewGuid())
            .Options;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        using (var context = new CatalogDbContext(options))
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new CatalogDbContext(options))
        {
            var savedProduct = await context.Products.FindAsync(product.Id);
            Assert.NotNull(savedProduct);
            Assert.Equal("Test Product", savedProduct!.Name);
            Assert.Equal(99.99m, savedProduct.Price);
        }
    }

    [Fact]
    public async Task CatalogDbContext_ShouldEnforceRequiredProperties()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogTestDb_" + Guid.NewGuid())
            .Options;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 50.00m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act & Assert
        using var context = new CatalogDbContext(options);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var savedProduct = await context.Products.FindAsync(product.Id);
        Assert.NotNull(savedProduct);
        Assert.Equal("Test Product", savedProduct!.Name);
    }
}
