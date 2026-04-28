using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Catalog.Data;
using UrbanX.Services.Catalog.Models;

namespace UrbanX.Services.Catalog;

public static class DataSeeder
{
    public static async Task SeedAsync(CatalogDbContext context)
    {
        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context);
    }

    private static async Task SeedCategoriesAsync(CatalogDbContext context)
    {
        var categoriesPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "categories.json");
        if (!File.Exists(categoriesPath)) return;

        var jsonContent = await File.ReadAllTextAsync(categoriesPath);
        var names = JsonSerializer.Deserialize<List<string>>(jsonContent);
        if (names is null) return;

        var existingNames = await context.Categories.Select(c => c.Name).ToListAsync();
        var toAdd = names
            .Where(n => !string.IsNullOrWhiteSpace(n) && !existingNames.Contains(n))
            .Select(n => new Category
            {
                Id = Guid.NewGuid(),
                Name = n,
                Slug = Slugify(n),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            await context.Categories.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedProductsAsync(CatalogDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

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

        if (productsData == null || productsData.Count == 0) return;

        var categoryByName = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

        var products = productsData.Select(p => new Product
        {
            Id = Guid.NewGuid(),
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            MerchantId = p.MerchantId,
            StockQuantity = p.StockQuantity,
            Category = p.Category,
            CategoryId = p.Category is not null && categoryByName.TryGetValue(p.Category, out var id) ? id : null,
            IsActive = p.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
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
