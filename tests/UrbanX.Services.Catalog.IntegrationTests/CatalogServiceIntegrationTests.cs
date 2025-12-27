using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.IntegrationTests;

public class CatalogServiceApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseInMemoryDatabase("CatalogTestDb_" + Guid.NewGuid());
            });
        });

        return base.CreateHost(builder);
    }
}

public class CatalogServiceIntegrationTests : IClassFixture<CatalogServiceApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CatalogServiceApplicationFactory _factory;

    public CatalogServiceIntegrationTests(CatalogServiceApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOkResult()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        result.Should().NotBeNull();
        result!.Products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProducts_WithSearch_ReturnsFilteredResults()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/products?search=Laptop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        result.Should().NotBeNull();
        result!.Products.Should().Contain(p => p.Name.Contains("Laptop"));
    }

    [Fact]
    public async Task GetProducts_WithCategory_ReturnsFilteredResults()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/products?category=Electronics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        result.Should().NotBeNull();
        result!.Products.Should().OnlyContain(p => p.Category == "Electronics");
    }

    [Fact]
    public async Task GetProducts_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/products?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        result.Should().NotBeNull();
        result!.Products.Count.Should().BeLessThanOrEqualTo(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        var productId = await SeedDatabase();

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(productId);
    }

    [Fact]
    public async Task GetProductById_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductsByMerchant_ReturnsOnlyMerchantProducts()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        await SeedDatabaseForMerchant(merchantId);

        // Act
        var response = await _client.GetAsync($"/api/products/merchant/{merchantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeNull();
        products.Should().OnlyContain(p => p.MerchantId == merchantId);
    }

    private async Task<Guid> SeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var merchantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var products = new List<Product>
        {
            new Product
            {
                Id = productId,
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
                Name = "Office Chair",
                Description = "Comfortable chair",
                Price = 199.99m,
                MerchantId = Guid.NewGuid(),
                StockQuantity = 15,
                Category = "Furniture",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        return productId;
    }

    private async Task SeedDatabaseForMerchant(Guid merchantId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product 1",
                Price = 10.00m,
                MerchantId = merchantId,
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
                MerchantId = merchantId,
                StockQuantity = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private record ProductListResponse(List<Product> Products, int Total, int Page, int PageSize);
}
