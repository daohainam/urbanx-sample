using Microsoft.FluentUI.AspNetCore.Components;
using UrbanX.Admin.Client.Pages;
using UrbanX.Admin.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add FluentUI services
builder.Services.AddFluentUIComponents();

// Add HTTP client for API Gateway
builder.Services.AddHttpClient("AdminAPI", client =>
{
    var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(gatewayUrl);
});

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(UrbanX.Admin.Client._Imports).Assembly);

app.Run();
