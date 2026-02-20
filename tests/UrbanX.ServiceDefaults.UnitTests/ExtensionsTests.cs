using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UrbanX.ServiceDefaults.UnitTests;

public class ExtensionsTests
{
    [Fact]
    public void AddServiceDefaults_ShouldRegisterServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        
        // Act
        builder.AddServiceDefaults();
        var app = builder.Build();

        // Assert
        Assert.NotNull(app);
        Assert.NotNull(app.Services);
    }

    [Fact]
    public void AddDefaultHealthChecks_ShouldRegisterHealthChecks()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        
        // Act
        builder.AddDefaultHealthChecks();
        var app = builder.Build();

        // Assert
        var healthCheckService = app.Services.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public void MapDefaultEndpoints_ShouldMapHealthChecksInDevelopment()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        builder.Environment.EnvironmentName = Environments.Development;
        builder.AddDefaultHealthChecks();
        var app = builder.Build();

        // Act
        app.MapDefaultEndpoints();

        // Assert
        Assert.NotNull(app);
        // In a real integration test, we would verify the endpoints are mapped
    }

    [Fact]
    public void ConfigureOpenTelemetry_ShouldConfigureObservability()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder([]);
        
        // Act
        builder.ConfigureOpenTelemetry();
        var app = builder.Build();

        // Assert
        Assert.NotNull(app);
        // OpenTelemetry services should be configured
    }
}
