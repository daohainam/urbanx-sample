using System.Text.Json;
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

        var productsPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "products.json");
        if (!File.Exists(productsPath))
        {
            throw new FileNotFoundException($"Seed data file not found at {productsPath}. Ensure the SeedData folder and products.json file are included in the project and copied to the output directory.");
        }

        var jsonContent = await File.ReadAllTextAsync(productsPath);
        var productsData = JsonSerializer.Deserialize<List<ProductSeedData>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (productsData == null || productsData.Count == 0)
        {
            return;
        }

        var products = productsData.Select(p => new Product
        {
            Id = Guid.NewGuid(),
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            MerchantId = p.MerchantId,
            StockQuantity = p.StockQuantity,
            Category = p.Category,
            IsActive = p.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    private class ProductSeedData
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public Guid MerchantId { get; set; }
        public int StockQuantity { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
    }
}
