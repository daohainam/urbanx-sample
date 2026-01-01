using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using UrbanX.Frontend.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add authentication services for Keycloak
builder.Services.AddOidcAuthentication(options =>
{
    // Configure the authority (Keycloak server)
    options.ProviderOptions.Authority = "http://localhost:8080/realms/urbanx";
    options.ProviderOptions.ClientId = "urbanx-spa";
    options.ProviderOptions.ResponseType = "code";
    
    // Configure scopes
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
    options.ProviderOptions.DefaultScopes.Add("roles");
    
    // Post logout redirect
    options.ProviderOptions.PostLogoutRedirectUri = "/";
    
    // Additional configuration
    options.UserOptions.RoleClaim = "realm_access.roles";
});

builder.Services.AddSingleton<CartState>();

await builder.Build().RunAsync();
