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

        var merchants = new List<Models.Merchant>
        {
            new Models.Merchant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Tech Hub Electronics",
                Description = "Your one-stop shop for all electronics and tech accessories",
                Email = "contact@techhub.com",
                Phone = "+1-555-0100",
                Address = "123 Tech Street, Silicon Valley, CA 94025",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Models.Merchant
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Smart Gadgets Store",
                Description = "Premium smart devices and wearables",
                Email = "info@smartgadgets.com",
                Phone = "+1-555-0200",
                Address = "456 Innovation Ave, San Francisco, CA 94103",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Merchants.AddRangeAsync(merchants);
        await context.SaveChangesAsync();
    }
}
