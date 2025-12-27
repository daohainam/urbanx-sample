using FluentAssertions;
using UrbanX.Services.Merchant.Models;

namespace UrbanX.Services.Merchant.UnitTests;

public class MerchantModelsTests
{
    [Fact]
    public void Merchant_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var merchant = new Models.Merchant
        {
            Id = Guid.NewGuid(),
            Name = "Test Merchant",
            Email = "test@merchant.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        merchant.Should().NotBeNull();
        merchant.Name.Should().Be("Test Merchant");
        merchant.Email.Should().Be("test@merchant.com");
        merchant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Merchant_ShouldAllowOptionalProperties()
    {
        // Arrange & Act
        var merchant = new Models.Merchant
        {
            Id = Guid.NewGuid(),
            Name = "Test Merchant",
            Description = "A test merchant",
            Email = "test@merchant.com",
            Phone = "123-456-7890",
            Address = "123 Main St",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        merchant.Description.Should().Be("A test merchant");
        merchant.Phone.Should().Be("123-456-7890");
        merchant.Address.Should().Be("123 Main St");
    }

    [Fact]
    public void MerchantProduct_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
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

        // Assert
        product.Should().NotBeNull();
        product.Name.Should().Be("Test Product");
        product.Price.Should().Be(99.99m);
        product.StockQuantity.Should().Be(10);
    }

    [Fact]
    public void MerchantProduct_ShouldAllowOptionalProperties()
    {
        // Arrange & Act
        var product = new MerchantProduct
        {
            Id = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Product description",
            Price = 50.00m,
            StockQuantity = 5,
            ImageUrl = "https://example.com/image.jpg",
            Category = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        product.Description.Should().Be("Product description");
        product.ImageUrl.Should().Be("https://example.com/image.jpg");
        product.Category.Should().Be("Electronics");
    }

    [Fact]
    public void MerchantCategory_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var category = new MerchantCategory
        {
            Id = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            Name = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        category.Should().NotBeNull();
        category.Name.Should().Be("Electronics");
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MerchantCategory_ShouldAllowOptionalDescription()
    {
        // Arrange & Act
        var category = new MerchantCategory
        {
            Id = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            Name = "Electronics",
            Description = "Electronic products",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        category.Description.Should().Be("Electronic products");
    }
}
