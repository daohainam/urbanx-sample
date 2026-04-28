using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace UrbanX.Management.Web.Services.Auth;

/// <summary>
/// Forwards the current user's OIDC access token (saved via SaveTokens=true)
/// as a Bearer Authorization header on outbound HTTP calls to backend services.
/// </summary>
public sealed class TokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
        {
            var token = await ctx.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
