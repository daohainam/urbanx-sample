using Microsoft.AspNetCore.Identity;
using UrbanX.Services.Identity.Models;

namespace UrbanX.Services.Identity;

/// <summary>
/// Seeds the database with initial users and roles on first startup.
///
/// This class is idempotent: it checks whether each user already exists before
/// creating them, so it is safe to call on every startup.
///
/// Seeded accounts:
/// - admin@urbanx.com  (role: admin)      – for managing users via the API
/// - customer@urbanx.com (role: customer) – sample customer account
/// - merchant@urbanx.com (role: merchant) – sample merchant account
///
/// IMPORTANT: Change default passwords immediately after first deployment.
/// Default passwords are intentionally weak for ease of initial setup only.
/// In production, use environment variables or a secret manager for passwords.
/// </summary>
public static class SeedData
{
    private static readonly (string Email, string FullName, string Role, string DefaultPassword)[] DefaultUsers =
    [
        ("admin@urbanx.com",    "UrbanX Admin",    "admin",    "Admin@123!"),
        ("customer@urbanx.com", "Sample Customer", "customer", "Customer@123!"),
        ("merchant@urbanx.com", "Sample Merchant", "merchant", "Merchant@123!")
    ];

    /// <summary>
    /// Creates roles and seed users if they do not already exist.
    /// Call this once from Program.cs after migrations have been applied.
    /// </summary>
    public static async Task EnsureSeedDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // 1. Ensure all application roles exist
        string[] roles = ["admin", "customer", "merchant"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                    logger.LogInformation("Created role '{Role}'", role);
                else
                    logger.LogError("Failed to create role '{Role}': {Errors}", role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // 2. Ensure each seed user exists
        foreach (var (email, fullName, role, defaultPassword) in DefaultUsers)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                // User already seeded; skip to avoid overwriting production data.
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,   // Skip email confirmation for seed accounts
                FullName = fullName,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, defaultPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create seed user {Email}: {Errors}", email,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            // Assign the role to the user (used for ASP.NET Identity role-based auth)
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to assign role '{Role}' to user {Email}: {Errors}",
                    role, email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
            else
            {
                logger.LogInformation("Seeded user {Email} with role '{Role}'", email, role);
            }
        }
    }
}
