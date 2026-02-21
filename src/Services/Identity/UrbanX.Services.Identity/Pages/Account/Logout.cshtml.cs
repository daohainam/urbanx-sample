using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UrbanX.Services.Identity.Models;

namespace UrbanX.Services.Identity.Pages.Account;

/// <summary>
/// Logout page handler for the OIDC RP-Initiated Logout flow (OpenID Connect Session Management).
///
/// Flow overview:
/// 1. The SPA calls GET /connect/endsession?post_logout_redirect_uri=...&id_token_hint=...
/// 2. IdentityServer validates the id_token_hint and redirects to GET /Account/Logout?logoutId=...
/// 3. This page shows a confirmation prompt (to prevent CSRF-driven logout attacks).
/// 4. On POST, it signs the user out (removes the session cookie) and calls
///    GetLogoutContextAsync to retrieve the post-logout redirect URI.
/// 5. The user is either auto-redirected to the client's post_logout_redirect_uri or
///    sees a "You have been signed out" message with a manual link.
///
/// Security note: The logoutId is a server-side reference to the logout request context.
/// It prevents attackers from fabricating returnUrls.
/// </summary>
public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        IIdentityServerInteractionService interaction,
        IEventService events,
        ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _interaction = interaction;
        _events = events;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? LogoutId { get; set; }

    /// <summary>
    /// When true, render the confirmation prompt.
    /// When false, logout already occurred (page was rendered after sign-out).
    /// </summary>
    public bool ShowLogoutPrompt { get; set; } = true;

    /// <summary>
    /// URI to redirect the user to after logout (the client's post_logout_redirect_uri).
    /// Null if the logout request did not specify one.
    /// </summary>
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// GET /Account/Logout?logoutId=...
    /// Shows a confirmation prompt to prevent CSRF-driven logouts.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // If the user is not authenticated there is nothing to sign out.
        if (User?.Identity?.IsAuthenticated != true)
        {
            ShowLogoutPrompt = false;
            return Page();
        }

        // Retrieve the logout context for this request so we can later
        // obtain the post-logout redirect URI and iframe for front-channel logout.
        var context = await _interaction.GetLogoutContextAsync(LogoutId);
        if (context?.ShowSignoutPrompt == false)
        {
            // Safe to skip the prompt (e.g. when triggered by the client directly)
            return await PerformLogout();
        }

        return Page();
    }

    /// <summary>
    /// POST /Account/Logout
    /// Signs the user out, raises an audit event, and redirects.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return await PerformLogout();
        }

        ShowLogoutPrompt = false;
        return Page();
    }

    private async Task<IActionResult> PerformLogout()
    {
        // Retrieve the logout context before signing out (the session cookie is still valid here).
        var context = await _interaction.GetLogoutContextAsync(LogoutId);
        PostLogoutRedirectUri = context?.PostLogoutRedirectUri;

        var userId = User?.FindFirst("sub")?.Value;
        var userName = User?.FindFirst("name")?.Value ?? User?.Identity?.Name;

        // Sign out of ASP.NET Identity (clears the application cookie) and IdentityServer.
        await _signInManager.SignOutAsync();

        // Also clear the external authentication cookie if an external provider was used.
        await HttpContext.SignOutAsync();

        if (userId != null)
        {
            await _events.RaiseAsync(new UserLogoutSuccessEvent(userId, userName));
            _logger.LogInformation("User {UserId} signed out", userId);
        }

        ShowLogoutPrompt = false;

        // Redirect to the client's post-logout URI if provided and valid.
        if (!string.IsNullOrEmpty(PostLogoutRedirectUri))
        {
            return Redirect(PostLogoutRedirectUri);
        }

        return Page();
    }
}
