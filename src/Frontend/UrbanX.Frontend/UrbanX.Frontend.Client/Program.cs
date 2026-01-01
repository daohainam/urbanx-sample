using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UrbanX.Frontend.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<CartState>();

await builder.Build().RunAsync();
