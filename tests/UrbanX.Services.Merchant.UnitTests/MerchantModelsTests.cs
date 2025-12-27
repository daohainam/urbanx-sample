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
        Assert.NotNull(merchant);
        Assert.Equal("Test Merchant", merchant.Name);
        Assert.Equal("test@merchant.com", merchant.Email);
        Assert.True(merchant.IsActive);
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
        Assert.Equal("A test merchant", merchant.Description);
        Assert.Equal("123-456-7890", merchant.Phone);
        Assert.Equal("123 Main St", merchant.Address);
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
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(99.99m, product.Price);
        Assert.Equal(10, product.StockQuantity);
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
        Assert.Equal("Product description", product.Description);
        Assert.Equal("https://example.com/image.jpg", product.ImageUrl);
        Assert.Equal("Electronics", product.Category);
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
        Assert.NotNull(category);
        Assert.Equal("Electronics", category.Name);
        Assert.True(category.IsActive);
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
        Assert.Equal("Electronic products", category.Description);
    }
}
