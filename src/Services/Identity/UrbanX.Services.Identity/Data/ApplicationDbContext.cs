using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrbanX.Services.Identity.Models;

namespace UrbanX.Services.Identity.Data;

/// <summary>
/// ApplicationDbContext inherits from IdentityDbContext which provides EF Core mappings for:
/// - AspNetUsers        → ApplicationUser rows (hashed passwords, claims, lockout state, etc.)
/// - AspNetRoles        → IdentityRole rows
/// - AspNetUserRoles    → many-to-many user↔role pivot
/// - AspNetUserClaims   → per-user claims (not used directly here; we use ApplicationUser.Role)
/// - AspNetUserLogins   → external login providers (Google, GitHub, etc.)
/// - AspNetUserTokens   → refresh/security tokens stored server-side
/// - AspNetRoleClaims   → per-role claims
///
/// Using IdentityDbContext keeps the schema in sync with the Identity framework and avoids
/// manual migration of its tables.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Call the base implementation first so Identity tables are configured
        base.OnModelCreating(builder);

        // Apply custom configuration for ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            // Index on Role for fast lookups when listing users by role
            entity.HasIndex(u => u.Role);

            // Constrain Role to known values at the database level via check constraint
            entity.ToTable("AspNetUsers", t =>
                t.HasCheckConstraint("CK_AspNetUsers_Role",
                    "\"Role\" IS NULL OR \"Role\" IN ('customer', 'merchant', 'admin')"));
        });
    }
}
