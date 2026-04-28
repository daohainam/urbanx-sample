using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace UrbanX.Shared.Security;

/// <summary>
/// Named authorization policy constants used across all UrbanX services.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Requires the authenticated user to have the <c>customer</c> role.</summary>
    public const string CustomerOnly = "CustomerOnly";

    /// <summary>Requires the authenticated user to have the <c>merchant</c> role.</summary>
    public const string MerchantOnly = "MerchantOnly";

    /// <summary>Requires the authenticated user to have either the <c>customer</c> or <c>merchant</c> role.</summary>
    public const string CustomerOrMerchant = "CustomerOrMerchant";

    /// <summary>Requires the authenticated user to have the <c>admin</c> role.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Requires the authenticated user to have either the <c>merchant</c> or <c>admin</c> role.</summary>
    public const string MerchantOrAdmin = "MerchantOrAdmin";

    /// <summary>
    /// Registers the UrbanX authorization policies with the DI container.
    /// Call this instead of the plain <c>AddAuthorization()</c> overload.
    /// </summary>
    public static IServiceCollection AddUrbanXAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(CustomerOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "customer"));

            options.AddPolicy(MerchantOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "merchant"));

            options.AddPolicy(CustomerOrMerchant, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(ctx =>
                          ctx.User.HasClaim("role", "customer") ||
                          ctx.User.HasClaim("role", "merchant")));

            options.AddPolicy(AdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "admin"));

            options.AddPolicy(MerchantOrAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(ctx =>
                          ctx.User.HasClaim("role", "merchant") ||
                          ctx.User.HasClaim("role", "admin")));
        });

        return services;
    }
}
