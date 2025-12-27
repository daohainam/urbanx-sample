using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Merchant.Data;

namespace UrbanX.Services.Merchant;

public static class DataSeeder
{
    public static async Task SeedAsync(MerchantDbContext context)
    {
        if (await context.Merchants.AnyAsync())
        {
            return; // Database already seeded
        }

        var seedDataPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "merchants.json");
        if (!File.Exists(seedDataPath))
        {
            throw new FileNotFoundException($"Seed data file not found at {seedDataPath}. Ensure the SeedData folder and merchants.json file are included in the project and copied to the output directory.");
        }

        var jsonContent = await File.ReadAllTextAsync(seedDataPath);
        var merchantsData = JsonSerializer.Deserialize<List<MerchantSeedData>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (merchantsData == null || merchantsData.Count == 0)
        {
            return;
        }

        var merchants = merchantsData.Select(m => new Models.Merchant
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Email = m.Email,
            Phone = m.Phone,
            Address = m.Address,
            IsActive = m.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.Merchants.AddRangeAsync(merchants);
        await context.SaveChangesAsync();
    }

    private class MerchantSeedData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
}
