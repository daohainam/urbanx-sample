using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Merchant.Data;
using UrbanX.Services.Merchant.Models;

namespace UrbanX.Services.Merchant.UnitTests;

public class MerchantDbContextTests
{
    [Fact]
    public void MerchantDbContext_ShouldConfigureEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantTestDb_" + Guid.NewGuid())
            .Options;

        // Act
        using var context = new MerchantDbContext(options);
        var merchantEntityType = context.Model.FindEntityType(typeof(Models.Merchant));
        var productEntityType = context.Model.FindEntityType(typeof(MerchantProduct));
        var categoryEntityType = context.Model.FindEntityType(typeof(MerchantCategory));

        // Assert
        merchantEntityType.Should().NotBeNull();
        productEntityType.Should().NotBeNull();
        categoryEntityType.Should().NotBeNull();
    }

    [Fact]
    public async Task MerchantDbContext_ShouldAddAndRetrieveMerchant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantTestDb_" + Guid.NewGuid())
            .Options;

        var merchant = new Models.Merchant
        {
            Id = Guid.NewGuid(),
            Name = "Test Merchant",
            Email = "test@merchant.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new MerchantDbContext(options))
        {
            context.Merchants.Add(merchant);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new MerchantDbContext(options))
        {
            var savedMerchant = await context.Merchants.FindAsync(merchant.Id);
            savedMerchant.Should().NotBeNull();
            savedMerchant!.Name.Should().Be("Test Merchant");
            savedMerchant.Email.Should().Be("test@merchant.com");
        }
    }

    [Fact]
    public async Task MerchantDbContext_ShouldAddAndRetrieveProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantTestDb_" + Guid.NewGuid())
            .Options;

        var product = new MerchantProduct
        {
            Id = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new MerchantDbContext(options))
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new MerchantDbContext(options))
        {
            var savedProduct = await context.Products.FindAsync(product.Id);
            savedProduct.Should().NotBeNull();
            savedProduct!.Name.Should().Be("Test Product");
            savedProduct.Price.Should().Be(99.99m);
        }
    }

    [Fact]
    public async Task MerchantDbContext_ShouldAddAndRetrieveCategory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantTestDb_" + Guid.NewGuid())
            .Options;

        var category = new MerchantCategory
        {
            Id = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            Name = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new MerchantDbContext(options))
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new MerchantDbContext(options))
        {
            var savedCategory = await context.Categories.FindAsync(category.Id);
            savedCategory.Should().NotBeNull();
            savedCategory!.Name.Should().Be("Electronics");
        }
    }

    [Fact]
    public async Task MerchantDbContext_ShouldFilterProductsByMerchant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantTestDb_" + Guid.NewGuid())
            .Options;

        var merchantId = Guid.NewGuid();
        var product1 = new MerchantProduct
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            Name = "Product 1",
            Price = 10.00m,
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var product2 = new MerchantProduct
        {
            Id = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            Name = "Product 2",
            Price = 20.00m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new MerchantDbContext(options))
        {
            context.Products.AddRange(product1, product2);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new MerchantDbContext(options))
        {
            var merchantProducts = await context.Products
                .Where(p => p.MerchantId == merchantId)
                .ToListAsync();
            merchantProducts.Should().HaveCount(1);
            merchantProducts.First().Name.Should().Be("Product 1");
        }
    }
}
