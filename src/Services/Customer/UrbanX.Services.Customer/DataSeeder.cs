using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Customer.Data;

namespace UrbanX.Services.Customer;

public static class DataSeeder
{
    public static async Task SeedAsync(CustomerDbContext context)
    {
        if (await context.Customers.AnyAsync())
        {
            return; // Database already seeded
        }

        var seedDataPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "customers.json");
        if (!File.Exists(seedDataPath))
        {
            return; // Seed data file is optional
        }

        var jsonContent = await File.ReadAllTextAsync(seedDataPath);
        var customersData = JsonSerializer.Deserialize<List<CustomerSeedData>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (customersData == null || customersData.Count == 0)
        {
            return;
        }

        var customers = customersData.Select(c => new Models.Customer
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            Phone = c.Phone,
            Address = c.Address,
            City = c.City,
            Country = c.Country,
            PostalCode = c.PostalCode,
            IsActive = c.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        // Seed customer groups
        if (!await context.CustomerGroups.AnyAsync())
        {
            var groups = new[]
            {
                new Models.CustomerGroup
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Name = "Standard",
                    Description = "Standard customers",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.CustomerGroup
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "VIP",
                    Description = "VIP customers with exclusive benefits",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.CustomerGroup
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Name = "Premium",
                    Description = "Premium tier customers",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.CustomerGroups.AddRangeAsync(groups);
            await context.SaveChangesAsync();
        }
    }

    private class CustomerSeedData
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public bool IsActive { get; set; }
    }
}
