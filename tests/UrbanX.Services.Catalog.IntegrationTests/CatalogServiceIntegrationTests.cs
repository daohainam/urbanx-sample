using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.IntegrationTests;

public class CatalogServiceIntegrationTests
{
    [Fact]
    public async Task CatalogService_ShouldQueryActiveProducts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CatalogDbContext(options);
        
        var merchantId = Guid.NewGuid();
        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Gaming Laptop",
                Description = "High performance laptop",
                Price = 1299.99m,
                MerchantId = merchantId,
                StockQuantity = 10,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Wireless Mouse",
                Description = "Ergonomic mouse",
                Price = 29.99m,
                MerchantId = merchantId,
                StockQuantity = 50,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Inactive Product",
                Price = 100.00m,
                MerchantId = merchantId,
                StockQuantity = 0,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act
        var activeProducts = await context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        // Assert
        activeProducts.Should().HaveCount(2);
        activeProducts.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public async Task CatalogService_ShouldSearchProductsByName()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CatalogDbContext(options);
        
        var merchantId = Guid.NewGuid();
        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Gaming Laptop",
                Price = 1299.99m,
                MerchantId = merchantId,
                StockQuantity = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Office Laptop",
                Price = 899.99m,
                MerchantId = merchantId,
                StockQuantity = 15,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Wireless Mouse",
                Price = 29.99m,
                MerchantId = merchantId,
                StockQuantity = 50,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act
        var searchResults = await context.Products
            .Where(p => p.IsActive && p.Name.Contains("Laptop"))
            .ToListAsync();

        // Assert
        searchResults.Should().HaveCount(2);
        searchResults.Should().OnlyContain(p => p.Name.Contains("Laptop"));
    }

    [Fact]
    public async Task CatalogService_ShouldFilterProductsByCategory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CatalogDbContext(options);
        
        var merchantId = Guid.NewGuid();
        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Price = 1299.99m,
                MerchantId = merchantId,
                StockQuantity = 10,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Office Chair",
                Price = 199.99m,
                MerchantId = merchantId,
                StockQuantity = 20,
                Category = "Furniture",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act
        var electronicsProducts = await context.Products
            .Where(p => p.IsActive && p.Category == "Electronics")
            .ToListAsync();

        // Assert
        electronicsProducts.Should().HaveCount(1);
        electronicsProducts.First().Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task CatalogService_ShouldGetProductsByMerchant()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CatalogDbContext(options);
        
        var merchant1Id = Guid.NewGuid();
        var merchant2Id = Guid.NewGuid();
        
        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product 1",
                Price = 10.00m,
                MerchantId = merchant1Id,
                StockQuantity = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product 2",
                Price = 20.00m,
                MerchantId = merchant1Id,
                StockQuantity = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product 3",
                Price = 30.00m,
                MerchantId = merchant2Id,
                StockQuantity = 15,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act
        var merchant1Products = await context.Products
            .Where(p => p.MerchantId == merchant1Id && p.IsActive)
            .ToListAsync();

        // Assert
        merchant1Products.Should().HaveCount(2);
        merchant1Products.Should().OnlyContain(p => p.MerchantId == merchant1Id);
    }

    [Fact]
    public async Task CatalogService_ShouldSupportPagination()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogIntegrationTest_" + Guid.NewGuid())
            .Options;

        using var context = new CatalogDbContext(options);
        
        var merchantId = Guid.NewGuid();
        var products = Enumerable.Range(1, 25).Select(i => new Product
        {
            Id = Guid.NewGuid(),
            Name = $"Product {i}",
            Price = i * 10.00m,
            MerchantId = merchantId,
            StockQuantity = i,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act
        int page = 2;
        int pageSize = 10;
        var pagedProducts = await context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Assert
        pagedProducts.Should().HaveCount(10);
    }
}
