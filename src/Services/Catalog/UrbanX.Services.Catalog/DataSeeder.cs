using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog;

public static class DataSeeder
{
    public static async Task SeedAsync(CatalogDbContext context)
    {
        if (await context.Products.AnyAsync())
        {
            return; // Database already seeded
        }

        var merchantId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var merchantId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var products = new List<Product>
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Wireless Headphones",
                Description = "High-quality wireless headphones with noise cancellation",
                Price = 99.99m,
                MerchantId = merchantId1,
                StockQuantity = 50,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Laptop Stand",
                Description = "Ergonomic aluminum laptop stand",
                Price = 49.99m,
                MerchantId = merchantId1,
                StockQuantity = 100,
                Category = "Accessories",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Smart Watch",
                Description = "Fitness tracking smart watch with heart rate monitor",
                Price = 199.99m,
                MerchantId = merchantId2,
                StockQuantity = 30,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "USB-C Cable",
                Description = "Fast charging USB-C cable 6ft",
                Price = 14.99m,
                MerchantId = merchantId2,
                StockQuantity = 200,
                Category = "Accessories",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Mechanical Keyboard",
                Description = "RGB mechanical gaming keyboard",
                Price = 129.99m,
                MerchantId = merchantId1,
                StockQuantity = 25,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with precision tracking",
                Price = 39.99m,
                MerchantId = merchantId2,
                StockQuantity = 75,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
