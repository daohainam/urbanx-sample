using FluentAssertions;
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
        product.Should().NotBeNull();
        product.Name.Should().Be("Test Product");
        product.Price.Should().Be(99.99m);
        product.StockQuantity.Should().Be(10);
        product.IsActive.Should().BeTrue();
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
        product.Description.Should().Be("Test Description");
        product.ImageUrl.Should().Be("https://example.com/image.jpg");
        product.Category.Should().Be("Electronics");
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
        product.Description.Should().BeNull();
        product.ImageUrl.Should().BeNull();
        product.Category.Should().BeNull();
        product.IsActive.Should().BeFalse();
    }
}
