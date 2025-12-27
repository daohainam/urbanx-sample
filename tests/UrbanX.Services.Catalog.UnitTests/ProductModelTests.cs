using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog.UnitTests;

public class ProductModelTests
{
    [Fact]
    public void Product_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
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

        // Assert
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(99.99m, product.Price);
        Assert.Equal(10, product.StockQuantity);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Product_ShouldAllowOptionalProperties()
    {
        // Arrange & Act
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test Description",
            Price = 50.00m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 5,
            ImageUrl = "https://example.com/image.jpg",
            Category = "Electronics",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Assert
        Assert.Equal("Test Description", product.Description);
        Assert.Equal("https://example.com/image.jpg", product.ImageUrl);
        Assert.Equal("Electronics", product.Category);
    }

    [Fact]
    public void Product_ShouldAllowNullOptionalProperties()
    {
        // Arrange & Act
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = null,
            Price = 25.00m,
            MerchantId = Guid.NewGuid(),
            StockQuantity = 0,
            ImageUrl = null,
            Category = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = false
        };

        // Assert
        Assert.Null(product.Description);
        Assert.Null(product.ImageUrl);
        Assert.Null(product.Category);
        Assert.False(product.IsActive);
    }
}
