using Microsoft.AspNetCore.Identity;

namespace UrbanX.Services.Identity.Models;

/// <summary>
/// ApplicationUser extends ASP.NET Core Identity's IdentityUser to add application-specific
/// profile fields. IdentityUser already provides: Id (string, GUID), UserName, Email,
/// PasswordHash (PBKDF2-SHA256), SecurityStamp, PhoneNumber, EmailConfirmed, LockoutEnabled, etc.
///
/// In the OIDC/OAuth2 flow:
/// - The user's Id becomes the "sub" (subject) claim in the ID token and access token.
/// - Profile claims (name, email, role) are emitted by the IProfileService, which
///   reads them from this entity and includes them in tokens/userinfo responses.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// The user's display name, emitted as the "name" claim in tokens.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Application role: "customer" or "merchant".
    /// Stored here for convenience; actual role enforcement uses ASP.NET Identity Roles
    /// (IdentityRole) and the "role" claim injected by the profile service.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// UTC timestamp when the user account was created. Useful for auditing.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
