using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Merchant.Data;
using UrbanX.Services.Merchant.Models;

namespace UrbanX.Services.Merchant.IntegrationTests;

public class MerchantServiceIntegrationTests
{
    [Fact]
    public async Task MerchantService_ShouldManageMerchants()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new MerchantDbContext(options);
        
        var merchant = new Models.Merchant
        {
            Id = Guid.NewGuid(),
            Name = "Test Merchant",
            Email = "test@merchant.com",
            Phone = "123-456-7890",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Merchants.Add(merchant);
        await context.SaveChangesAsync();

        // Assert
        var savedMerchant = await context.Merchants.FindAsync(merchant.Id);
        savedMerchant.Should().NotBeNull();
        savedMerchant!.Name.Should().Be("Test Merchant");
        savedMerchant.Email.Should().Be("test@merchant.com");
    }

    [Fact]
    public async Task MerchantService_ShouldManageProductsForMerchant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new MerchantDbContext(options);
        var merchantId = Guid.NewGuid();
        
        var products = new List<MerchantProduct>
        {
            new MerchantProduct
            {
                Id = Guid.NewGuid(),
                MerchantId = merchantId,
                Name = "Product 1",
                Price = 99.99m,
                StockQuantity = 10,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new MerchantProduct
            {
                Id = Guid.NewGuid(),
                MerchantId = merchantId,
                Name = "Product 2",
                Price = 49.99m,
                StockQuantity = 20,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Assert
        var merchantProducts = await context.Products
            .Where(p => p.MerchantId == merchantId)
            .ToListAsync();
        merchantProducts.Should().HaveCount(2);
        merchantProducts.Should().OnlyContain(p => p.MerchantId == merchantId);
    }

    [Fact]
    public async Task MerchantService_ShouldUpdateProductInventory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new MerchantDbContext(options);
        
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

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act - Update stock
        product.StockQuantity = 5;
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updatedProduct = await context.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(5);
    }

    [Fact]
    public async Task MerchantService_ShouldManageCategories()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new MerchantDbContext(options);
        var merchantId = Guid.NewGuid();
        
        var categories = new List<MerchantCategory>
        {
            new MerchantCategory
            {
                Id = Guid.NewGuid(),
                MerchantId = merchantId,
                Name = "Electronics",
                Description = "Electronic items",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new MerchantCategory
            {
                Id = Guid.NewGuid(),
                MerchantId = merchantId,
                Name = "Clothing",
                Description = "Apparel items",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Assert
        var merchantCategories = await context.Categories
            .Where(c => c.MerchantId == merchantId)
            .ToListAsync();
        merchantCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task MerchantService_ShouldDeactivateProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MerchantDbContext>()
            .UseInMemoryDatabase(databaseName: "MerchantIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new MerchantDbContext(options);
        
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

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act - Deactivate
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updatedProduct = await context.Products.FindAsync(product.Id);
        updatedProduct!.IsActive.Should().BeFalse();
    }
}
