using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using UrbanX.Services.Identity.Models;

namespace UrbanX.Services.Identity.Pages.Account;

/// <summary>
/// Login page handler for the OAuth2 / OIDC Authorization Code flow.
///
/// Flow overview:
/// 1. The SPA calls GET /connect/authorize with response_type=code, client_id, redirect_uri,
///    scope, state, code_challenge (PKCE).
/// 2. IdentityServer checks whether the user already has an authenticated session cookie.
///    If not, it redirects to this page: GET /Account/Login?returnUrl=...
///    The returnUrl is an opaque IdentityServer-issued token (the "authorization request").
/// 3. This page shows a login form. On POST, it validates credentials via ASP.NET Identity's
///    SignInManager (password hashed with PBKDF2/bcrypt, never stored in plaintext).
/// 4. On success, HttpContext.SignInAsync() creates an encrypted session cookie.
///    IIdentityServerInteractionService.GrantConsentAsync() is NOT needed here because
///    we're doing username/password login, not external provider callback.
///    IdentityServer then resumes the authorization request and redirects back to the SPA.
/// 5. The SPA exchanges the authorization code for tokens at POST /connect/token (PKCE verifier).
/// </summary>
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IIdentityServerInteractionService interaction,
        IEventService events,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _interaction = interaction;
        _events = events;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Non-null when ASP.NET model validation fails or credentials are wrong.
    /// Displayed on the form so the user knows why login was rejected.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// GET /Account/Login?returnUrl=...
    /// Validates the returnUrl and pre-populates the form.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(string? returnUrl)
    {
        returnUrl ??= Url.Content("~/");

        // Validate that returnUrl is either a local path or an IdentityServer-issued
        // authorization request URL (starting with /connect/).
        // This prevents open redirect attacks where an attacker tricks the user into
        // logging in and then being redirected to a malicious site.
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context == null && !Url.IsLocalUrl(returnUrl))
        {
            // Reject suspicious returnUrls that don't belong to IdentityServer
            returnUrl = Url.Content("~/");
        }

        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>
    /// POST /Account/Login
    /// Validates credentials, creates session, and redirects to the authorization endpoint.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please enter your username and password.";
            return Page();
        }

        var returnUrl = Input.ReturnUrl ?? Url.Content("~/");

        // Look up the user. UserManager normalises the username/email for case-insensitive lookup.
        var user = await _userManager.FindByNameAsync(Input.Username)
                   ?? await _userManager.FindByEmailAsync(Input.Username);

        if (user != null)
        {
            // PasswordSignInAsync checks:
            // - password (PBKDF2-hashed), - lockout state, - account confirmation.
            // lockoutOnFailure: true – after N consecutive failures the account is locked
            // for a configurable window (default: 5 failures → 5 min lockout).
            var result = await _signInManager.PasswordSignInAsync(
                user, Input.Password, Input.RememberLogin, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Record the successful login event in the IdentityServer audit log.
                await _events.RaiseAsync(new UserLoginSuccessEvent(
                    user.UserName, user.Id, user.UserName));

                _logger.LogInformation("User {UserId} logged in successfully", user.Id);

                // IdentityServer validates the returnUrl; if it is an active authorization
                // request, it completes the flow and issues the authorization code.
                if (Url.IsLocalUrl(returnUrl) || _interaction.IsValidReturnUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return Redirect("~/");
            }

            if (result.IsLockedOut)
            {
                await _events.RaiseAsync(new UserLoginFailureEvent(
                    Input.Username, "account locked out"));
                _logger.LogWarning("User {Username} account locked out", Input.Username);
                ErrorMessage = "Your account has been temporarily locked due to multiple failed login attempts. Please try again later.";
                return Page();
            }
        }

        // Intentionally vague: don't reveal whether the account exists.
        await _events.RaiseAsync(new UserLoginFailureEvent(Input.Username, "invalid credentials"));
        _logger.LogWarning("Failed login attempt for username {Username}", Input.Username);
        ErrorMessage = "Invalid username or password.";
        return Page();
    }

    /// <summary>
    /// View model for the login form.
    /// </summary>
    public class InputModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberLogin { get; set; }

        /// <summary>
        /// Opaque IdentityServer-issued returnUrl that encodes the original
        /// /connect/authorize request. Must be threaded through the form so that
        /// on successful login the user is redirected back to complete the OAuth2 flow.
        /// </summary>
        public string? ReturnUrl { get; set; }
    }
}
